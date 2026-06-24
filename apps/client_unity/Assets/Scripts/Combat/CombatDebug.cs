using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// Spawns short-lived primitives so attack volumes are visible in the Game view
    /// (Debug.DrawRay only shows in the Scene view). Toggle off via Enabled.
    /// </summary>
    public static class CombatDebug
    {
        public static bool Enabled = true;

        /// <summary>A thin bar from -> to (e.g. a hitscan tracer).</summary>
        public static void Tracer(Vector3 from, Vector3 to, Color color, float duration = 0.06f)
        {
            if (!Enabled) return;

            var dir = to - from;
            var dist = dir.magnitude;
            var go = MakePrimitive(PrimitiveType.Cube, color, duration);
            go.name = "DebugTracer";
            go.transform.position = from + dir * 0.5f;
            if (dist > 0.0001f) go.transform.rotation = Quaternion.LookRotation(dir);
            go.transform.localScale = new Vector3(0.03f, 0.03f, dist);
        }

        /// <summary>A sphere marking an overlap/hit volume (e.g. a melee swing).</summary>
        public static void Sphere(Vector3 center, float radius, Color color, float duration = 0.12f)
        {
            if (!Enabled) return;

            var go = MakePrimitive(PrimitiveType.Sphere, color, duration);
            go.name = "DebugSphere";
            go.transform.position = center;
            go.transform.localScale = Vector3.one * (radius * 2f);
        }

        private static GameObject MakePrimitive(PrimitiveType type, Color color, float duration)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>()); // never affect physics queries
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
            Object.Destroy(go, duration);
            return go;
        }
    }
}
