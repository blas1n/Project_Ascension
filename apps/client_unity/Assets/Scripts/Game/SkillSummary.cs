using System.Collections.Generic;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// A short, human-readable summary of what a discovered skill DOES, derived from its
    /// primitives — so the player can tell skills apart (names alone don't say the effect).
    /// </summary>
    public static class SkillSummary
    {
        /// <summary>Effect summary for a discovered skill — from its effect GRAPH (ADR 0007) when
        /// it has one, else its primitives.</summary>
        public static string Describe(DiscoveredSkill d)
            => d.Graph != null ? DescribeGraph(d.Graph) : Describe(d.Skill);

        /// <summary>A short summary of a skill's effect graph: delivery shape + accents.</summary>
        public static string DescribeGraph(EffectNode graph)
        {
            if (graph == null) return "—";
            var parts = new List<string>();
            var delivery = EffectGraphQuery.DeliveryStyle(graph);
            if (!string.IsNullOrEmpty(delivery)) parts.Add(delivery + " dmg");
            if (EffectGraphQuery.HasSpread(graph)) parts.Add("multi-target");
            if (EffectGraphQuery.HasDot(graph)) parts.Add("burn");
            if (EffectGraphQuery.HasKnockback(graph)) parts.Add("knockback");
            if (EffectGraphQuery.HasHoming(graph)) parts.Add("homing");
            if (EffectGraphQuery.HasLeech(graph)) parts.Add("lifesteal");
            return parts.Count > 0 ? string.Join(" + ", parts) : "—";
        }

        /// <summary>A generic effect summary: the distinct primitive effects, joined.</summary>
        public static string Describe(Skill skill)
        {
            if (skill == null || skill.Primitives.Count == 0) return "—";
            var parts = new List<string>();
            var seen = new HashSet<SkillPrimitiveKind>();
            foreach (var p in skill.Primitives)
                if (seen.Add(p.Kind))
                    parts.Add(Phrase(p.Kind));
            return string.Join(" + ", parts);
        }

        /// <summary>A passive's continuous effect spelled out (double jump, wall-climb, damage
        /// reduction, lifesteal) — movement from the effect graph (ADR 0007), defensive from the
        /// primitives; falls back to the generic summary.</summary>
        public static string DescribePassive(DiscoveredSkill d)
        {
            var parts = new List<string>();

            var move = MovementCapability.From(new[] { d.Graph });
            if (move.ExtraJumps > 0) parts.Add(move.ExtraJumps > 1 ? $"+{move.ExtraJumps} air jumps" : "double jump");
            if (move.WallClimb) parts.Add("wall-climb");

            var e = PassiveResolver.Resolve(d.Skill);
            if (e.DamageReduction > 0f) parts.Add($"{e.DamageReduction * 100f:F0}% dmg reduction");
            if (e.Lifesteal > 0f) parts.Add($"{e.Lifesteal * 100f:F0}% lifesteal");
            return parts.Count > 0 ? string.Join(", ", parts) : Describe(d.Skill);
        }

        private static string Phrase(SkillPrimitiveKind k) => k switch
        {
            SkillPrimitiveKind.Projectile => "bolt dmg",
            SkillPrimitiveKind.Homing => "homing",
            SkillPrimitiveKind.Pierce => "pierce",
            SkillPrimitiveKind.Area => "AoE dmg",
            SkillPrimitiveKind.DamageOverTime => "burn",
            SkillPrimitiveKind.Chain => "chains",
            SkillPrimitiveKind.Fork => "splits",
            SkillPrimitiveKind.Beam => "beam dmg",
            SkillPrimitiveKind.Knockback => "knockback",
            SkillPrimitiveKind.Slow => "slow",
            SkillPrimitiveKind.Stun => "stun",
            SkillPrimitiveKind.Dash => "dash",
            SkillPrimitiveKind.Blink => "blink",
            SkillPrimitiveKind.Shield => "shield",
            SkillPrimitiveKind.Barrier => "ward",
            SkillPrimitiveKind.Leech => "lifesteal",
            _ => k.ToString(),
        };
    }
}
