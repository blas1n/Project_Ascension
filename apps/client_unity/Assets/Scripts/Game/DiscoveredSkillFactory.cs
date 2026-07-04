using System;
using System.Collections.Generic;
using System.Text;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
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
        /// <summary>Convert a Ready skill DTO into a discovered skill; <paramref name="weapon"/>
        /// is the minted equippable for a weapon manifestation, else null.</summary>
        public static DiscoveredSkill Build(SkillResponseDto dto, out WeaponData weapon)
        {
            var skill = SkillParser.Parse(string.IsNullOrEmpty(dto.name) ? "Discovery" : dto.name, dto.primitives);
            var manifestation = Enum.TryParse<ManifestationKind>(dto.manifestation, ignoreCase: true, out var kind)
                ? kind
                : ManifestationKind.Command;
            IReadOnlyList<InputToken> combo = manifestation == ManifestationKind.Command
                ? InputCombo.Parse(dto.invocationCombo ?? Array.Empty<string>())
                : Array.Empty<InputToken>();

            var discovered = new DiscoveredSkill(skill.Name, manifestation, skill, combo);
            weapon = manifestation == ManifestationKind.Weapon
                ? WeaponData.CreateDiscovered(skill.Name, skill, "spell:" + Slug(skill.Name))
                : null;
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
