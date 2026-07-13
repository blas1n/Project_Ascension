using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// Composed VFX for the basic (starter) weapons — a URP-safe glow material, a projectile
    /// trail, a hitscan tracer beam, and a muzzle/impact burst. The starters previously drew
    /// only a bare primitive (or a debug cube), so a shot barely read; these give them the
    /// same stylized, bloom-catching look as the discovered-skill VFX. Mirrors the Game-layer
    /// SkillVfx; kept in the Combat assembly so it needs no reference back to Game.
    /// </summary>
    public static class CombatVfx
    {
        private const float HdrBoost = 1.5f; // push the core past 1.0 so URP bloom catches it

        // One material per colour, not one per SHOT. Minting a Material (and hunting the shader) on
        // every trigger pull is a per-shot allocation whose first hitch is measured in tenths of a
        // second — and a frame that long is how a projectile ends up teleporting into a wall.
        private static readonly System.Collections.Generic.Dictionary<Color, Material> Materials = new();

        /// <summary>A bright unlit material (URP-safe, HDR) for bolts / trails / tracers. Cached per colour.</summary>
        public static Material Glow(Color color)
        {
            if (Materials.TryGetValue(color, out var cached) && cached != null) return cached;

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(color.r * HdrBoost, color.g * HdrBoost, color.b * HdrBoost, color.a);
            Materials[color] = mat;
            return mat;
        }

        /// <summary>A fading trail behind a projectile.</summary>
        public static void AddTrail(GameObject go, Color color, float width = 0.14f)
        {
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.material = Glow(color);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        /// <summary>An instant beam from -> to (a hitscan tracer).</summary>
        public static void Tracer(Vector3 from, Vector3 to, Color color, float width = 0.05f)
        {
            var go = new GameObject("CombatVfx_Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = Glow(color);
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = width;
            lr.numCapVertices = 2;
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            Object.Destroy(go, 0.08f);
        }

        /// <summary>A short particle burst — a muzzle flash, an impact, a melee swing.</summary>
        public static void Burst(Vector3 point, Color color, float scale = 1f)
        {
            var go = new GameObject("CombatVfx_Burst");
            go.transform.position = point;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 5f * scale;
            main.startSize = 0.2f * scale;
            main.startColor = color;
            main.gravityModifier = 0.1f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(18 * scale)) });
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = Glow(color);
            ps.Play();
            Object.Destroy(go, 1.0f);
        }
    }
}
