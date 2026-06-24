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
            CreateWeapon("Bow", "Bow", EquipmentType.Bow, SlotType.Either, damage: 18f, range: 60f, projectileSpeed: 28f, cooldown: 0.6f);
            CreateWeapon("Pistol", "Pistol", EquipmentType.Firearm, SlotType.Either, damage: 12f, range: 60f, projectileSpeed: 0f, cooldown: 0.25f);
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
            float damage, float range, float projectileSpeed, float cooldown)
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
            so.FindProperty("cooldown").floatValue = cooldown;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("RootLifetimeScope");
            root.AddComponent<RootLifetimeScope>();

            var bootstrapGo = new GameObject("Bootstrap");
            bootstrapGo.AddComponent<Bootstrap>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootstrapScene);
        }

        private static void BuildCityScene()
        {
            // Placeholder hub for this phase.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CityScene);
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

            // Player.
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;
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
            var loadoutConfig = AssetDatabase.LoadAssetAtPath<LoadoutConfig>(LoadoutConfigPath);
            Debug.Log($"[Setup] LoadoutConfig load: {(loadoutConfig == null ? "NULL" : loadoutConfig.name)}");
            SetObjectField(loadout, "config", loadoutConfig);

            // Combat: player is damageable ("Player" tag for monsters to find) and
            // can attack with the equipped weapons.
            player.tag = "Player";
            var playerHealth = player.AddComponent<HitReceiver>();
            playerHealth.SetMaxHealth(100f);
            var playerCombat = player.AddComponent<PlayerCombat>();
            SetObjectField(playerCombat, "loadout", loadout);
            SetObjectField(playerCombat, "aimSource", pivot.transform);

            // Main Camera gets the Cinemachine brain.
            var mainCam = FindMainCamera();
            if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
                mainCam.gameObject.AddComponent<CinemachineBrain>();

            // Monsters: spawner drops the 3 types around the origin on play.
            var spawnerGo = new GameObject("MonsterSpawner");
            spawnerGo.transform.position = Vector3.zero;
            spawnerGo.AddComponent<MonsterSpawner>();

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
