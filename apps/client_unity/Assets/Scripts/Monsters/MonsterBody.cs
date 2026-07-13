using UnityEngine;

namespace ProjectAscension.Monsters
{
    /// <summary>
    /// Builds a monster's body procedurally. The art direction asks for readability and SILHOUETTE
    /// first (docs/05-art/art-direction.md) — and it matters mechanically, not just aesthetically: you
    /// have to recognise what is coming at you across a field, before it is close enough to read a
    /// colour. Three tinted capsules cannot do that. These can.
    ///
    /// - Melee: squat and heavy. Broad shoulders, low to the ground. It closes distance and hits you.
    /// - Ranged: lean and tall, one long limb held out. It stands off and shoots.
    /// - Elite: big, crowned, jagged. It should read as "not that, not yet" from across the arena.
    ///
    /// No authored art yet (the art track swaps meshes into this seam later). Child renderers only —
    /// the ROOT keeps its collider, and MonsterBase tints every renderer for the attack telegraph.
    /// </summary>
    public static class MonsterBody
    {
        public static void Build(GameObject root, MonsterType type, Color color)
        {
            // The root capsule stays for collision; it just stops being what you look at.
            var rootRenderer = root.GetComponent<Renderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            var dark = color * 0.7f; dark.a = 1f;
            var pale = Color.Lerp(color, Color.white, 0.35f);

            switch (type)
            {
                case MonsterType.Ranged: BuildRanged(root.transform, color, dark, pale); break;
                case MonsterType.Elite: BuildElite(root.transform, color, dark, pale); break;
                default: BuildMelee(root.transform, color, dark, pale); break;
            }
        }

        // Squat and wide — mass low, shoulders out. Reads as a charger.
        private static void BuildMelee(Transform root, Color body, Color dark, Color pale)
        {
            Part(root, "Torso", new Vector3(0f, 0.85f, 0f), new Vector3(1.05f, 0.9f, 0.8f), body);
            Part(root, "Shoulders", new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 0.35f, 0.85f), dark);
            Part(root, "Head", new Vector3(0f, 1.55f, 0.1f), new Vector3(0.55f, 0.45f, 0.55f), pale);
            Part(root, "Arm_L", new Vector3(-0.8f, 0.95f, 0.1f), new Vector3(0.3f, 0.8f, 0.3f), dark);
            Part(root, "Arm_R", new Vector3(0.8f, 0.95f, 0.1f), new Vector3(0.3f, 0.8f, 0.3f), dark);
            Part(root, "Leg_L", new Vector3(-0.3f, 0.3f, 0f), new Vector3(0.35f, 0.6f, 0.35f), dark);
            Part(root, "Leg_R", new Vector3(0.3f, 0.3f, 0f), new Vector3(0.35f, 0.6f, 0.35f), dark);
        }

        // Lean and tall, one long limb held forward — reads as a shooter, at a glance, at range.
        private static void BuildRanged(Transform root, Color body, Color dark, Color pale)
        {
            Part(root, "Torso", new Vector3(0f, 1.1f, 0f), new Vector3(0.6f, 1.2f, 0.5f), body);
            Part(root, "Head", new Vector3(0f, 1.9f, 0.05f), new Vector3(0.45f, 0.4f, 0.45f), pale);
            var arm = Part(root, "LongArm", new Vector3(0.45f, 1.35f, 0.45f), new Vector3(0.2f, 1.3f, 0.2f), dark);
            arm.transform.localRotation = Quaternion.Euler(72f, 0f, 0f); // held out, aiming
            Part(root, "Arm_L", new Vector3(-0.4f, 1.2f, 0f), new Vector3(0.2f, 0.9f, 0.2f), dark);
            Part(root, "Leg_L", new Vector3(-0.18f, 0.35f, 0f), new Vector3(0.22f, 0.7f, 0.22f), dark);
            Part(root, "Leg_R", new Vector3(0.18f, 0.35f, 0f), new Vector3(0.22f, 0.7f, 0.22f), dark);
        }

        // Big, crowned, asymmetric. It should say "not yet" from across the arena.
        private static void BuildElite(Transform root, Color body, Color dark, Color pale)
        {
            Part(root, "Torso", new Vector3(0f, 1.0f, 0f), new Vector3(1.2f, 1.3f, 1.0f), body);
            Part(root, "Shoulders", new Vector3(0f, 1.6f, 0f), new Vector3(1.9f, 0.4f, 1.1f), dark);
            Part(root, "Head", new Vector3(0f, 2.05f, 0.1f), new Vector3(0.6f, 0.5f, 0.6f), pale);

            // A crown of spines — the silhouette that marks it out.
            for (int i = 0; i < 5; i++)
            {
                var spine = Part(root, $"Spine_{i}", new Vector3(-0.6f + i * 0.3f, 2.35f, 0f),
                    new Vector3(0.12f, 0.7f, 0.12f), pale);
                spine.transform.localRotation = Quaternion.Euler(0f, 0f, -20f + i * 10f);
            }

            Part(root, "Arm_L", new Vector3(-1.0f, 1.1f, 0.1f), new Vector3(0.36f, 1.1f, 0.36f), dark);
            var maul = Part(root, "Arm_R", new Vector3(1.05f, 1.05f, 0.15f), new Vector3(0.5f, 1.2f, 0.5f), dark);
            Part(maul.transform, "Fist", new Vector3(0f, -0.75f, 0f), new Vector3(1.5f, 0.5f, 1.5f), body);

            Part(root, "Leg_L", new Vector3(-0.38f, 0.32f, 0f), new Vector3(0.45f, 0.65f, 0.45f), dark);
            Part(root, "Leg_R", new Vector3(0.38f, 0.32f, 0f), new Vector3(0.45f, 0.65f, 0.45f), dark);
        }

        private static GameObject Part(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>()); // the root's collider is the one that counts
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
            return go;
        }
    }
}
