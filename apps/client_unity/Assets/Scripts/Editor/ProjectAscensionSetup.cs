using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ProjectAscension.Core;
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
        private const string PlayerDataPath = DataDir + "/PlayerData.asset";
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

            var playerData = GetOrCreatePlayerData();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
                Debug.LogWarning($"[Setup] Input actions not found at {InputActionsPath}; assign it on FrontierLifetimeScope manually.");

            // Preserve whatever the user currently has open and restore it afterwards
            // so this is safe to run automatically.
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildBootstrapScene();
                BuildCityScene();
                BuildFrontierScene(playerData, inputActions);
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

        private static void BuildFrontierScene(PlayerData playerData, InputActionAsset inputActions)
        {
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

            // Main Camera gets the Cinemachine brain.
            var mainCam = FindMainCamera();
            if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
                mainCam.gameObject.AddComponent<CinemachineBrain>();

            // VContainer scope for the player stack.
            var scopeGo = new GameObject("FrontierLifetimeScope");
            var scope = scopeGo.AddComponent<FrontierLifetimeScope>();
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
