using System;
using System.Collections.Generic;
using System.Text;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.Net;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Builds the executable <see cref="DiscoveredSkill"/> (and, for a weapon manifestation,
    /// the equippable <see cref="WeaponData"/>) from the server's composed-skill DTO. Shared
    /// by the session-start restore (GameSession) and the in-frontier discovery loader
    /// (SkillCaster) so both produce identical results.
    /// </summary>
    public static class DiscoveredSkillFactory
    {
        /// <summary>Convert a Ready skill DTO into a discovered skill (null if not Ready);
        /// <paramref name="weapon"/> is the minted equippable for a weapon manifestation, else null.
        /// Acceptance + core build live in the headless <see cref="SkillRestore"/> (contract-tested);
        /// only the WeaponData minting is Unity-side.</summary>
        public static DiscoveredSkill Build(SkillResponseDto dto, out WeaponData weapon)
        {
            weapon = null;
            var discovered = SkillRestore.FromResponse(
                dto.status, dto.name, dto.manifestation, dto.primitives, dto.invocationCombo,
                dto.contextTags, dto.description, dto.effectGraph, dto.behaviors);
            if (discovered == null) return null;

            if (discovered.Manifestation == ManifestationKind.Weapon)
                weapon = WeaponData.CreateDiscovered(discovered.Skill.Name, discovered.Skill, "spell:" + Slug(discovered.Skill.Name));
            return discovered;
        }

        private static string Slug(string name)
        {
            if (string.IsNullOrEmpty(name)) return "discovery";
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
            return sb.ToString();
        }
    }
}
