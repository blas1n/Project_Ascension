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
        /// loadout must hold every piece of the skill's required equipment (ADR 0005 —
        /// a discovery is bound to the weapon it was discovered with).</summary>
        public static bool Usable(DiscoveredSkill skill, IReadOnlyCollection<string> equipped)
        {
            if (skill.RequiredEquipment is null || skill.RequiredEquipment.Count == 0) return true;
            foreach (var required in skill.RequiredEquipment)
                if (!equipped.Contains(required)) return false;
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
