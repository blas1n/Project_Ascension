using System.Collections.Generic;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Harness
{
    /// <summary>What a simulated fight produced — enough to assert it behaved and to compare kits.</summary>
    public sealed record CombatReport(
        int Ticks,
        bool AllTargetsDown,
        int TargetsDown,
        int InitialTargets,
        float TotalDamageDealt,
        float PlayerHealed,
        float PlayerShield);

    /// <summary>
    /// A headless, deterministic combat arena (ADR 0007 simulation, mode B) — a player wielding a
    /// kit of discovered skills (their effect graphs) fights a wave of dummy targets over ticks,
    /// resolving each cast through the SAME <see cref="GraphSkillResolver"/> the game uses. It
    /// imitates the expedition combat loop without Unity, so we can replay many kits × waves and
    /// check the fight behaves (terminates, damage lands, nothing degenerates) — coverage a manual
    /// playtest can't reach. Targets are inert HP pools (control has no one to disable), which is
    /// enough to exercise the damage/dot/heal/shield paths deterministically.
    /// </summary>
    public static class CombatSimulator
    {
        public static CombatReport Simulate(
            IReadOnlyList<EffectNode> playerSkills,
            int targetCount,
            float targetHp,
            int maxTicks = 600,
            int castIntervalTicks = 30,
            CombatTuning tuning = null)
        {
            var t = tuning ?? CombatTuning.Default;
            var hp = new float[targetCount];
            for (int i = 0; i < targetCount; i++) hp[i] = targetHp;
            // Per-target damage-over-time streams still ticking.
            var dots = new List<(float PerTick, int TicksLeft)>[targetCount];
            for (int i = 0; i < targetCount; i++) dots[i] = new List<(float, int)>();

            float totalDamage = 0f, healed = 0f, shield = 0f;
            int nextSkill = 0, tick = 0;

            for (; tick < maxTicks; tick++)
            {
                // Damage-over-time resolves each tick.
                for (int i = 0; i < targetCount; i++)
                {
                    if (hp[i] <= 0f) continue;
                    var stream = dots[i];
                    for (int k = stream.Count - 1; k >= 0; k--)
                    {
                        var (perTick, left) = stream[k];
                        if (hp[i] > 0f) { hp[i] -= perTick; totalDamage += perTick; }
                        if (left - 1 <= 0) stream.RemoveAt(k); else stream[k] = (perTick, left - 1);
                    }
                }

                if (AllDown(hp)) break;

                // The player casts one skill on the beat, cycling through the kit.
                if (playerSkills.Count > 0 && tick % castIntervalTicks == 0)
                {
                    var skill = playerSkills[nextSkill % playerSkills.Count];
                    nextSkill++;

                    var alive = AliveIndices(hp);
                    var res = GraphSkillResolver.Resolve(skill, alive.Count, t);
                    foreach (var effect in res.Hits)
                    {
                        if (effect.TargetIndex >= alive.Count) continue;
                        int target = alive[effect.TargetIndex];
                        if (hp[target] <= 0f) continue;
                        hp[target] -= effect.Damage;
                        totalDamage += effect.Damage;
                        if (effect.DamageOverTimePerTick > 0f && effect.DamageOverTimeTicks > 0)
                            dots[target].Add((effect.DamageOverTimePerTick, effect.DamageOverTimeTicks));
                    }
                    healed += res.SelfHeal;
                    shield += res.SelfShield;

                    if (AllDown(hp)) { tick++; break; }
                }
            }

            int down = 0;
            for (int i = 0; i < targetCount; i++) if (hp[i] <= 0f) down++;
            return new CombatReport(tick, down == targetCount && targetCount > 0, down, targetCount, totalDamage, healed, shield);
        }

        private static bool AllDown(float[] hp)
        {
            foreach (var h in hp) if (h > 0f) return false;
            return true;
        }

        private static List<int> AliveIndices(float[] hp)
        {
            var alive = new List<int>();
            for (int i = 0; i < hp.Length; i++) if (hp[i] > 0f) alive.Add(i);
            return alive;
        }
    }
}
