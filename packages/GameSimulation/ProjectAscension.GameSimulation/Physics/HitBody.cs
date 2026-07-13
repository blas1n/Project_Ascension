using System;
using System.Numerics;

namespace ProjectAscension.GameSimulation.Physics
{
    /// <summary>The three primitive shapes the collision world understands (ADR 0013).
    /// Everything gameplay swings, fires, or stands on is one of these.</summary>
    public enum HitBodyKind
    {
        Sphere,
        Capsule,
        Box,
    }

    /// <summary>
    /// A single collidable body in the <see cref="CollisionWorld"/>: the simulation's own
    /// description of a piece of the world, independent of whatever engine renders it. Every
    /// gameplay collider (player, monster, static blockout geometry) registers ONE of these; a
    /// body that isn't registered does not exist to the game (ADR 0013) — it cannot stop a
    /// bullet no matter how solid it looks on screen.
    ///
    /// <see cref="ActorId"/> 0 means static level geometry (nobody's body); a nonzero actor id is
    /// whoever owns this body (the player, a monster) and is what <c>ignoreActorId</c> excludes.
    /// </summary>
    public readonly struct HitBody
    {
        public int Id { get; }
        public int ActorId { get; }
        public HitBodyKind Kind { get; }

        /// <summary>World-space centre of the body (sphere centre, capsule midpoint, box centre).</summary>
        public Vector3 Center { get; }

        /// <summary>Sphere radius, or capsule radius. Unused (0) for a box.</summary>
        public float Radius { get; }

        /// <summary>Capsule only: half the length of the inner LINE segment between the two
        /// hemisphere centres (i.e. (height - 2*radius) / 2, floored at 0 for a "sphere-shaped"
        /// capsule shorter than its own radius). 0 for sphere/box.</summary>
        public float CapsuleHalfLine { get; }

        /// <summary>Capsule only: unit direction of the capsule's long axis in world space.
        /// Defaults to +Y (upright) for sphere/box, where it is unused.</summary>
        public Vector3 CapsuleAxis { get; }

        /// <summary>Box only: half-extents along its own local axes. Zero for sphere/capsule.</summary>
        public Vector3 BoxHalfExtents { get; }

        /// <summary>Box only: world orientation. Identity for sphere/capsule.</summary>
        public Quaternion BoxRotation { get; }

        private HitBody(int id, int actorId, HitBodyKind kind, Vector3 center, float radius,
            float capsuleHalfLine, Vector3 capsuleAxis, Vector3 boxHalfExtents, Quaternion boxRotation)
        {
            Id = id;
            ActorId = actorId;
            Kind = kind;
            Center = center;
            Radius = radius;
            CapsuleHalfLine = capsuleHalfLine;
            CapsuleAxis = capsuleAxis;
            BoxHalfExtents = boxHalfExtents;
            BoxRotation = boxRotation;
        }

        public static HitBody Sphere(int id, int actorId, Vector3 center, float radius)
            => new(id, actorId, HitBodyKind.Sphere, center, MathF.Max(0f, radius), 0f, Vector3.UnitY, Vector3.Zero, Quaternion.Identity);

        /// <param name="height">The FULL capsule height, cap to cap (cylinder length + 2*radius) —
        /// the same convention as Unity's CharacterController/CapsuleCollider.</param>
        /// <param name="axis">The capsule's long axis (need not be normalized).</param>
        public static HitBody Capsule(int id, int actorId, Vector3 center, float radius, float height, Vector3 axis)
        {
            radius = MathF.Max(0.001f, radius);
            var dir = axis.LengthSquared() > 1e-8f ? Vector3.Normalize(axis) : Vector3.UnitY;
            float halfLine = MathF.Max(0f, height * 0.5f - radius);
            return new HitBody(id, actorId, HitBodyKind.Capsule, center, radius, halfLine, dir, Vector3.Zero, Quaternion.Identity);
        }

        public static HitBody Box(int id, int actorId, Vector3 center, Vector3 halfExtents, Quaternion rotation)
            => new(id, actorId, HitBodyKind.Box, center, 0f, 0f, Vector3.UnitY, halfExtents, rotation);

        /// <summary>The capsule's two hemisphere-centre endpoints in world space. Meaningless for
        /// a sphere/box (both are Center).</summary>
        public (Vector3 A, Vector3 B) CapsuleSegment
            => (Center - CapsuleAxis * CapsuleHalfLine, Center + CapsuleAxis * CapsuleHalfLine);
    }
}
