using System.Collections.Generic;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// A short, human-readable summary of what a discovered skill DOES, derived from its
    /// primitives — so the player can tell skills apart (names alone don't say the effect).
    /// </summary>
    public static class SkillSummary
    {
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

        /// <summary>A passive's continuous effect spelled out (double jump, damage reduction,
        /// lifesteal) — falls back to the generic summary.</summary>
        public static string DescribePassive(Skill skill)
        {
            var e = PassiveResolver.Resolve(skill);
            var parts = new List<string>();
            if (e.ExtraJumps > 0) parts.Add(e.ExtraJumps > 1 ? $"+{e.ExtraJumps} air jumps" : "double jump");
            if (e.DamageReduction > 0f) parts.Add($"{e.DamageReduction * 100f:F0}% dmg reduction");
            if (e.Lifesteal > 0f) parts.Add($"{e.Lifesteal * 100f:F0}% lifesteal");
            return parts.Count > 0 ? string.Join(", ", parts) : Describe(skill);
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
