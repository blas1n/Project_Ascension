using System;
using System.Numerics;
using ProjectAscension.GameSimulation.Physics;
using Xunit;
using Xunit.Abstractions;

namespace ProjectAscension.GameSimulation.Tests.Physics
{
    /// <summary>
    /// Headless fuzz test for hit resolution (ADR 0013) — the collision-world equivalent of the
    /// graph runtime harness (Simulation/RuntimeSimulationTests.cs). A hit used to need a Unity
    /// scene to even exercise, so nothing could fuzz it; the "projectile dies on spawn" bug lived
    /// for weeks in a codebase with 375 green tests because of exactly that gap.
    ///
    /// Fires thousands of shots from random (sometimes overlapping-the-shooter) starts, at random
    /// directions, speeds, and step sizes (including simulated frame hitches), through random
    /// sphere/capsule/box geometry, and cross-checks each fuzzed, stepped simulation against a
    /// single ground-truth sweep over the shot's whole (gravity-free, so straight-line) path.
    /// Asserts the two properties a physics-query hit detector could never be tested for: a shot
    /// never damages its own shooter, and it never tunnels through — or skips — a body that
    /// actually lies on its segment. Seeded, so any failure is reproducible.
    /// </summary>
    public class PhysicsFuzzTests
    {
        private readonly ITestOutputHelper _out;
        public PhysicsFuzzTests(ITestOutputHelper output) => _out = output;

        [Fact]
        public void FuzzedShots_NeverHitTheirOwnShooter_AndAlwaysAgreeWithTheGroundTruthFirstHit()
        {
            var rng = new Random(20260713);
            const int shooterActor = 1;
            int shotsWithGroundTruthHit = 0;
            int shotsExpired = 0;

            for (int i = 0; i < 5000; i++)
            {
                var world = new CollisionWorld();

                // The shooter's own body — sometimes right where the muzzle spawns, sometimes not.
                var shooterCenter = RandomVector(rng, 2f);
                world.AddOrUpdate(HitBody.Capsule(1, shooterActor, shooterCenter, 0.4f, 1.8f, Vector3.UnitY));

                var from = shooterCenter + RandomVector(rng, 0.3f); // muzzle sometimes buried in the shooter's own body
                var dir = RandomDirection(rng);
                float speed = 5f + (float)rng.NextDouble() * 60f;

                // A handful of OTHER actors' bodies, all placed comfortably clear of the muzzle
                // (so overlap-at-spawn is only ever the shooter's own case, tested above) — some
                // squarely on the shot's path (real obstacles), some off to the side (distractors).
                int bodyCount = rng.Next(1, 6);
                for (int b = 0; b < bodyCount; b++)
                {
                    int id = 10 + b;
                    int actorId = 100 + b;
                    float obstacleRadius = 0.3f + (float)rng.NextDouble() * 1.2f;
                    // Comfortably clear of the muzzle regardless of perpDist below (>= 2m margin
                    // past the obstacle's own radius) — overlap-at-spawn is only ever the
                    // shooter's own case (tested above), never one of these.
                    float alongDist = 2f + obstacleRadius + (float)rng.NextDouble() * 12f;
                    float combined = 0.2f + obstacleRadius;
                    // Half the time comfortably inside the combined radius (a hit); half the time
                    // comfortably outside it (a miss) — a real mix of obstacles and distractors.
                    float perpDist = rng.Next(2) == 0
                        ? (float)(rng.NextDouble() * combined * 0.8)
                        : combined * 1.6f + (float)rng.NextDouble() * 3f;
                    var center = from + dir * alongDist + RandomPerpendicular(rng, dir) * perpDist;

                    switch (rng.Next(3))
                    {
                        case 0: world.AddOrUpdate(HitBody.Sphere(id, actorId, center, obstacleRadius)); break;
                        case 1: world.AddOrUpdate(HitBody.Capsule(id, actorId, center, obstacleRadius * 0.5f, obstacleRadius * 2.2f, Vector3.UnitY)); break;
                        default: world.AddOrUpdate(HitBody.Box(id, actorId, center, new Vector3(obstacleRadius, obstacleRadius, obstacleRadius), Quaternion.Identity)); break;
                    }
                }

                float radius = 0.05f + (float)rng.NextDouble() * 0.15f;
                var settings = new ProjectileSimSettings
                {
                    OwnerActorId = shooterActor,
                    Radius = radius,
                    Gravity = 0f, // straight-line flight, so the ground-truth single sweep below is exact
                    Lifetime = 1_000_000f, // this test is about arrival, not expiry (see ExpiresAfterItsLifetime)
                    ArmDistance = 0.5f,
                    MaxCatchUp = 0.02f + (float)rng.NextDouble() * 0.18f,
                };

                // Ground truth: ONE sweep over the shot's entire possible path, well beyond every
                // placed obstacle. Excludes only the shooter — exactly what the stepped simulation
                // below must agree with (modulo the shooter's own unarmed-overlap-at-spawn, which
                // ground truth doesn't need to reason about since it already excludes the shooter).
                bool groundTruthFound = world.SweepSphere(from, from + dir * 30f, radius, shooterActor, out var groundHit);

                // The stepped, hitchy simulation — random per-shot frame pacing, including an
                // occasional huge "hitch" dt, exactly like a real frame can hand it. Stops the
                // instant it hits something; otherwise runs a generous, bounded number of frames
                // (enough physics coverage well past every obstacle regardless of catch-up
                // throttling) without relying on lifetime expiry to terminate the loop.
                float dt = rng.Next(5) == 0 ? 0.4f + (float)rng.NextDouble() * 2f : 1f / 60f;
                var state = new ProjectileSimState(from, dir * speed);
                bool everHit = false;
                ProjectileOutcome hitOutcome = default;
                for (int f = 0; f < 900; f++)
                {
                    ProjectileOutcome stepOutcome;
                    (state, stepOutcome) = ProjectileSim.Step(state, settings, world, dt);
                    if (stepOutcome.Status == ProjectileStatus.Hit)
                    {
                        everHit = true;
                        hitOutcome = stepOutcome;
                        break;
                    }
                }

                // No shot ever damages its own shooter.
                Assert.False(everHit && hitOutcome.HitActorId == shooterActor,
                    $"seed shot #{i}: hit its own shooter — from {from} dir {dir} speed {speed}");

                if (groundTruthFound)
                {
                    shotsWithGroundTruthHit++;
                    // Something genuinely lies on this segment — the stepped simulation, however
                    // it chopped up the flight, must find the SAME first body. A mismatch means
                    // either it tunnelled through the real obstacle, or stopped on the wrong one.
                    Assert.True(everHit,
                        $"seed shot #{i}: ground truth hit body {groundHit.BodyId} @ t={groundHit.T:F3}, " +
                        $"but the stepped simulation never hit anything — from {from} dir {dir} speed {speed} dt={dt}");
                    Assert.True(hitOutcome.HitBodyId == groundHit.BodyId,
                        $"seed shot #{i}: ground truth says body {groundHit.BodyId} is first, " +
                        $"stepped simulation hit body {hitOutcome.HitBodyId} instead (tunnelled past the real one, or hit a wrong one) — " +
                        $"from {from} dir {dir} speed {speed} dt={dt}");
                }
                else
                {
                    shotsExpired++;
                    Assert.False(everHit,
                        $"seed shot #{i}: ground truth found nothing on the path, but the stepped simulation reported a hit " +
                        $"on body {hitOutcome.HitBodyId} anyway — from {from} dir {dir} speed {speed} dt={dt}");
                }
            }

            _out.WriteLine($"fuzzed 5000 shots — {shotsWithGroundTruthHit} genuine hits, {shotsExpired} clean misses, " +
                            "0 self-hits, 0 tunnelled/mismatched hits.");
        }

        private static Vector3 RandomVector(Random rng, float extent)
            => new(
                (float)(rng.NextDouble() * 2 - 1) * extent,
                (float)(rng.NextDouble() * 2 - 1) * extent,
                (float)(rng.NextDouble() * 2 - 1) * extent);

        private static Vector3 RandomDirection(Random rng)
        {
            float theta = (float)(rng.NextDouble() * Math.PI * 2);
            float z = (float)(rng.NextDouble() * 2 - 1);
            float r = MathF.Sqrt(Math.Max(0f, 1f - z * z));
            var v = new Vector3(r * MathF.Cos(theta), r * MathF.Sin(theta), z);
            return v.LengthSquared() > 1e-6f ? Vector3.Normalize(v) : Vector3.UnitZ;
        }

        private static Vector3 RandomPerpendicular(Random rng, Vector3 dir)
        {
            var arbitrary = MathF.Abs(dir.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
            var perp1 = Vector3.Normalize(Vector3.Cross(dir, arbitrary));
            var perp2 = Vector3.Cross(dir, perp1);
            float angle = (float)(rng.NextDouble() * Math.PI * 2);
            return perp1 * MathF.Cos(angle) + perp2 * MathF.Sin(angle);
        }
    }
}
