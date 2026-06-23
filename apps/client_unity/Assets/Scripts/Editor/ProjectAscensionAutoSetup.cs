using UnityEditor;
using UnityEngine;

namespace ProjectAscension.Editor
{
    /// <summary>
    /// Runs the Phase 1 scene/asset setup automatically the first time the project
    /// is opened (or whenever the Frontier scene is missing), so no manual menu
    /// click is required. The build is non-destructive: it restores the scene the
    /// user had open. Re-run manually via "Project Ascension/Setup/Build All Scenes".
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectAscensionAutoSetup
    {
        static ProjectAscensionAutoSetup()
        {
            EditorApplication.delayCall += TrySetup;
        }

        private static void TrySetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Wait until imports/compiles settle before touching assets.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TrySetup;
                return;
            }

            // Frontier scene present => setup already done.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectAscensionSetup.FrontierScenePath) != null)
                return;

            Debug.Log("[Project Ascension] First-time setup: building scenes and PlayerData...");
            ProjectAscensionSetup.BuildAllScenes();
        }
    }
}
