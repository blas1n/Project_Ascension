namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// A firearm's magazine + reload rules (ADR: Unity is a shell). There is no ammo reserve or
    /// economy — the magazine is the only resource, and running it dry costs a beat of
    /// vulnerability (the reload). That beat is the point: it is game feel, not a number to spend.
    /// The clock is passed in (Unity supplies Time.time), so gating and reload progress are
    /// headless-testable — WeaponBase reads these; it enforces no timing itself.
    /// A weapon with <c>magazineSize == 0</c> has no magazine at all and is untouched by every rule
    /// here (always able to fire, never reloads) — the single branch that keeps melee/bow/catalyst
    /// weapons out of the gun code.
    /// </summary>
    public static class ReloadRules
    {
        /// <summary>Whether the weapon may fire right now: a magazine-less weapon always can;
        /// a magazine weapon can only when it isn't mid-reload and has a round chambered.</summary>
        public static bool CanFire(int magazineSize, int loaded, bool isReloading)
        {
            if (magazineSize <= 0) return true;
            return !isReloading && loaded > 0;
        }

        /// <summary>Whether beginning a reload now would do anything. False (a no-op) when the
        /// weapon has no magazine, is already reloading, or the magazine is already full.</summary>
        public static bool CanBeginReload(int magazineSize, int loaded, bool isReloading)
            => magazineSize > 0 && !isReloading && loaded < magazineSize;

        /// <summary>Loadout-level gate: whether an ATTACK may fire right now, given both hands'
        /// reload state. Reloading is a commitment that costs the whole loadout, not just the
        /// reloading weapon — while either hand is mid-reload, neither hand may attack. Symmetric
        /// (the two parameters are interchangeable), so a caller passes "this hand" and "the other
        /// hand" in either order. Blocking is not an attack (a shield's raise/lower never routes
        /// through this — see ShieldWeapon), so it is unaffected by this gate.</summary>
        public static bool CanAttack(bool handAReloading, bool handBReloading)
            => !handAReloading && !handBReloading;

        /// <summary>A round leaving the chamber costs one from the magazine (never below 0).</summary>
        public static int AfterShot(int loaded) => loaded > 0 ? loaded - 1 : 0;

        /// <summary>Whether a reload begun at <paramref name="reloadStart"/> has finished by
        /// <paramref name="time"/>.</summary>
        public static bool ReloadComplete(float reloadStart, float time, float reloadTime)
            => time - reloadStart >= reloadTime;

        /// <summary>How far through the current reload (0..1) — for a HUD progress bar. Reads 0
        /// when not reloading. Guards against a zero/near-zero reload time the same way
        /// <see cref="WeaponFireRules.ChargeFraction"/> guards charge time.</summary>
        public static float ReloadFraction(bool isReloading, float reloadStart, float time, float reloadTime)
        {
            if (!isReloading) return 0f;
            float span = reloadTime < 0.01f ? 0.01f : reloadTime;
            float t = (time - reloadStart) / span;
            return t < 0f ? 0f : t > 1f ? 1f : t;
        }
    }
}
