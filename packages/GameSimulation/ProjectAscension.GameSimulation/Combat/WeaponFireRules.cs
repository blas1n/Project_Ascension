namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// A weapon's deterministic fire-rate and charge rules (ADR: Unity is a shell). The clock is
    /// passed in (Unity supplies Time.time), so cooldown gating and the charge fraction are
    /// headless-testable — the Unity WeaponBase reads these; it enforces no timing itself.
    /// </summary>
    public static class WeaponFireRules
    {
        /// <summary>Whether the weapon may fire — the cooldown from the last shot has elapsed.</summary>
        public static bool CanFire(float time, float nextReadyTime) => time >= nextReadyTime;

        /// <summary>The next ready time after firing now (a cooldown ahead).</summary>
        public static float NextReady(float time, float cooldown) => time + cooldown;

        /// <summary>How charged a held shot is (0..1) — held time over the weapon's charge time,
        /// clamped. A not-started charge (start &lt; 0) or an instant weapon reads 0.</summary>
        public static float ChargeFraction(float chargeStart, float time, float chargeTime)
        {
            if (chargeStart < 0f) return 0f;
            float span = chargeTime < 0.01f ? 0.01f : chargeTime;
            float t = (time - chargeStart) / span;
            return t < 0f ? 0f : t > 1f ? 1f : t;
        }
    }
}
