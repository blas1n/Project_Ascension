using System.Collections.Generic;
using System.Numerics;

namespace ProjectAscension.GameSimulation.Physics
{
    /// <summary>The answer to a <see cref="CollisionWorld.SweepSphere"/> query: what the segment
    /// hit first, at what fraction along it, where, and along which surface normal.</summary>
    public readonly struct HitResult
    {
        public int BodyId { get; }
        public int ActorId { get; }

        /// <summary>0..1 along the swept segment. 0 means the sweep STARTED already overlapping
        /// this body (the muzzle is inside it) — callers that need an "arming" distance (a
        /// projectile) must not treat a t=0 hit against an unarmed body as terminal (see
        /// <see cref="ProjectileSim"/>); every other caller can treat it exactly like any other hit.</summary>
        public float T { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }

        public HitResult(int bodyId, int actorId, float t, Vector3 point, Vector3 normal)
        {
            BodyId = bodyId;
            ActorId = actorId;
            T = t;
            Point = point;
            Normal = normal;
        }
    }

    /// <summary>
    /// The simulation's own model of the world's geometry (ADR 0013) — what combat actually asks
    /// when it needs to know who got hit. Not a physics engine: no forces, no solver, just bodies
    /// and two queries. Deterministic and engine-free, so the exact same code can run in Unity
    /// (client prediction / rendering) and the server (the day it resolves hits itself).
    ///
    /// Bodies are added/updated/removed by id — an actor updates its body every frame it moves;
    /// static level geometry registers once. A body that is never registered here does not exist
    /// to the game: it can look perfectly solid on screen and a bullet will still pass through it.
    /// </summary>
    public sealed class CollisionWorld
    {
        private readonly Dictionary<int, HitBody> _bodies = new();
        private static readonly HashSet<int> NoExclusions = new();

        public int Count => _bodies.Count;

        public void AddOrUpdate(HitBody body) => _bodies[body.Id] = body;

        public bool Remove(int id) => _bodies.Remove(id);

        public bool TryGet(int id, out HitBody body) => _bodies.TryGetValue(id, out body);

        public void Clear() => _bodies.Clear();

        /// <summary>The first body hit sweeping a sphere of <paramref name="radius"/> from
        /// <paramref name="from"/> to <paramref name="to"/> — a ray (pistol hitscan) is this with
        /// radius 0. Bodies owned by <paramref name="ignoreActorId"/> (the shooter, and everything
        /// belonging to them) are excluded entirely; pass 0 to exclude nobody (0 is the "no actor"
        /// sentinel, same as static geometry's actor id, so there is nothing to exclude).</summary>
        public bool SweepSphere(Vector3 from, Vector3 to, float radius, int ignoreActorId, out HitResult hit)
            => SweepSphere(from, to, radius, ignoreActorId, NoExclusions, out hit);

        /// <summary>As above, ALSO excluding specific body ids — used by <see cref="ProjectileSim"/>
        /// to keep looking past a body it isn't armed to hit yet without re-considering it. Takes
        /// the concrete <see cref="HashSet{T}"/> (not <c>IReadOnlySet&lt;int&gt;</c>) — Unity's
        /// Mono/netstandard profile hits a spurious "inaccessible due to protection level" on that
        /// interface, so this stays engine-friendly without sacrificing anything callers need.</summary>
        public bool SweepSphere(Vector3 from, Vector3 to, float radius, int ignoreActorId, HashSet<int> ignoreBodyIds, out HitResult hit)
        {
            hit = default;
            bool found = false;
            float bestT = float.MaxValue;

            foreach (var body in _bodies.Values)
            {
                if (ignoreActorId != 0 && body.ActorId == ignoreActorId) continue;
                if (ignoreBodyIds != null && ignoreBodyIds.Count > 0 && ignoreBodyIds.Contains(body.Id)) continue;

                if (SweepMath.TrySweep(body, from, to, radius, out var t, out var point, out var normal) && t < bestT)
                {
                    bestT = t;
                    hit = new HitResult(body.Id, body.ActorId, t, point, normal);
                    found = true;
                }
            }
            return found;
        }

        /// <summary>Every DISTINCT actor id with at least one body overlapping the sphere at
        /// <paramref name="centre"/> — sword arcs and blast radii. <paramref name="ignoreActorId"/>
        /// excludes the attacker (0 = exclude nobody).</summary>
        public IReadOnlyList<int> OverlapSphere(Vector3 centre, float radius, int ignoreActorId)
        {
            var result = new List<int>();
            var seen = new HashSet<int>();

            foreach (var body in _bodies.Values)
            {
                if (ignoreActorId != 0 && body.ActorId == ignoreActorId) continue;
                if (!SweepMath.Overlaps(body, centre, radius)) continue;
                if (seen.Add(body.ActorId)) result.Add(body.ActorId);
            }
            return result;
        }
    }
}
