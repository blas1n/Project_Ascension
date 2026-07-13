using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Builds the frontier procedurally at load: the OUTSKIRTS you are sent to survey, a threshold, and
    /// the DEEP frontier beyond it. The two have to look like different worlds, because the first hour
    /// turns on the player misreading the second one as more of the first
    /// (docs/03-gameplay/first-hour-experience.md, stages 6–8).
    ///
    /// Outskirts: open grassland, ruins, survivable — the same green world the city sits in.
    /// Deep: colder, darker, jagged. Nothing here is built by people. It should feel like a place that
    /// does not care whether you leave.
    ///
    /// Procedural (no authored art yet — the art track swaps meshes into these seams later) and
    /// deterministic, so the ground you charted stays the ground you charted.
    /// </summary>
    public sealed class FrontierBlockout : MonoBehaviour
    {
        // Outskirts — the world still recognisably the city's.
        private static readonly Color Grass = new Color(0.40f, 0.58f, 0.34f);
        private static readonly Color Rock = new Color(0.55f, 0.55f, 0.52f);
        private static readonly Color Ruin = new Color(0.72f, 0.70f, 0.64f);
        // Deep — drained of it.
        private static readonly Color DeepGround = new Color(0.22f, 0.24f, 0.28f);
        private static readonly Color DeepRock = new Color(0.30f, 0.31f, 0.36f);
        private static readonly Color DeepBone = new Color(0.60f, 0.60f, 0.58f);
        private static readonly Color ThresholdGlow = new Color(0.65f, 0.35f, 0.35f);

        /// <summary>Where the survey marker belongs — out in the outskirts, a walk from the gate.</summary>
        public static readonly Vector3 SurveyMarker = new Vector3(0f, 0f, 34f);
        /// <summary>The pass into the deep frontier. Unmapped ground: you cannot find it without the chart.</summary>
        public static readonly Vector3 DeepThreshold = new Vector3(0f, 0f, 56f);
        /// <summary>The deep arena — where the thing that kills you is waiting (staged in the next pass).</summary>
        public static readonly Vector3 DeepArena = new Vector3(0f, 0f, 78f);

        private void Awake()
        {
            var root = new GameObject("FrontierBlockout_Generated").transform;
            root.SetParent(transform, false);

            Outskirts(root);
            Threshold(root);
            Deep(root);
        }

        private static void Outskirts(Transform root)
        {
            Box(root, "OutskirtsGround", new Vector3(0f, -0.05f, 22f), new Vector3(90f, 0.1f, 70f), Grass);

            // Ruins — people were here once, and are not any more. The outskirts are survivable, not safe.
            Box(root, "Ruin_Wall_A", new Vector3(-14f, 1.6f, 14f), new Vector3(8f, 3.2f, 0.8f), Ruin);
            Box(root, "Ruin_Wall_B", new Vector3(-18f, 1.2f, 18f), new Vector3(0.8f, 2.4f, 7f), Ruin);
            Box(root, "Ruin_Pillar", new Vector3(-10f, 2f, 20f), new Vector3(1.2f, 4f, 1.2f), Ruin);
            Box(root, "Ruin_Wall_C", new Vector3(16f, 1.4f, 24f), new Vector3(9f, 2.8f, 0.8f), Ruin);

            // Outcrops to break sightlines, so the ground has to be walked to be known.
            Rocks(root, "Out", new[]
            {
                new Vector3(-22f, 0f, 6f), new Vector3(20f, 0f, 8f), new Vector3(-8f, 0f, 28f),
                new Vector3(12f, 0f, 32f), new Vector3(26f, 0f, 18f), new Vector3(-26f, 0f, 24f),
            }, Rock, 2.5f);
        }

        /// <summary>The pass. A cleft between cliffs, closed by a gate you cannot read your way through
        /// without the chart — the map's whole reason to exist.</summary>
        private static void Threshold(Transform root)
        {
            Box(root, "Cliff_W", DeepThreshold + new Vector3(-14f, 5f, 0f), new Vector3(22f, 10f, 8f), Rock);
            Box(root, "Cliff_E", DeepThreshold + new Vector3(14f, 5f, 0f), new Vector3(22f, 10f, 8f), Rock);
            Box(root, "PassMarker_L", DeepThreshold + new Vector3(-3f, 1.5f, 0f), new Vector3(0.6f, 3f, 0.6f), DeepBone);
            Box(root, "PassMarker_R", DeepThreshold + new Vector3(3f, 1.5f, 0f), new Vector3(0.6f, 3f, 0.6f), DeepBone);
            // A line on the ground: past here, the world changes.
            Box(root, "PassLine", DeepThreshold + new Vector3(0f, 0.03f, 0f), new Vector3(6f, 0.06f, 0.6f), ThresholdGlow);

            var gate = new GameObject("DeepZoneGate");
            gate.transform.SetParent(root, false);
            gate.transform.localPosition = DeepThreshold + new Vector3(0f, 1.5f, 0f);
            var box = gate.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(6f, 3f, 2f);
            gate.AddComponent<DeepZoneGate>();
        }

        /// <summary>The deep frontier. Read as more of the same at a glance; is not.</summary>
        private static void Deep(Transform root)
        {
            Box(root, "DeepGround", DeepArena + new Vector3(0f, -0.05f, 0f), new Vector3(70f, 0.1f, 60f), DeepGround);

            Rocks(root, "Deep", new[]
            {
                DeepArena + new Vector3(-18f, 0f, -12f), DeepArena + new Vector3(17f, 0f, -10f),
                DeepArena + new Vector3(-14f, 0f, 14f), DeepArena + new Vector3(15f, 0f, 16f),
                DeepArena + new Vector3(-24f, 0f, 2f), DeepArena + new Vector3(23f, 0f, 4f),
            }, DeepRock, 4.5f);

            // Bones the size of buildings. Something large has died here; something larger killed it.
            for (int i = 0; i < 5; i++)
            {
                var rib = Box(root, $"Rib_{i}", DeepArena + new Vector3(-6f + i * 3f, 2.5f, 6f),
                    new Vector3(0.5f, 5f, 0.5f), DeepBone);
                rib.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 14f : -14f);
            }

            // The arena floor — a flat place to be caught in the open.
            Box(root, "ArenaFloor", DeepArena + new Vector3(0f, 0.01f, 0f), new Vector3(26f, 0.04f, 26f), DeepRock);
        }

        private static void Rocks(Transform root, string tag, Vector3[] at, Color color, float scale)
        {
            for (int i = 0; i < at.Length; i++)
            {
                float h = scale * (1f + 0.35f * ((i % 3) - 1)); // varied, but fixed — the same crags every run
                var r = Box(root, $"{tag}Rock_{i}", at[i] + new Vector3(0f, h * 0.5f, 0f),
                    new Vector3(scale, h, scale), color);
                r.transform.localRotation = Quaternion.Euler(0f, i * 37f, i % 2 == 0 ? 6f : -6f);
            }
        }

        private static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
            return go;
        }
    }
}
