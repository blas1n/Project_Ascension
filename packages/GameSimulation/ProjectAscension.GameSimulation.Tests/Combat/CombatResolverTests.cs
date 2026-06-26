using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class CombatResolverTests
    {
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
