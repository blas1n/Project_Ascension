using System;
using System.Numerics;
using ProjectAscension.GameSimulation.Physics;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Physics
{
    /// <summary>
    /// The collision-world maths (ADR 0013): the exact building blocks combat asks of the
    /// simulation — sweep and overlap — independent of any Unity scene.
    /// </summary>
    public class CollisionWorldTests
    {
        [Fact]
        public void SweepSphere_RadiusZero_IsARay_AndHitsTheSphereAhead()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 10, center: new Vector3(0, 0, 5), radius: 1f));

            bool found = world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), radius: 0f, ignoreActorId: 0, out var hit);

            Assert.True(found);
            Assert.Equal(10, hit.ActorId);
            Assert.InRange(hit.T, 0.35f, 0.45f); // enters the sphere's surface at z=4 -> t=0.4
            Assert.InRange(hit.Point.Z, 3.9f, 4.1f);
        }

        [Fact]
        public void SweepSphere_NearestBodyWins_RegardlessOfRegistrationOrder()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 1, new Vector3(0, 0, 8), 1f)); // far, added FIRST
            world.AddOrUpdate(HitBody.Sphere(2, actorId: 2, new Vector3(0, 0, 3), 1f)); // near, added SECOND

            bool found = world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 20), 0.1f, 0, out var hit);

            Assert.True(found);
            Assert.Equal(2, hit.ActorId);
        }

        [Fact]
        public void SweepSphere_IgnoreActorId_ExcludesTheShooterAndEverythingOfTheirs()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 99, new Vector3(0, 0, 1), 0.5f)); // shooter's own body, right in front
            world.AddOrUpdate(HitBody.Sphere(2, actorId: 5, new Vector3(0, 0, 10), 1f));

            bool found = world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 20), 0.1f, ignoreActorId: 99, out var hit);

            Assert.True(found);
            Assert.Equal(5, hit.ActorId); // the shooter's own body was skipped entirely, not just de-prioritized
        }

        [Fact]
        public void SweepSphere_IgnoreActorIdZero_ExcludesNobody_SinceZeroIsTheNoActorSentinel()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 0, new Vector3(0, 0, 3), 1f)); // static geometry

            Assert.True(world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), 0.1f, ignoreActorId: 0, out var hit));
            Assert.Equal(0, hit.ActorId);
        }

        [Fact]
        public void SweepSphere_NoHit_WhenTheSegmentMissesEverything()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 1, new Vector3(10, 0, 0), 1f)); // well off to the side

            Assert.False(world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), 0.1f, 0, out _));
        }

        [Fact]
        public void SweepSphere_StartingInsideABody_ReportsAZeroTHit()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 4, center: Vector3.Zero, radius: 1f));

            bool found = world.SweepSphere(new Vector3(0.1f, 0f, 0f), new Vector3(0, 0, 10), 0.05f, 0, out var hit);

            Assert.True(found);
            Assert.Equal(0f, hit.T);
        }

        [Fact]
        public void SweepSphere_Capsule_HitsTheCylindricalBodyFromTheSide()
        {
            var world = new CollisionWorld();
            // Upright capsule: centre (0,1,5), radius .5, height 2 -> occupies y in [0.5, 1.5], the
            // cylindrical body — sweeping straight through it at y=1 stays in that regime the whole way.
            world.AddOrUpdate(HitBody.Capsule(1, actorId: 7, center: new Vector3(0, 1, 5), radius: 0.5f, height: 2f, axis: Vector3.UnitY));

            bool found = world.SweepSphere(new Vector3(0, 1, 0), new Vector3(0, 1, 10), radius: 0.1f, ignoreActorId: 0, out var hit);

            Assert.True(found);
            Assert.Equal(7, hit.ActorId);
            Assert.InRange(hit.Point.Z, 4.0f, 4.7f); // combined radius .6 -> surface at z=4.4
        }

        [Fact]
        public void SweepSphere_Capsule_HitsTheRoundedEndCap()
        {
            var world = new CollisionWorld();
            // Upright capsule at the origin: hemisphere caps centred at y=+-0.5 (height 2, radius .5).
            world.AddOrUpdate(HitBody.Capsule(1, actorId: 7, center: Vector3.Zero, radius: 0.5f, height: 2f, axis: Vector3.UnitY));

            // Straight down through the top cap — never enters the cylindrical regime at all.
            bool found = world.SweepSphere(new Vector3(0, 10, 0), new Vector3(0, -10, 0), radius: 0.05f, ignoreActorId: 0, out var hit);

            Assert.True(found);
            Assert.Equal(7, hit.ActorId);
            Assert.True(hit.Point.Y > 0.9f, $"expected the hit near the top hemisphere, got y={hit.Point.Y}");
        }

        [Fact]
        public void SweepSphere_Capsule_MissesWhenOffToTheSideOfBothTheBodyAndTheCaps()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Capsule(1, actorId: 7, center: Vector3.Zero, radius: 0.5f, height: 2f, axis: Vector3.UnitY));

            // Passes well outside the capsule's radius the entire way.
            Assert.False(world.SweepSphere(new Vector3(5, 0, -10), new Vector3(5, 0, 10), 0.1f, 0, out _));
        }

        [Fact]
        public void SweepSphere_Box_HitsAnAxisAlignedBox()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Box(1, actorId: 3, center: new Vector3(0, 0, 5), halfExtents: new Vector3(1, 1, 1), rotation: Quaternion.Identity));

            bool found = world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), radius: 0f, ignoreActorId: 0, out var hit);

            Assert.True(found);
            Assert.Equal(3, hit.ActorId);
            Assert.InRange(hit.Point.Z, 3.95f, 4.05f); // front face at z=4
        }

        [Fact]
        public void SweepSphere_Box_RotatedFortyFiveDegrees_StillHits()
        {
            var world = new CollisionWorld();
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4f);
            world.AddOrUpdate(HitBody.Box(1, actorId: 3, new Vector3(0, 0, 5), new Vector3(1, 1, 1), rotation));

            bool found = world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), 0.05f, 0, out var hit);

            Assert.True(found);
            Assert.Equal(3, hit.ActorId);
        }

        [Fact]
        public void SweepSphere_Box_MissesWhenTheSegmentPassesBesideIt()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Box(1, actorId: 3, new Vector3(5, 0, 5), new Vector3(1, 1, 1), Quaternion.Identity));

            Assert.False(world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), 0.1f, 0, out _));
        }

        [Fact]
        public void OverlapSphere_ReturnsDistinctActorsInsideTheRadius_AndExcludesTheIgnoredActor()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 1, new Vector3(0, 0, 1), 0.5f));
            world.AddOrUpdate(HitBody.Sphere(2, actorId: 2, new Vector3(0, 0, 20), 0.5f)); // far — outside the radius
            world.AddOrUpdate(HitBody.Sphere(3, actorId: 3, new Vector3(0, 0, -1), 0.5f)); // "self" — excluded by ignoreActorId

            var hits = world.OverlapSphere(Vector3.Zero, radius: 2f, ignoreActorId: 3);

            Assert.Contains(1, hits);
            Assert.DoesNotContain(2, hits);
            Assert.DoesNotContain(3, hits);
        }

        [Fact]
        public void OverlapSphere_DedupesAnActorWithMultipleOverlappingBodies()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 9, new Vector3(0, 0, 0.5f), 0.3f));
            world.AddOrUpdate(HitBody.Sphere(2, actorId: 9, new Vector3(0, 0.4f, 0), 0.3f));

            var hits = world.OverlapSphere(Vector3.Zero, radius: 2f, ignoreActorId: 0);

            Assert.Single(hits);
            Assert.Equal(9, hits[0]);
        }

        [Fact]
        public void OverlapSphere_FindsAnActorInsideACapsuleOrBox()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Capsule(1, actorId: 11, new Vector3(3, 1, 0), 0.5f, 2f, Vector3.UnitY));
            world.AddOrUpdate(HitBody.Box(2, actorId: 12, new Vector3(-3, 0, 0), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.Identity));

            var hits = world.OverlapSphere(new Vector3(0, 1, 0), radius: 3.2f, ignoreActorId: 0);

            Assert.Contains(11, hits);
            Assert.Contains(12, hits);
        }

        [Fact]
        public void AddOrUpdate_MovesABody_SubsequentSweepsSeeTheNewPosition()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 1, new Vector3(0, 0, 3), 1f));
            Assert.True(world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), 0.1f, 0, out _));

            world.AddOrUpdate(HitBody.Sphere(1, actorId: 1, new Vector3(100, 0, 3), 1f)); // moved far away — same id

            Assert.False(world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), 0.1f, 0, out _));
        }

        [Fact]
        public void Remove_TakesABodyOutOfTheWorld()
        {
            var world = new CollisionWorld();
            world.AddOrUpdate(HitBody.Sphere(1, actorId: 1, new Vector3(0, 0, 3), 1f));
            world.Remove(1);

            Assert.False(world.SweepSphere(Vector3.Zero, new Vector3(0, 0, 10), 0.1f, 0, out _));
        }
    }
}
