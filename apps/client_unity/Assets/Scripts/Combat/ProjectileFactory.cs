using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>Spawns a placeholder projectile (no art yet).</summary>
    public static class ProjectileFactory
    {
        public static void Spawn(AttackContext ctx, float speed, float damage, Color color, float radius = 0.12f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.localScale = Vector3.one * radius;
            go.transform.position = ctx.Origin + ctx.Direction.normalized * 0.6f;

            go.GetComponent<Collider>().isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;

            go.AddComponent<Projectile>().Launch(ctx.Direction, speed, damage, ctx.Attacker);
        }
    }
}
