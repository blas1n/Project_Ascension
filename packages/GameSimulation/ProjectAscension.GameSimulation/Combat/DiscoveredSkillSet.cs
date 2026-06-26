using System.Collections.Generic;
using System.Linq;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The player's discovered skills, organized by how they are wielded: weapons
    /// (synthesized magic — equipped and fired) and commands (techniques — invoked).
    /// Both execute through the same <see cref="SkillResolver"/>; the manifestation
    /// only decides where the skill is slotted and how it is triggered.
    /// </summary>
    public sealed class DiscoveredSkillSet
    {
        private readonly List<DiscoveredSkill> _weapons = new();
        private readonly List<DiscoveredSkill> _commands = new();

        public IReadOnlyList<DiscoveredSkill> Weapons => _weapons;
        public IReadOnlyList<DiscoveredSkill> Commands => _commands;

        public void Add(DiscoveredSkill skill)
        {
            if (skill.Manifestation == ManifestationKind.Weapon) _weapons.Add(skill);
            else _commands.Add(skill);
        }

        /// <summary>Whether the skill can be used with the currently equipped gear: the
        /// loadout's equipment must be the SAME SET the skill was discovered with —
        /// both hands match, hand position irrelevant (ADR 0005). Empty binding (e.g. a
        /// no-equipment movement discovery) is always usable.</summary>
        public static bool Usable(DiscoveredSkill skill, IReadOnlyCollection<string> equipped)
        {
            var required = skill.RequiredEquipment;
            if (required is null || required.Count == 0) return true;
            if (equipped.Count != required.Count) return false; // no extra / no missing
            foreach (var tag in required)
                if (!equipped.Contains(tag)) return false;
            return true;
        }

        /// <summary>Execute a discovered skill against the targets in range
        /// (index 0 = primary) — only if the equipped gear satisfies its binding;
        /// otherwise no effect. Weapon or command resolves identically.</summary>
        public SkillResolution Use(DiscoveredSkill skill, IReadOnlyCollection<string> equipped, int availableTargets)
            => Usable(skill, equipped)
                ? SkillResolver.Resolve(skill.Skill, availableTargets)
                : SkillResolution.Empty;
    }
}
