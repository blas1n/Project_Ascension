using System.Collections.Generic;

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

        /// <summary>Execute a discovered skill against the targets in range
        /// (index 0 = primary). Weapon or command resolves identically.</summary>
        public SkillResolution Use(DiscoveredSkill skill, int availableTargets)
            => SkillResolver.Resolve(skill.Skill, availableTargets);
    }
}
