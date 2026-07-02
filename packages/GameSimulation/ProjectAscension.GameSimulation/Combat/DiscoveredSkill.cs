using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>How a discovered skill is wielded. Mirrors the server's classification
    /// (ProjectAscension.SkillForge.ManifestationKind); names match so the API's
    /// manifestation string parses straight in.</summary>
    public enum ManifestationKind
    {
        Weapon,
        Command,
        Passive,
    }

    /// <summary>A discovered, executable skill plus how the player wields it — a
    /// synthesized-magic <see cref="ManifestationKind.Weapon"/> (a new equippable
    /// weapon, equipped and fired) or an invoked <see cref="ManifestationKind.Command"/>
    /// (a button-combo technique). No equipment use-gate (ADR 0005). <see cref="Combo"/> is
    /// the command's assigned invocation combo (empty for weapons/passives) — the guide HUD
    /// shows it so the player knows how to trigger the command.</summary>
    public sealed record DiscoveredSkill(
        string Name, ManifestationKind Manifestation, Skill Skill,
        IReadOnlyList<InputToken>? Combo = null);
}
