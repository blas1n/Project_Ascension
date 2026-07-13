using System;
using System.Collections.Generic;
using System.Numerics;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.GameSimulation.Physics
{
    public enum ProjectileStatus
    {
        Flying,
        Hit,
        Expired,
    }

    /// <summary>What a projectile step decided: still in flight, hit something (and what/where),
    /// or ran out of lifetime.</summary>
    public readonly struct ProjectileOutcome
    {
        public ProjectileStatus Status { get; }
        public int HitActorId { get; }
        public int HitBodyId { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }

        private ProjectileOutcome(ProjectileStatus status, int hitActorId, int hitBodyId, Vector3 point, Vector3 normal)
        {
            Status = status;
            HitActorId = hitActorId;
            HitBodyId = hitBodyId;
            Point = point;
            Normal = normal;
        }

        public static readonly ProjectileOutcome StillFlying = new(ProjectileStatus.Flying, 0, 0, default, default);
        public static ProjectileOutcome Expired(Vector3 point) => new(ProjectileStatus.Expired, 0, 0, point, default);
        public static ProjectileOutcome Hit(int actorId, int bodyId, Vector3 point, Vector3 normal)
            => new(ProjectileStatus.Hit, actorId, bodyId, point, normal);
    }

    /// <summary>A projectile's flight state — deliberately a plain immutable value so a caller
    /// (Unity's Projectile shell, a test, eventually the server) can hold it across frames without
    /// this package owning any lifecycle.</summary>
    public readonly struct ProjectileSimState
    {
        public Vector3 Position { get; }
        public Vector3 Velocity { get; }

        /// <summary>Distance travelled since the muzzle — arming is by DISTANCE, never elapsed
        /// time, so a slow-starting bolt doesn't get a free pass through the thing it spawned in.</summary>
        public float Travelled { get; }

        /// <summary>Seconds alive (real elapsed time, NOT capped by <see cref="ProjectileSimSettings.MaxCatchUp"/> —
        /// lifetime expiry is wall-clock, catch-up only throttles how much PHYSICS a single Step call replays).</summary>
        public float Age { get; }

        /// <summary>Leftover fixed-step time carried into the next Step call.</summary>
        public float Accumulator { get; }

        public ProjectileSimState(Vector3 position, Vector3 velocity, float travelled = 0f, float age = 0f, float accumulator = 0f)
        {
            Position = position;
            Velocity = velocity;
            Travelled = travelled;
            Age = age;
            Accumulator = accumulator;
        }
    }

    public sealed class ProjectileSimSettings
    {
        public float Gravity { get; init; }
        public float Radius { get; init; } = 0.06f;

        /// <summary>The shooter's actor id — their bodies (and everything belonging to them) are
        /// never hit, full stop, regardless of arming.</summary>
        public int OwnerActorId { get; init; }

        public float Lifetime { get; init; } = 5f;

        /// <summary>A bolt cannot be stopped by anything it is ALREADY inside of until it has
        /// travelled this far from the muzzle. You fire from the eye, and the eye is inside the
        /// player — and sometimes inside the wall you are standing against.</summary>
        public float ArmDistance { get; init; } = 0.5f;

        /// <summary>The most simulated flight one Step call may replay. Without this, a frame hitch
        /// (a shader compile, a first-shot allocation) hands the bolt a huge dt and it would need to
        /// replay hundreds of fixed sub-steps in one call; capping it just spreads that catch-up
        /// across more frames; it does NOT let the bolt tunnel — every sub-step it does run is still
        /// swept exactly.</summary>
        public float MaxCatchUp { get; init; } = 0.1f;
    }

    /// <summary>
    /// Steps a projectile's trajectory (<see cref="Ballistics"/>) at its fixed step, sweeping each
    /// sub-step against a <see cref="CollisionWorld"/>, and reports the outcome. Pure and
    /// deterministic — this is the code that decides whether a shot hit, replacing what used to be
    /// a Unity <c>SphereCastAll</c> loop inside a MonoBehaviour (ADR 0013).
    ///
    /// Arms by DISTANCE travelled from the muzzle, never elapsed time: a body the sweep starts
    /// already inside of (the shooter, or a wall the muzzle spawned overlapping) does not count as
    /// a hit until the bolt has cleared <see cref="ProjectileSimSettings.ArmDistance"/> — but that
    /// body is only skipped, not the whole sweep, so the bolt still stops at the NEXT thing on its
    /// path rather than sailing through it "for free". A step is always the actual segment
    /// travelled that sub-tick, however large, so a frame hitch cannot tunnel through geometry —
    /// it can only make MORE sub-steps run in a single call (bounded by MaxCatchUp).
    /// </summary>
    public static class ProjectileSim
    {
        public static (ProjectileSimState State, ProjectileOutcome Outcome) Step(
            ProjectileSimState state, ProjectileSimSettings settings, CollisionWorld world, float frameDt)
        {
            float maxCatchUp = settings.MaxCatchUp > 0f ? settings.MaxCatchUp : 0.1f;
            float accumulator = MathF.Min(state.Accumulator + frameDt, maxCatchUp);
            var position = state.Position;
            var velocity = state.Velocity;
            float travelled = state.Travelled;
            float age = state.Age;

            while (accumulator >= Ballistics.FixedStep)
            {
                var from = position;
                (position, velocity) = Ballistics.Step(position, velocity, settings.Gravity, Ballistics.FixedStep);
                accumulator -= Ballistics.FixedStep;

                if (TrySweepSubStep(from, position, ref travelled, settings, world, out var outcome))
                {
                    var hitState = new ProjectileSimState(outcome.Point, velocity, travelled, age, accumulator);
                    return (hitState, outcome);
                }
            }

            age += frameDt;
            var nextState = new ProjectileSimState(position, velocity, travelled, age, accumulator);
            return age >= settings.Lifetime
                ? (nextState, ProjectileOutcome.Expired(position))
                : (nextState, ProjectileOutcome.StillFlying);
        }

        private static readonly HashSet<int> NoExclusions = new();

        // One fixed sub-step's sweep. Excludes the shooter outright (CollisionWorld's
        // ignoreActorId); a body the sweep starts already inside of, while still unarmed, is
        // skipped WITHOUT abandoning the sweep — it keeps looking for the next body on the same
        // segment, bounded so a degenerate world can't spin forever.
        private static bool TrySweepSubStep(Vector3 from, Vector3 to, ref float travelled,
            ProjectileSimSettings settings, CollisionWorld world, out ProjectileOutcome outcome)
        {
            outcome = default;
            float stepDistance = Vector3.Distance(from, to);
            if (stepDistance <= 1e-5f) return false;

            HashSet<int>? excluded = null;
            for (int guard = 0; guard < 32; guard++)
            {
                if (!world.SweepSphere(from, to, settings.Radius, settings.OwnerActorId, excluded ?? NoExclusions, out var hit))
                {
                    travelled += stepDistance;
                    return false;
                }

                float hitTravelled = travelled + hit.T * stepDistance;
                bool startedInside = hit.T <= 1e-6f;
                if (startedInside && hitTravelled < settings.ArmDistance)
                {
                    excluded ??= new HashSet<int>();
                    excluded.Add(hit.BodyId);
                    continue;
                }

                travelled = hitTravelled;
                outcome = ProjectileOutcome.Hit(hit.ActorId, hit.BodyId, hit.Point, hit.Normal);
                return true;
            }

            // Pathological: 32+ unarmed overlapping bodies on one sub-step. Treat as clear rather
            // than loop forever — the next sub-step gets another chance.
            travelled += stepDistance;
            return false;
        }
    }
}
