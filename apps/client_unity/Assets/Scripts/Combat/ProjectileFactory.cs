using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>Spawns a projectile with composed VFX — a glowing bolt, a fading trail, a
    /// muzzle flash at the origin, and an impact burst on hit (see CombatVfx).</summary>
    public static class ProjectileFactory
    {
        public static void Spawn(AttackContext ctx, float speed, float damage, Color color, float radius = 0.12f, float gravity = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.localScale = Vector3.one * radius;
            go.transform.position = ctx.Origin + ctx.Direction.normalized * 0.6f;

            // No collider, no rigidbody: the bolt finds its own hits by sweeping the segment it flew
            // (see Projectile). A trigger volume would only re-introduce the spawn-overlap and
            // tunnelling bugs it used to have.
            var collider = go.GetComponent<Collider>();
            collider.enabled = false; // Destroy is deferred to end of frame; disable it NOW so it can't
            Object.Destroy(collider);  // block another bolt's sweep for the rest of this frame

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CombatVfx.Glow(color); // bright, URP-safe, blooms

            CombatVfx.AddTrail(go, color);
            CombatVfx.Burst(go.transform.position, color, 0.7f); // muzzle flash

            var projectile = go.AddComponent<Projectile>();
            projectile.SetImpactColor(color);
            projectile.Launch(ctx.Direction, speed, damage, ctx.Attacker, gravity: gravity, radius: radius * 0.5f);
        }
    }
}
