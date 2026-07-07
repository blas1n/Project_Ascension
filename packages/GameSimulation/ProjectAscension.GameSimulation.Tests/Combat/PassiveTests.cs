using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.GameSimulation.Player;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class PassiveTests
    {
        private static DiscoveredSkill Passive(string name, params SkillPrimitive[] primitives)
            => new(name, ManifestationKind.Passive, new Skill(name, primitives));

        // A mobility passive carrying a double-jump graph (ADR 0007).
        private static DiscoveredSkill Mover(string name, EffectNode graph)
            => new(name, ManifestationKind.Passive, new Skill(name, new[] { new SkillPrimitive(SkillPrimitiveKind.Dash, 1) }),
                   Graph: graph);

        [Fact]
        public void Resolve_MapsDefensivePrimitives()
        {
            var effect = PassiveResolver.Resolve(new Skill("Ward",
                new[] { new SkillPrimitive(SkillPrimitiveKind.Barrier, 2), new SkillPrimitive(SkillPrimitiveKind.Leech, 3) }));

            Assert.Equal(0.16f, effect.DamageReduction, precision: 3); // Barrier 2 × 0.08
            Assert.Equal(0.15f, effect.Lifesteal, precision: 3);       // Leech 3 × 0.05
        }

        [Fact]
        public void AggregateMovement_DoubleJumpFromGraph()
        {
            // A mobility skill's graph — on an in-air jump, an upward impulse — grants an extra
            // air jump. No ExtraJumps field: the capability is read off the graph (ADR 0007).
            var set = new DiscoveredSkillSet();
            set.Add(Mover("Leap", new Trigger(TriggerKind.OnJumpInAir, new Impulse(ImpulseDirection.Up, 1))));

            Assert.Equal(1, set.AggregateMovement().ExtraJumps);
        }

        [Fact]
        public void AggregateMovement_WallClimbFromGraph()
        {
            var set = new DiscoveredSkillSet();
            set.Add(Mover("Wall Run", new Trigger(TriggerKind.OnWallContact, new Impulse(ImpulseDirection.Up, 2))));

            Assert.True(set.AggregateMovement().WallClimb);
        }

        [Fact]
        public void AggregateMovement_ExtraJumpsIsCapped()
        {
            var set = new DiscoveredSkillSet();
            for (int i = 0; i < 5; i++)
                set.Add(Mover($"leap-{i}", new Trigger(TriggerKind.OnJumpInAir, new Impulse(ImpulseDirection.Up, 1))));

            Assert.Equal(MovementCapability.MaxExtraJumps, set.AggregateMovement().ExtraJumps);
        }

        [Fact]
        public void Resolve_MobilityPrimitives_NoLongerGrantMovementViaPassiveEffect()
        {
            // Movement moved out of PassiveEffect entirely — Dash/Blink no longer affect it.
            var effect = PassiveResolver.Resolve(new Skill("Leap",
                new[] { new SkillPrimitive(SkillPrimitiveKind.Blink, 1) }));
            Assert.Equal(PassiveEffect.None, effect);
        }

        [Fact]
        public void AggregateDamageReduction_IsCapped()
        {
            // Many strong wards stack but cannot exceed the cap.
            var set = new DiscoveredSkillSet();
            for (int i = 0; i < 5; i++)
                set.Add(Passive($"ward-{i}", new SkillPrimitive(SkillPrimitiveKind.Barrier, 5))); // 0.40 each

            Assert.Equal(PassiveEffect.MaxDamageReduction, set.AggregatePassive().DamageReduction, precision: 3);
        }

        [Fact]
        public void Set_PartitionsAndAggregatesPassives()
        {
            var set = new DiscoveredSkillSet();
            set.Add(Passive("Bulwark", new SkillPrimitive(SkillPrimitiveKind.Barrier, 1)));   // 0.08 reduction
            set.Add(Passive("Siphon", new SkillPrimitive(SkillPrimitiveKind.Leech, 2)));       // 0.10 lifesteal
            set.Add(new DiscoveredSkill("Bolt", ManifestationKind.Weapon,
                new Skill("Bolt", new[] { new SkillPrimitive(SkillPrimitiveKind.Projectile, 1) })));

            Assert.Equal(2, set.Passives.Count);
            Assert.Single(set.Weapons);

            var total = set.AggregatePassive();
            Assert.Equal(0.08f, total.DamageReduction, precision: 3);
            Assert.Equal(0.10f, total.Lifesteal, precision: 3);
        }

        [Fact]
        public void NoPassives_AggregatesToNone()
        {
            Assert.Equal(PassiveEffect.None, new DiscoveredSkillSet().AggregatePassive());
        }
    }
}
