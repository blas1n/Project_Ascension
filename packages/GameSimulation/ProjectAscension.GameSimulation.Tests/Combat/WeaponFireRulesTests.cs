using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>Weapon fire-rate + charge, now headless (ADR: Unity is a shell) — cooldown gating
    /// and the charge fraction are tested without Unity's clock.</summary>
    public class WeaponFireRulesTests
    {
        [Fact]
        public void CanFire_OnlyOnceTheCooldownElapsed()
        {
            Assert.True(WeaponFireRules.CanFire(time: 5f, nextReadyTime: 5f));   // exactly ready
            Assert.True(WeaponFireRules.CanFire(time: 6f, nextReadyTime: 5f));
            Assert.False(WeaponFireRules.CanFire(time: 4.9f, nextReadyTime: 5f)); // still cooling
        }

        [Fact]
        public void NextReady_IsACooldownAhead()
            => Assert.Equal(5.5f, WeaponFireRules.NextReady(time: 5f, cooldown: 0.5f), precision: 3);

        [Fact]
        public void ChargeFraction_ScalesWithHeldTime_Clamped()
        {
            Assert.Equal(0.5f, WeaponFireRules.ChargeFraction(chargeStart: 0f, time: 0.5f, chargeTime: 1f), precision: 3);
            Assert.Equal(1f, WeaponFireRules.ChargeFraction(chargeStart: 0f, time: 2f, chargeTime: 1f), precision: 3); // capped
            Assert.Equal(0f, WeaponFireRules.ChargeFraction(chargeStart: -1f, time: 5f, chargeTime: 1f), precision: 3); // not charging
        }

        [Fact]
        public void ChargeFraction_GuardsAgainstZeroChargeTime()
            => Assert.Equal(1f, WeaponFireRules.ChargeFraction(chargeStart: 0f, time: 0.5f, chargeTime: 0f), precision: 3);
    }
}
