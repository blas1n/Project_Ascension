using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class CombatResolverTests
    {
        [Fact]
        public void Reduced_AppliesDefensiveReduction_Clamped()
        {
            Assert.Equal(50f, CombatResolver.Reduced(100f, 0.5f), precision: 3);  // 50% reduction
            Assert.Equal(100f, CombatResolver.Reduced(100f, 0f), precision: 3);   // none
            Assert.Equal(0f, CombatResolver.Reduced(100f, 1f), precision: 3);     // full block
            Assert.Equal(100f, CombatResolver.Reduced(100f, -1f), precision: 3);  // clamped below 0
            Assert.Equal(0f, CombatResolver.Reduced(100f, 2f), precision: 3);     // clamped above 1
        }

        [Fact]
        public void ApplyDamage_WithReduction_IsAtomic()
        {
            var next = CombatResolver.ApplyDamage(Health.Full(100f), 40f, reduction: 0.25f); // 40 × 0.75 = 30
            Assert.Equal(70f, next.Current, precision: 3);
        }

        [Fact]
        public void ApplyDamage_ReducesCurrentHealth()
        {
            var health = Health.Full(100f);

            var next = CombatResolver.ApplyDamage(health, 30f);

            Assert.Equal(70f, next.Current, precision: 3);
            Assert.False(next.IsDead);
        }

        [Fact]
        public void ApplyDamage_ClampsAtZeroAndIsDead()
        {
            var health = Health.Full(50f);

            var next = CombatResolver.ApplyDamage(health, 80f);

            Assert.Equal(0f, next.Current, precision: 3);
            Assert.True(next.IsDead);
        }

        [Fact]
        public void ApplyDamage_NonPositive_NoChange()
        {
            var health = Health.Full(40f);

            var next = CombatResolver.ApplyDamage(health, 0f);

            Assert.Equal(40f, next.Current, precision: 3);
        }

        [Fact]
        public void ApplyHeal_RestoresUpToMax()
        {
            var health = CombatResolver.ApplyDamage(Health.Full(100f), 50f); // 50

            Assert.Equal(80f, CombatResolver.ApplyHeal(health, 30f).Current, precision: 3);
            Assert.Equal(100f, CombatResolver.ApplyHeal(health, 999f).Current, precision: 3); // clamped
        }

        [Fact]
        public void ApplyHeal_DoesNotReviveTheDead()
        {
            var dead = CombatResolver.ApplyDamage(Health.Full(20f), 50f);

            var next = CombatResolver.ApplyHeal(dead, 10f);

            Assert.True(next.IsDead);
            Assert.Equal(0f, next.Current, precision: 3);
        }
    }
}
