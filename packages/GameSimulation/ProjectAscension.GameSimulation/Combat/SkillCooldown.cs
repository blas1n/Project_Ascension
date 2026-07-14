using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// A discovered skill's cooldown — derived from what the skill IS (its effect graph's
    /// structural "power points", ADR 0007), never authored per skill. Skills are composed at
    /// runtime by the AI, so nobody is there to hand-tune each one's wait; a bigger composition
    /// simply takes longer to ready again, the same size metric <see cref="EffectGraphQuery.PowerPoints"/>
    /// that already drives knowledge value. Replaces the removed Focus resource (project-owner
    /// decision: per-skill cooldown, Overwatch-style UI) — a skill no longer costs a pool, it
    /// costs a wait.
    /// </summary>
    public static class SkillCooldown
    {
        /// <summary>Seconds before the skill may be cast again: power points × CooldownSecondsPerPoint
        /// (DB-driven), clamped to [CooldownFloorSeconds, CooldownCeilingSeconds] so a trivial skill
        /// is never spammable and a huge one is never a once-a-fight button.</summary>
        public static float Of(EffectNode graph, CombatTuning tuning = null)
        {
            var t = tuning ?? CombatTuning.Default;
            float raw = EffectGraphQuery.PowerPoints(graph) * t.CooldownSecondsPerPoint;
            return raw < t.CooldownFloorSeconds ? t.CooldownFloorSeconds
                : raw > t.CooldownCeilingSeconds ? t.CooldownCeilingSeconds
                : raw;
        }
    }
}
