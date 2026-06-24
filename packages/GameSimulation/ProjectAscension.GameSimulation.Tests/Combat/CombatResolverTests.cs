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
    }
}
