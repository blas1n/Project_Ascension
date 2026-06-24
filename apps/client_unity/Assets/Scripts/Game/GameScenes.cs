using UnityEngine.SceneManagement;

namespace ProjectAscension.Game
{
    /// <summary>Scene names + transitions for the City &lt;-&gt; Frontier loop.</summary>
    public static class GameScenes
    {
        public const string City = "City";
        public const string Frontier = "Frontier_01";

        public static void LoadCity() => SceneManager.LoadScene(City);
        public static void LoadFrontier() => SceneManager.LoadScene(Frontier);
    }
}
