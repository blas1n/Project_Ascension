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

        private void Start()
        {
            SceneManager.LoadScene(firstScene);
        }
    }
}
