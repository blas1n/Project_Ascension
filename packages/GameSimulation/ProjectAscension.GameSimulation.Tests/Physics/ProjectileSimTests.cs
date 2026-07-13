using System.Numerics;
using ProjectAscension.GameSimulation.Physics;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Physics
{
    /// <summary>
    /// Reproduces the three bugs that shipped when hit detection lived in Unity's physics scene
    /// (ADR 0013) — "bolts dying the instant they were fired" — as tests that now pass against the
    /// pure <see cref="ProjectileSim"/>. Before this, none of the 375 green tests in the repo could
    /// see this bug class at all; it took playing the game to find it.
    /// </summary>
    public class ProjectileSimTests
    {
        private static ProjectileSimSettings Settings(int owner = 1, float radius = 0.06f, float armDistance = 0.5f, float lifetime = 1000f)
            => new()
            {
                OwnerActorId = owner,
                Radius = radius,
                ArmDistance = armDistance,
                Lifetime = lifetime,
                Gravity = 0f,
            };

        private static ProjectileOutcome RunToOutcome(ref ProjectileSimState state, ProjectileSimSettings settings, CollisionWorld world, float dt, int maxFrames = 600)
        {
            ProjectileOutcome outcome = ProjectileOutcome.StillFlying;
            for (int i = 0; i < maxFrames && outcome.Status == ProjectileStatus.Flying; i++)
                (state, outcome) = ProjectileSim.Step(state, settings, world, dt);
            return outcome;
        }

        // Bug 1: a bolt fired from INSIDE the shooter's own body hits nothing and flies on.
        [Fact]
        public void Bug1_ABoltFiredFromInsideTheShootersOwnBody_HitsNothingAndFliesOn()
        {
            var world = new CollisionWorld();
            const int shooter = 1;
            // The shooter's own hitbox surrounds the muzzle spawn point — this is exactly what
            // used to register as an immediate hit ("the projectile vanished the instant I fired").
            world.AddOrUpdate(HitBody.Capsule(1, shooter, center: Vector3.Zero, radius: 0.5f, height: 1.8f, axis: Vector3.UnitY));
            // A wall far downrange, so we can also confirm the bolt actually keeps flying and lands correctly.
            world.AddOrUpdate(HitBody.Box(2, actorId: 0, new Vector3(0, 0, 20), new Vector3(5, 5, 0.5f), Quaternion.Identity));

            var state = new ProjectileSimState(Vector3.Zero, new Vector3(0, 0, 30));
            var outcome = RunToOutcome(ref state, Settings(owner: shooter), world, 1f / 60f);

            Assert.Equal(ProjectileStatus.Hit, outcome.Status);
            Assert.Equal(2, outcome.HitBodyId);
            Assert.NotEqual(shooter, outcome.HitActorId);
        }

        // Bug 2: a bolt spawned overlapping a wall (muzzle buried in geometry) does not detonate on
        // it at spawn, and still stops at the NEXT wall.
        [Fact]
        public void Bug2_ABoltSpawnedInsideAWall_DoesNotDetonateAtSpawn_AndStillStopsAtTheNextWall()
        {
            var world = new CollisionWorld();
            // The muzzle spawn point is buried a few centimetres into this THIN wall (static geometry,
            // actor 0) — a gun barrel clipping the door frame you're standing in, not a thick bunker.
            world.AddOrUpdate(HitBody.Box(1, actorId: 0, center: Vector3.Zero, new Vector3(2, 2, 0.08f), Quaternion.Identity));
            // A second wall well beyond arming distance — the bolt must stop HERE, not sail through it.
            world.AddOrUpdate(HitBody.Box(2, actorId: 0, new Vector3(0, 0, 8), new Vector3(5, 5, 0.5f), Quaternion.Identity));

            var state = new ProjectileSimState(Vector3.Zero, new Vector3(0, 0, 5)); // modest speed -> fine-grained sub-steps
            var outcome = RunToOutcome(ref state, Settings(owner: 1), world, 1f / 60f);

            Assert.Equal(ProjectileStatus.Hit, outcome.Status);
            Assert.Equal(2, outcome.HitBodyId); // NOT the spawn wall (body 1)
            Assert.InRange(outcome.Point.Z, 7.3f, 7.6f); // expanded by the bolt radius (.06)
        }

        // Bug 3a: an absurdly long step (a frame hitch) does not tunnel through a thin wall.
        [Fact]
        public void Bug3_AFrameHitch_DoesNotTunnelThroughAThinWall()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Box(1, actorId: 0, new Vector3(0, 0, 10), new Vector3(5, 5, 0.3f), Quaternion.Identity));

            var state = new ProjectileSimState(Vector3.Zero, new Vector3(0, 0, 233f)); // very fast
            var settings = Settings(owner: 1);

            // A single huge frame delta — the "hitch" that used to let a bolt jump metres and
            // materialise inside (or beyond) geometry in one frame.
            var (_, outcome) = ProjectileSim.Step(state, settings, world, 5f);

            Assert.Equal(ProjectileStatus.Hit, outcome.Status);
            Assert.Equal(1, outcome.HitBodyId);
            Assert.InRange(outcome.Point.Z, 9.5f, 9.75f);
        }

        // Bug 3b: an absurdly long step does not teleport past a monster either.
        [Fact]
        public void Bug3_AFrameHitch_DoesNotTeleportPastAMonster()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Capsule(1, actorId: 42, new Vector3(0, 0, 8), radius: 0.6f, height: 1.8f, axis: Vector3.UnitY));

            var state = new ProjectileSimState(Vector3.Zero, new Vector3(0, 0, 233f));
            var settings = Settings(owner: 1);

            var (_, outcome) = ProjectileSim.Step(state, settings, world, 5f);

            Assert.Equal(ProjectileStatus.Hit, outcome.Status);
            Assert.Equal(42, outcome.HitActorId);
        }

        [Fact]
        public void ExpiresAfterItsLifetime_WhenNothingIsEverHit()
        {
            var world = new CollisionWorld(); // empty — nothing to hit
            var state = new ProjectileSimState(Vector3.Zero, new Vector3(0, 0, 10));
            var settings = Settings(owner: 1, lifetime: 0.05f);

            ProjectileOutcome outcome = ProjectileOutcome.StillFlying;
            for (int i = 0; i < 30 && outcome.Status == ProjectileStatus.Flying; i++)
                (state, outcome) = ProjectileSim.Step(state, settings, world, 1f / 60f);

            Assert.Equal(ProjectileStatus.Expired, outcome.Status);
        }

        [Fact]
        public void StillArmsNormally_AgainstABodyItNeverStartedInside()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 5, new Vector3(0, 0, 3), 0.5f));

            var state = new ProjectileSimState(Vector3.Zero, new Vector3(0, 0, 10));
            var outcome = RunToOutcome(ref state, Settings(owner: 1), world, 1f / 60f);

            Assert.Equal(ProjectileStatus.Hit, outcome.Status);
            Assert.Equal(5, outcome.HitActorId);
        }
    }
}
