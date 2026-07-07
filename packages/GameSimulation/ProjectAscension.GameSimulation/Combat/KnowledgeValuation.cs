using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Values a discovered skill's knowledge for sale ("간단한 거래" — knowledge becomes a
    /// tradeable asset). The worth derives from the skill's power (the size the discovery engine
    /// froze), so a stronger discovery sells for more. Selling a license is income; the discoverer
    /// keeps the discovery (the first-discoverer record is permanent — ADR 0002).
    /// </summary>
    public static class KnowledgeValuation
    {
        /// <summary>The skill's power, summed across its primitives.</summary>
        public static int PowerPoints(Skill skill)
        {
            int points = 0;
            foreach (var p in skill.Primitives)
                points += p.Magnitude + p.Range + p.Duration;
            return points;
        }

        /// <summary>The skill's power from its effect graph (ADR 0007 Phase 4c) — the graph
        /// analogue used when the discovery carries a graph.</summary>
        public static int PowerPoints(EffectNode graph) => EffectGraphQuery.PowerPoints(graph);

        /// <summary>Gold a knowledge license sells for, at the given rate per power point.</summary>
        public static int LicensePrice(Skill skill, int goldPerPoint)
            => PowerPoints(skill) * goldPerPoint;

        /// <summary>Standing (명성) gained from selling notable knowledge — one point per
        /// <paramref name="pointsPerReputation"/> of power (0 disables).</summary>
        public static int LicenseReputation(Skill skill, int pointsPerReputation)
            => pointsPerReputation <= 0 ? 0 : PowerPoints(skill) / pointsPerReputation;
    }
}
