using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>Colors the renderer at runtime (set in the editor without leaking materials).</summary>
    public sealed class Tint : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;

        private void Awake()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }
    }
}
