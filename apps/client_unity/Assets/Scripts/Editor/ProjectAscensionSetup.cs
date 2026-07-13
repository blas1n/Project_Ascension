using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ProjectAscension.Combat;
using ProjectAscension.Core;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Equipment;
using ProjectAscension.Game;
using ProjectAscension.Monsters;
using ProjectAscension.Player;

namespace ProjectAscension.Editor
{
    /// <summary>
    /// One-click scene/asset setup so the Phase 1 player scene is created
    /// deterministically instead of being wired by hand. Run from the menu:
    /// "Project Ascension/Setup/Build All Scenes".
    /// </summary>
    public static class ProjectAscensionSetup
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string DataDir = "Assets/Data/ScriptableObjects";
        private const string WeaponsDir = DataDir + "/Weapons";
        private const string PlayerDataPath = DataDir + "/PlayerData.asset";
        private const string LoadoutConfigPath = DataDir + "/StarterLoadout.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        // The slice's API base URL — GameSession fetches DB-driven balance (combat tuning,
        // weapon + monster stats) from here at startup. Empty = offline (built-in defaults
        // / authored assets). Change to where your API runs (Docker maps localhost:8080).
        private const string DevServerUrl = "http://localhost:8080";

        private const string BootstrapScene = ScenesDir + "/Bootstrap.unity";
        private const string CityScene = ScenesDir + "/City.unity";
        private const string FrontierScene = ScenesDir + "/Frontier_01.unity";

        /// <summary>Marker path used to detect whether first-time setup has run.</summary>
        public static string FrontierScenePath => FrontierScene;

        [MenuItem("Project Ascension/Setup/Build All Scenes")]
        public static void BuildAllScenes()
        {
            EnsureFolder(ScenesDir);
            EnsureFolder(DataDir);
            EnsureFolder(WeaponsDir);

            // Create/ensure ALL assets first, then SaveAssets. The scene builders
            // load asset references themselves (immediately before assigning) so
            // they are never invalidated by these writes.
            GetOrCreatePlayerData();
            GetOrCreateStarterLoadout();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Preserve whatever the user currently has open and restore it afterwards
            // so this is safe to run automatically.
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildBootstrapScene();
                BuildCityScene();
                BuildFrontierScene();
                RegisterBuildScenes();
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Setup] Built Bootstrap, City, and Frontier_01. Open Frontier_01 and press Play.");
        }

        [MenuItem("Project Ascension/Setup/Create PlayerData Asset")]
        public static void CreatePlayerDataMenu()
        {
            EnsureFolder(DataDir);
            GetOrCreatePlayerData();
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath));
        }

        private static PlayerData GetOrCreatePlayerData()
        {
            var data = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<PlayerData>();
                AssetDatabase.CreateAsset(data, PlayerDataPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(PlayerDataPath);
                // Reload as the imported asset so scene references serialize correctly.
                data = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
                Debug.Log($"[Setup] Created {PlayerDataPath}");
            }

            if (data == null)
                Debug.LogError($"[Setup] Failed to load or create PlayerData at {PlayerDataPath}.");

            return data;
        }

        // The four starter weapons owned by the player. In the full game these come
        // from the inventory; here they are authored assets. StarterLoadout picks
        // two of them (the pre-chosen loadout — no in-field switching).
        private static LoadoutConfig GetOrCreateStarterLoadout()
        {
            // Create all weapons first, then save once, then load fresh references.
            // (Each CreateWeapon must not hold a reference across other writes.)
            CreateWeapon("Sword", "Sword", EquipmentType.Weapon, SlotType.Either, damage: 25f, range: 2.2f, projectileSpeed: 0f, cooldown: 0.5f);
            // Bow: a two-handed charge weapon (hold to draw, release to loose) whose arrows arc.
            CreateWeapon("Bow", "Bow", EquipmentType.Bow, SlotType.TwoHand, damage: 18f, range: 60f, projectileSpeed: 28f, cooldown: 0.6f, chargeTime: 0.8f, maxChargeMultiplier: 2.5f, projectileGravity: 9.8f);
            // Pistol: a firearm whose accuracy blooms under sustained fire (spread).
            CreateWeapon("Pistol", "Pistol", EquipmentType.Firearm, SlotType.Either, damage: 12f, range: 60f, projectileSpeed: 0f, cooldown: 0.25f,
                spreadMin: 1f, spreadMax: 9f, spreadPerShot: 1.4f, spreadRecovery: 7f);
            CreateWeapon("Catalyst", "Arcane Catalyst", EquipmentType.Catalyst, SlotType.Either, damage: 22f, range: 50f, projectileSpeed: 18f, cooldown: 0.8f);

            var config = AssetDatabase.LoadAssetAtPath<LoadoutConfig>(LoadoutConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LoadoutConfig>();
                AssetDatabase.CreateAsset(config, LoadoutConfigPath);
            }
            AssetDatabase.SaveAssets();

            var sword = AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponsDir}/Sword.asset");
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponsDir}/Pistol.asset");

            var so = new SerializedObject(config);
            so.FindProperty("left").objectReferenceValue = sword;
            so.FindProperty("right").objectReferenceValue = pistol;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<LoadoutConfig>(LoadoutConfigPath);
        }

        private static void CreateWeapon(string assetName, string displayName, EquipmentType equipmentType, SlotType slotType,
            float damage, float range, float projectileSpeed, float cooldown,
            float chargeTime = 0f, float maxChargeMultiplier = 1f,
            float spreadMin = 0f, float spreadMax = 0f, float spreadPerShot = 0f, float spreadRecovery = 0f,
            float projectileGravity = 0f)
        {
            var path = $"{WeaponsDir}/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(data, path);
            }
            var so = new SerializedObject(data);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("equipmentType").enumValueIndex = (int)equipmentType;
            so.FindProperty("slotType").enumValueIndex = (int)slotType;
            so.FindProperty("damage").floatValue = damage;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            so.FindProperty("projectileGravity").floatValue = projectileGravity;
            so.FindProperty("cooldown").floatValue = cooldown;
            so.FindProperty("chargeTime").floatValue = chargeTime;
            so.FindProperty("maxChargeMultiplier").floatValue = maxChargeMultiplier;
            so.FindProperty("spreadMin").floatValue = spreadMin;
            so.FindProperty("spreadMax").floatValue = spreadMax;
            so.FindProperty("spreadPerShot").floatValue = spreadPerShot;
            so.FindProperty("spreadRecovery").floatValue = spreadRecovery;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static WeaponData[] LoadWeapons() => new[]
        {
            AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponsDir}/Sword.asset"),
            AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponsDir}/Pistol.asset"),
            AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponsDir}/Bow.asset"),
            AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponsDir}/Catalyst.asset"),
        };

        private static void SetObjectArray(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Setup] Array field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateTrigger(string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Collider>().isTrigger = true;
            SetColorField(go.AddComponent<Tint>(), "color", color);
            return go;
        }

        private static void SetColorField(Object target, string fieldName, Color color)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("RootLifetimeScope");
            root.AddComponent<RootLifetimeScope>();

            var bootstrapGo = new GameObject("Bootstrap");
            bootstrapGo.AddComponent<Bootstrap>();

            // Cross-scene game state (contracts, currency, loadout selection).
            var sessionGo = new GameObject("GameSession");
            var session = sessionGo.AddComponent<GameSession>();
            SetObjectArray(session, "ownedWeapons", LoadWeapons());
            SetStringField(session, "serverUrl", DevServerUrl); // enable DB-driven balance fetch

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootstrapScene);
        }

        private static void BuildCityScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // The city is a PLACE, not a menu (docs/03-gameplay/first-hour-experience.md): the player
            // walks to the training ground, the board, and the people. CityBlockout builds it
            // procedurally at load — 훈련장 / 게시판 / 안전 구역, the three things stage 1 requires.
            new GameObject("CityBlockout").AddComponent<CityBlockout>();

            BuildPlayerStack(CityBlockout.PlayerSpawn);

            var hub = new GameObject("CityHub");
            hub.AddComponent<CityHub>();

            // The first discovery happens in the TRAINING GROUND — which is inside the city. So the
            // discovery stack has to live here too, not only in the frontier, or stage 4 can never fire.
            var reporter = new GameObject("DiscoveryReporter").AddComponent<DiscoveryReporter>();
            SetStringField(reporter, "serverUrl", DevServerUrl);
            new GameObject("DiscoverySkillBinder").AddComponent<DiscoverySkillBinder>();
            new GameObject("DiscoveryNotification").AddComponent<DiscoveryNotification>();
            new GameObject("ContractHud").AddComponent<ContractHud>();

            // VContainer scope for the player stack (input/movement/camera) — the city needs it now
            // that it has a player to drive.
            var scopeGo = new GameObject("CityLifetimeScope");
            var scope = scopeGo.AddComponent<FrontierLifetimeScope>();
            SetObjectField(scope, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            SetObjectField(scope, "playerData", AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CityScene);
        }

        /// <summary>The full player stack (body, camera, hands, loadout, combat, focus, discovery).
        /// Both the City and the Frontier need it: the city is a PLACE the player walks around, not a
        /// menu — the first hour has them walk to the training ground, the board, and the people.</summary>
        private static GameObject BuildPlayerStack(Vector3 spawn)
        {
            // Player.
            var player = new GameObject("Player");
            player.transform.position = spawn;
            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.5f;

            var bodyMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyMesh.name = "Body";
            Object.DestroyImmediate(bodyMesh.GetComponent<Collider>());
            bodyMesh.transform.SetParent(player.transform, false);
            bodyMesh.transform.localPosition = new Vector3(0f, 1f, 0f);

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var vcamGo = new GameObject("PlayerVCam");
            vcamGo.transform.SetParent(pivot.transform, false);
            vcamGo.transform.localPosition = Vector3.zero;
            vcamGo.transform.localRotation = Quaternion.identity;
            vcamGo.AddComponent<CinemachineCamera>();

            var playerController = player.AddComponent<PlayerController>();
            SetObjectField(playerController, "cameraPivot", pivot.transform);

            // Hand anchors under the camera pivot (view-locked) + Loadout that
            // equips the pre-chosen pair on spawn.
            var leftHand = new GameObject("LeftHand");
            leftHand.transform.SetParent(pivot.transform, false);
            leftHand.transform.localPosition = new Vector3(-0.3f, -0.25f, 0.4f);
            var rightHand = new GameObject("RightHand");
            rightHand.transform.SetParent(pivot.transform, false);
            rightHand.transform.localPosition = new Vector3(0.3f, -0.25f, 0.4f);

            var loadout = player.AddComponent<Loadout>();
            SetObjectField(loadout, "leftHand", leftHand.transform);
            SetObjectField(loadout, "rightHand", rightHand.transform);
            // config left null: the loadout is driven by LoadoutApplier (City selection,
            // or the StarterLoadout fallback when played directly).
            var loadoutConfig = AssetDatabase.LoadAssetAtPath<LoadoutConfig>(LoadoutConfigPath);
            var applier = new GameObject("LoadoutApplier").AddComponent<LoadoutApplier>();
            SetObjectField(applier, "fallback", loadoutConfig);

            // Combat: player is damageable ("Player" tag for monsters to find) and
            // can attack with the equipped weapons.
            player.tag = "Player";
            var playerHealth = player.AddComponent<HitReceiver>();
            playerHealth.SetMaxHealth(100f);
            var playerCombat = player.AddComponent<PlayerCombat>();
            SetObjectField(playerCombat, "loadout", loadout);
            SetObjectField(playerCombat, "aimSource", pivot.transform);

            // Focus (the resource discovered skills spend).
            player.AddComponent<FocusPool>();

            // Server discovery → skill: SkillCaster fetches a fired discovery's composed
            // skill and mints/equips it; DiscoveryReporter posts behavior to the trigger;
            // the binder connects the two. Without this the discovery→weapon loop is dead.
            var skillCaster = player.AddComponent<SkillCaster>();
            SetStringField(skillCaster, "serverUrl", DevServerUrl);
            SetObjectField(skillCaster, "aimSource", pivot.transform);

            // Main Camera gets the Cinemachine brain.
            var mainCam = FindMainCamera();
            if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
                mainCam.gameObject.AddComponent<CinemachineBrain>();

            return player;
        }

        private static void BuildFrontierScene()
        {
            // Asset references are (re)loaded immediately before each SetObjectField
            // below. Loading earlier and holding a reference is unreliable: any
            // intervening AssetDatabase write or scene op can invalidate it, which
            // then serializes as null (fileID: 0) in the scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Ground (50 x 50, top at y = 0 to match the simulation's ground plane).
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);

            // A couple of blocks so movement/camera are visibly readable.
            CreateBlock("Block_A", new Vector3(4f, 1f, 6f));
            CreateBlock("Block_B", new Vector3(-5f, 1f, 3f));

            BuildPlayerStack(Vector3.zero);
            // Monsters: spawner drops the 3 types around the origin on play.
            var spawnerGo = new GameObject("MonsterSpawner");
            spawnerGo.transform.position = Vector3.zero;
            spawnerGo.AddComponent<MonsterSpawner>();

            // Contract HUD (objective + progress).
            new GameObject("ContractHud").AddComponent<ContractHud>();

            // Discovery is server-authoritative: DiscoveryReporter posts behavior, the composed
            // skill loads, and DiscoveryNotification toasts its server-composed name.
            new GameObject("DiscoveryNotification").AddComponent<DiscoveryNotification>();

            // Server discovery reporter + the binder that mints a fired discovery's skill.
            var reporter = new GameObject("DiscoveryReporter").AddComponent<DiscoveryReporter>();
            SetStringField(reporter, "serverUrl", DevServerUrl);
            new GameObject("DiscoverySkillBinder").AddComponent<DiscoverySkillBinder>();

            // Objectives: collectibles (Collection), a survey marker (Survey), and a
            // green return pad (step on it to go back to the City).
            var sample = new Color(0.3f, 1f, 0.4f);
            CreateTrigger("Collectible_1", PrimitiveType.Sphere, new Vector3(6f, 1f, 2f), Vector3.one * 0.6f, sample).AddComponent<Collectible>();
            CreateTrigger("Collectible_2", PrimitiveType.Sphere, new Vector3(-4f, 1f, 7f), Vector3.one * 0.6f, sample).AddComponent<Collectible>();
            CreateTrigger("Collectible_3", PrimitiveType.Sphere, new Vector3(7f, 1f, -3f), Vector3.one * 0.6f, sample).AddComponent<Collectible>();
            CreateTrigger("SurveyPoint", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 12f), new Vector3(1f, 1.5f, 1f), new Color(0.3f, 0.6f, 1f)).AddComponent<SurveyPoint>();
            CreateTrigger("ReturnPad", PrimitiveType.Cube, new Vector3(0f, 0.1f, -5f), new Vector3(3f, 0.2f, 3f), new Color(0.2f, 0.9f, 0.3f)).AddComponent<ReturnZone>();

            // VContainer scope for the player stack.
            var scopeGo = new GameObject("FrontierLifetimeScope");
            var scope = scopeGo.AddComponent<FrontierLifetimeScope>();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            var playerData = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
            Debug.Log($"[Setup] PlayerData load: {(playerData == null ? "NULL" : playerData.name)} | InputActions: {(inputActions == null ? "NULL" : inputActions.name)}");
            SetObjectField(scope, "inputActions", inputActions);
            SetObjectField(scope, "playerData", playerData);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, FrontierScene);
        }

        private static void CreateBlock(string name, Vector3 position)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = new Vector3(2f, 2f, 2f);
        }

        private static Camera FindMainCamera()
        {
            if (Camera.main != null) return Camera.main;
            return Object.FindAnyObjectByType<Camera>();
        }

        private static void RegisterBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new(BootstrapScene, true),
                new(CityScene, true),
                new(FrontierScene, true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Assigns a serialized object-reference field (incl. private [SerializeField]).</summary>
        private static void SetObjectField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Setup] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a serialized string field (incl. private [SerializeField]).</summary>
        private static void SetStringField(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Setup] String field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            var parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            var leaf = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
