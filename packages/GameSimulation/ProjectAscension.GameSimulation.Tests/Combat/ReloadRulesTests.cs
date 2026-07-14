using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>Magazine + reload gating, headless (ADR: Unity is a shell). There is no ammo
    /// reserve — only the magazine — so these rules are the entire vulnerability beat of running
    /// dry: cannot fire empty or mid-reload, a shot spends a round, a completed reload refills it.</summary>
    public class ReloadRulesTests
    {
        [Fact]
        public void CanFire_MagazinelessWeapon_IsAlwaysUnaffected()
        {
            // A sword/bow/catalyst/shield (MagazineSize 0) fires regardless of "loaded"/reloading —
            // the single branch that keeps them out of the gun code entirely.
            Assert.True(ReloadRules.CanFire(magazineSize: 0, loaded: 0, isReloading: true));
            Assert.True(ReloadRules.CanFire(magazineSize: 0, loaded: 0, isReloading: false));
        }

        [Fact]
        public void CanFire_MagazineWeapon_NeedsRoundsAndNotReloading()
        {
            Assert.True(ReloadRules.CanFire(magazineSize: 12, loaded: 1, isReloading: false));
            Assert.False(ReloadRules.CanFire(magazineSize: 12, loaded: 0, isReloading: false)); // dry
            Assert.False(ReloadRules.CanFire(magazineSize: 12, loaded: 12, isReloading: true)); // mid-reload
        }

        [Fact]
        public void CanBeginReload_NoOpWhenNoMagazineAlreadyReloadingOrFull()
        {
            Assert.False(ReloadRules.CanBeginReload(magazineSize: 0, loaded: 0, isReloading: false)); // no magazine
            Assert.False(ReloadRules.CanBeginReload(magazineSize: 12, loaded: 6, isReloading: true));  // already reloading
            Assert.False(ReloadRules.CanBeginReload(magazineSize: 12, loaded: 12, isReloading: false)); // full
            Assert.True(ReloadRules.CanBeginReload(magazineSize: 12, loaded: 0, isReloading: false));
            Assert.True(ReloadRules.CanBeginReload(magazineSize: 12, loaded: 11, isReloading: false));
        }

        [Fact]
        public void AfterShot_DecrementsAndFloorsAtZero()
        {
            Assert.Equal(11, ReloadRules.AfterShot(12));
            Assert.Equal(0, ReloadRules.AfterShot(0)); // never goes negative
        }

        [Fact]
        public void ReloadComplete_OnlyOnceReloadTimeHasElapsed()
        {
            Assert.True(ReloadRules.ReloadComplete(reloadStart: 5f, time: 7f, reloadTime: 1.6f));  // well past done
            Assert.False(ReloadRules.ReloadComplete(reloadStart: 5f, time: 6f, reloadTime: 1.6f)); // still reloading
        }

        [Fact]
        public void ReloadFraction_ScalesWithElapsedTime_Clamped()
        {
            Assert.Equal(0f, ReloadRules.ReloadFraction(isReloading: false, reloadStart: 0f, time: 99f, reloadTime: 1.6f)); // not reloading
            Assert.Equal(0.5f, ReloadRules.ReloadFraction(isReloading: true, reloadStart: 0f, time: 0.8f, reloadTime: 1.6f), precision: 3);
            Assert.Equal(1f, ReloadRules.ReloadFraction(isReloading: true, reloadStart: 0f, time: 5f, reloadTime: 1.6f)); // capped
        }

        [Fact]
        public void ReloadFraction_GuardsAgainstZeroReloadTime()
            => Assert.Equal(1f, ReloadRules.ReloadFraction(isReloading: true, reloadStart: 0f, time: 0.5f, reloadTime: 0f));

        [Fact]
        public void CanAttack_TrueOnlyWhenNeitherHandIsReloading()
        {
            Assert.True(ReloadRules.CanAttack(handAReloading: false, handBReloading: false));
            Assert.False(ReloadRules.CanAttack(handAReloading: true, handBReloading: false)); // own hand
            Assert.False(ReloadRules.CanAttack(handAReloading: false, handBReloading: true));  // other hand
            Assert.False(ReloadRules.CanAttack(handAReloading: true, handBReloading: true));   // both
        }

        [Fact]
        public void CanAttack_IsSymmetric()
            => Assert.Equal(
                ReloadRules.CanAttack(handAReloading: true, handBReloading: false),
                ReloadRules.CanAttack(handAReloading: false, handBReloading: true));
    }
}
