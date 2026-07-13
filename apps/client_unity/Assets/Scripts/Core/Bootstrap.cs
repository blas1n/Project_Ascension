using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAscension.Core
{
    /// <summary>
    /// Entry point that lives in the Bootstrap scene. Once the RootLifetimeScope
    /// is up, it loads the first gameplay scene. The Player Controller test scene
    /// (Frontier_01) can also be played directly without going through Bootstrap.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string firstScene = "City";

        private void Awake()
        {
            DisableRuntimeDebugUi();
        }

        private void Start()
        {
            SceneManager.LoadScene(firstScene);
        }

        // BUG 4: URP's built-in Rendering Debugger overlay (default shortcut Ctrl+Backspace;
        // gamepad L3+R3) has no discoverable way to close it once summoned — a player who hits it
        // by accident is stuck looking at frame-stats/settings with no way out. It is not our UI;
        // it must simply never be reachable. Wrapped in a null check (not a platform #if — the
        // Core RP Library that owns DebugManager is a transitive dependency of URP, which this
        // project always uses) so a build/tooling context where the manager hasn't initialized
        // yet still compiles and runs cleanly instead of throwing.
        private static void DisableRuntimeDebugUi()
        {
            var debugManager = UnityEngine.Rendering.DebugManager.instance;
            if (debugManager != null) debugManager.enableRuntimeUI = false;
        }
    }
}
