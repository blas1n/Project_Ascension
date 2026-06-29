using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    // The manifestation is derived from the skill's composition, so different discovered
    // skills deliver differently (the discovery system's promise) — not a uniform hit.
    public class DeliveryInferenceTests
    {
        private static Skill Of(params SkillPrimitive[] primitives) => new("Test", primitives);

        [Fact]
        public void Projectile_FliesAsAProjectile()
        {
            var spec = DeliveryInference.From(Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 3)), CombatTuning.Default);
            Assert.Equal(DeliveryMotion.Projectile, spec.Motion);
            Assert.Equal(DeliveryOrigin.Muzzle, spec.Origin);
            Assert.True(spec.Speed > 0f);
            Assert.True(spec.IsInstant);
        }

        [Fact]
        public void Area_LandsAtTheAimPointAsASphere()
        {
            var spec = DeliveryInference.From(Of(new SkillPrimitive(SkillPrimitiveKind.Area, 2)), CombatTuning.Default);
            Assert.Equal(DeliveryOrigin.AimPoint, spec.Origin);
            Assert.Equal(DeliveryMotion.None, spec.Motion);
            Assert.Equal(DeliveryShape.Sphere, spec.Shape);
            Assert.True(spec.Radius >= 2f);
        }

        [Fact]
        public void Beam_OrDefault_IsAnInstantHitscanLine()
        {
            var beam = DeliveryInference.From(Of(new SkillPrimitive(SkillPrimitiveKind.Beam, 2)), CombatTuning.Default);
            Assert.Equal(DeliveryMotion.None, beam.Motion);
            Assert.Equal(DeliveryShape.Line, beam.Shape);
            Assert.True(beam.IsInstant);

            var fallback = DeliveryInference.From(Of(new SkillPrimitive(SkillPrimitiveKind.Slow, 1)), CombatTuning.Default);
            Assert.Equal(DeliveryMotion.None, fallback.Motion); // no projectile/area → hitscan
        }

        [Fact]
        public void PersistentFlag_IsReservedAndNotProducedYet()
        {
            // Today's inference is all instant; the persistent axis (zone/turret/summon) is
            // architecture-only until the composition produces it.
            var spec = DeliveryInference.From(Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 1)), CombatTuning.Default);
            Assert.False(spec.IsPersistent);
        }
    }
}
