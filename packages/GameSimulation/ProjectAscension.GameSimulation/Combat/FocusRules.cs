namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Deterministic focus transitions: spend a skill's cost (only if affordable) and
    /// regenerate over time, clamped to max. Kept separate from <see cref="Focus"/> so
    /// the resource is a value and the rules are testable.
    /// </summary>
    public static class FocusRules
    {
        /// <summary>Spend <paramref name="cost"/> if affordable. Returns false and leaves
        /// focus unchanged when there is not enough.</summary>
        public static bool TrySpend(Focus focus, float cost, out Focus result)
        {
            if (cost <= 0f) { result = focus; return true; }
            if (focus.Current < cost) { result = focus; return false; }
            result = focus with { Current = focus.Current - cost };
            return true;
        }

        /// <summary>Restore focus by <paramref name="amount"/>, capped at max.</summary>
        public static Focus Regenerate(Focus focus, float amount)
        {
            if (amount <= 0f) return focus;
            var next = focus.Current + amount;
            return focus with { Current = next > focus.Max ? focus.Max : next };
        }
    }
}
