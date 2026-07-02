using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.GameSimulation.Combat;

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

        // ----- Per-primitive modifiers ---------------------------------------------------
        // The composed skill's PRIMITIVES layer extra accents on top of the delivery shape,
        // the same way they layer extra effects on top of the damage. A skill that chains
        // arcs between its targets; one that knocks back throws a shockwave; a lingering
        // damage-over-time leaves a hazard pool; a fork splits into streaks. Assembled from
        // the skill's own data — no per-skill asset.

        /// <summary>Reads a resolved skill's primitives and plays the matching impact accents
        /// at <paramref name="impact"/>. Homing is a flight accent (see <see cref="HomingAccent"/>),
        /// handled where the projectile is spawned.</summary>
        public static void PlayImpactModifiers(Skill skill, Vector3 impact, IReadOnlyList<Vector3> targets, Vector3 casterPos, float intensity)
        {
            if (skill == null) return;
            var color = ElementColor(skill.Name);

            bool chain = false, fork = false, knockback = false, leech = false;
            int dotDuration = -1;
            foreach (var p in skill.Primitives)
            {
                switch (p.Kind)
                {
                    case SkillPrimitiveKind.Chain: chain = true; break;
                    case SkillPrimitiveKind.Fork: fork = true; break;
                    case SkillPrimitiveKind.Knockback: knockback = true; break;
                    case SkillPrimitiveKind.Leech: leech = true; break;
                    case SkillPrimitiveKind.DamageOverTime: dotDuration = Mathf.Max(dotDuration, p.Duration); break;
                }
            }

            if (knockback) Shockwave(impact, color, intensity);
            if (dotDuration >= 0) Lingering(impact, color, 1.3f * intensity, intensity, 1.6f + dotDuration * 0.8f);
            if (fork) Fork(impact, color, intensity);
            if (chain && targets != null) ChainThrough(impact, targets, color, intensity);
            if (leech) Beam(impact, casterPos, color, intensity * 0.5f); // a faint tether draining back
        }

        /// <summary>A flat expanding ring on the ground — a knockback's shockwave.</summary>
        public static void Shockwave(Vector3 center, Color color, float intensity)
        {
            var go = new GameObject("SkillVfx_Shockwave");
            go.transform.position = center + Vector3.up * 0.1f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // circle lies flat on the ground
            var ps = Configure(go, color, intensity);
            var main = ps.main;
            main.startSpeed = 8f * intensity;
            main.startSize = 0.35f * intensity;
            main.startLifetime = 0.35f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            shape.arc = 360f;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(40 * intensity)) });
            ps.Play();
            Object.Destroy(go, 0.8f);
        }

        /// <summary>A lingering hazard pool at a point — a damage-over-time's residue.</summary>
        public static void Lingering(Vector3 point, Color color, float radius, float intensity, float seconds)
        {
            var go = new GameObject("SkillVfx_Lingering");
            go.transform.position = point;
            var ps = Configure(go, color, intensity);
            var main = ps.main;
            main.duration = seconds;
            main.startSpeed = 0.4f;
            main.startSize = 0.3f * intensity;
            main.startLifetime = 0.9f;
            main.gravityModifier = -0.05f; // gentle upward drift
            var emission = ps.emission;
            emission.rateOverTime = 14f * intensity; // continuous, not a one-shot burst
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;
            ps.Play();
            Object.Destroy(go, seconds + 1.2f);
        }

        /// <summary>Streaks fanning out from the impact — a fork splitting.</summary>
        public static void Fork(Vector3 origin, Color color, float intensity)
        {
            float[] angles = { -35f, -12f, 12f, 35f };
            foreach (var a in angles)
            {
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Beam(origin, origin + dir * (1.6f * intensity), color, intensity * 0.7f);
            }
        }

        // Jagged arcs hopping impact -> target -> target — a chain lightning look.
        private static void ChainThrough(Vector3 impact, IReadOnlyList<Vector3> targets, Color color, float intensity)
        {
            var prev = impact;
            int hops = 0;
            foreach (var t in targets)
            {
                if ((t - impact).sqrMagnitude < 0.04f) continue; // skip the primary at the impact point
                ChainArc(prev, t, color, intensity);
                prev = t;
                if (++hops >= 4) break;
            }
        }

        /// <summary>A single jagged arc between two points.</summary>
        public static void ChainArc(Vector3 from, Vector3 to, Color color, float intensity)
        {
            var go = new GameObject("SkillVfx_ChainArc");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = Glow(color);
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = 0.07f * intensity;
            lr.numCapVertices = 2;
            const int segs = 6;
            lr.positionCount = segs + 1;
            var perp = Vector3.Cross((to - from).normalized, Vector3.up);
            for (int i = 0; i <= segs; i++)
            {
                var p = Vector3.Lerp(from, to, i / (float)segs);
                if (i != 0 && i != segs)
                    p += perp * (((i % 2 == 0) ? 1f : -1f) * 0.18f * intensity); // alternating jag
                lr.SetPosition(i, p);
            }
            Object.Destroy(go, 0.14f);
        }

        /// <summary>Curling motes left behind a homing projectile (call once, on the projectile).
        /// World-space simulation makes the motes hang in the air as a curving trail as it flies.</summary>
        public static void HomingAccent(GameObject projectile, Color color, float intensity)
        {
            if (projectile == null) return;
            var child = new GameObject("SkillVfx_HomingMotes");
            child.transform.SetParent(projectile.transform, false);
            var ps = child.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.duration = 4f;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 0.6f;
            main.startSize = 0.11f * intensity;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 36f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.material = Glow(color);
            ps.Play();
        }

        /// <summary>A control accent at a target — slow reads cold, stun sparks overhead,
        /// knockback a neutral puff. Kind-specific colour (clearer than the element colour).</summary>
        public static void ControlAccent(Vector3 point, ControlKind kind, float intensity)
        {
            switch (kind)
            {
                case ControlKind.Slow: Nova(point, new Color(0.4f, 0.85f, 1f), 0.8f, intensity * 0.8f); break;       // frost ring at the feet
                case ControlKind.Stun: Burst(point + Vector3.up * 1.6f, new Color(1f, 0.9f, 0.4f), intensity * 0.8f); break; // stars overhead
                case ControlKind.Knockback: Burst(point, new Color(0.9f, 0.9f, 1f), intensity); break;              // white push puff
            }
        }

        /// <summary>A protective shell of motes around the caster — a shield/barrier grant.</summary>
        public static void ShieldBubble(Vector3 center, float intensity)
        {
            var go = new GameObject("SkillVfx_Shield");
            go.transform.position = center;
            var ps = Configure(go, new Color(0.4f, 0.85f, 0.9f), intensity);
            var main = ps.main;
            main.startSpeed = 0f;       // motes hold on the shell → a bubble outline
            main.startSize = 0.18f * intensity;
            main.startLifetime = 0.6f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.1f;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(60 * intensity)) });
            ps.Play();
            Object.Destroy(go, 1.0f);
        }

        /// <summary>A motion streak along a dash/blink from start to end.</summary>
        public static void DashStreak(Vector3 from, Vector3 dir, float distance, float intensity)
        {
            var to = from + dir.normalized * distance;
            Beam(from, to, new Color(0.75f, 0.55f, 1f), intensity);
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
