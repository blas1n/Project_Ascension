namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// A discovered command's deterministic per-cast cooldown gate (ADR: Unity is a shell) —
    /// mirrors <see cref="WeaponFireRules"/>'s fire-rate gating exactly, but tracks the ability
    /// slot a COMMAND is bound to (AbilitySlots), not a held weapon. The clock is passed in
    /// (Unity supplies Time.time), so the gate is headless-testable; AbilitySlots enforces it and
    /// owns no timing of its own — the DECISION lives here.
    /// </summary>
    public static class SkillCooldownRules
    {
        /// <summary>Whether the bound command may be cast — the cooldown from its last cast has elapsed.</summary>
        public static bool CanCast(float time, float nextReadyTime) => time >= nextReadyTime;

        /// <summary>The next ready time after casting now (a cooldown ahead).</summary>
        public static float NextReady(float time, float cooldown) => time + cooldown;
    }
}
