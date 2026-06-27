namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The deterministic payoff of a charged attack: a charge level (0..1) maps to a
    /// damage/speed multiplier on a linear ramp from ×1 (no charge) up to the weapon's
    /// max (≥1). Held by a bow/charge weapon so the math is testable and identical on
    /// server and client.
    /// </summary>
    public static class ChargeRules
    {
        public static float Multiplier(float charge01, float maxMultiplier)
        {
            if (charge01 < 0f) charge01 = 0f;
            else if (charge01 > 1f) charge01 = 1f;
            float max = maxMultiplier < 1f ? 1f : maxMultiplier;
            return 1f + (max - 1f) * charge01;
        }
    }
}
