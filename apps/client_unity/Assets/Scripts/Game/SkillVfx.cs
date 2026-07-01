using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Composes a discovered skill's VISUAL the same way the skill itself is composed — from a
    /// small library of parts, assembled per skill, so the infinitely many discoveries all get
    /// a readable, stylized look without a per-skill asset. The delivery decides the SHAPE
    /// (projectile / beam / burst / nova / arc), the skill's theme decides the ELEMENT COLOR
    /// (AI flavor -> palette), and its power decides the INTENSITY (common subtle -> legendary
    /// grand). Procedural / URP-friendly for now (bright unlit trails, lines, particles); real
    /// VFX-Graph assets can replace these later behind the same composition seam.
    ///
    /// Art direction: Stylized Realism, readability first (docs/05-art) — bright silhouettes,
    /// strong colour, the mage civilisation's purple/teal as the default.
    /// </summary>
    public static class SkillVfx
    {
        // Theme keyword -> element colour. Defaults to the mage-civilisation arcane purple.
        private static readonly (string Key, Color Color)[] Palette =
        {
            ("void", new Color(0.55f, 0.2f, 0.9f)), ("shadow", new Color(0.45f, 0.2f, 0.7f)), ("phantom", new Color(0.5f, 0.3f, 0.85f)),
            ("infernal", new Color(1f, 0.35f, 0.1f)), ("flame", new Color(1f, 0.45f, 0.12f)), ("searing", new Color(1f, 0.5f, 0.15f)), ("ember", new Color(1f, 0.4f, 0.1f)),
            ("frost", new Color(0.35f, 0.8f, 1f)), ("glacial", new Color(0.4f, 0.85f, 1f)), ("ice", new Color(0.45f, 0.85f, 1f)),
            ("storm", new Color(0.35f, 0.55f, 1f)), ("thunder", new Color(0.5f, 0.6f, 1f)), ("lightning", new Color(0.6f, 0.7f, 1f)),
            ("radian", new Color(1f, 0.85f, 0.4f)), ("divine", new Color(1f, 0.9f, 0.5f)), ("celestial", new Color(1f, 0.88f, 0.55f)), ("astral", new Color(0.7f, 0.85f, 1f)),
            ("arcane", new Color(0.8f, 0.4f, 1f)), ("ethereal", new Color(0.7f, 0.5f, 1f)), ("aether", new Color(0.75f, 0.55f, 1f)),
        };

        private static readonly Color Default = new Color(0.75f, 0.45f, 1f); // arcane

        /// <summary>The skill's element colour, inferred from its AI-composed name/theme.</summary>
        public static Color ElementColor(string skillName)
        {
            if (!string.IsNullOrEmpty(skillName))
            {
                var lower = skillName.ToLowerInvariant();
                foreach (var (key, color) in Palette)
                    if (lower.Contains(key)) return color;
            }
            return Default;
        }

        /// <summary>How grand the effect looks, from its power cost (common ~ subtle,
        /// legendary ~ grand). Maps to a ~0.8..1.8 scale on size / particle counts.</summary>
        public static float Intensity(int powerCost) => Mathf.Clamp(0.8f + powerCost / 60f, 0.8f, 1.8f);

        // A bright, unlit material for trails/lines/particles (URP-safe, reads as a glow with
        // bloom on; still stylised without it).
        public static Material Glow(Color color)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            return mat;
        }

        /// <summary>An instant beam of light from origin to point (delivery: beam).</summary>
        public static void Beam(Vector3 from, Vector3 to, Color color, float intensity)
        {
            var go = new GameObject("SkillVfx_Beam");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = Glow(color);
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = 0.10f * intensity;
            lr.numCapVertices = 4;
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            Object.Destroy(go, 0.09f);
        }

        /// <summary>An eruption at a point (delivery: burst / any impact).</summary>
        public static void Burst(Vector3 point, Color color, float intensity)
        {
            var go = new GameObject("SkillVfx_Burst");
            go.transform.position = point;
            var ps = Configure(go, color, intensity);
            var main = ps.main;
            main.startSpeed = 6f * intensity;
            main.startSize = 0.25f * intensity;
            var burst = ps.emission;
            burst.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(24 * intensity)) });
            ps.Play();
            Object.Destroy(go, 1.2f);
        }

        /// <summary>A ring erupting around a centre (delivery: nova, around the caster).</summary>
        public static void Nova(Vector3 center, Color color, float radius, float intensity)
        {
            var go = new GameObject("SkillVfx_Nova");
            go.transform.position = center;
            var ps = Configure(go, color, intensity);
            var main = ps.main;
            main.startSpeed = radius * 3f;
            main.startSize = 0.3f * intensity;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            shape.arc = 360f;
            var burst = ps.emission;
            burst.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(48 * intensity)) });
            ps.Play();
            Object.Destroy(go, 1.2f);
        }

        // A short-lived additive-ish particle burst, coloured + scaled by intensity.
        private static ParticleSystem Configure(GameObject go, Color color, float intensity)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startColor = color;
            main.gravityModifier = 0.1f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = Glow(color);
            return ps;
        }
    }
}
