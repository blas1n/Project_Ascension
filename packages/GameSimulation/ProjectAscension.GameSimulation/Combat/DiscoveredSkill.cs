namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>How a discovered skill is wielded. Mirrors the server's classification
    /// (ProjectAscension.SkillForge.ManifestationKind); names match so the API's
    /// manifestation string parses straight in.</summary>
    public enum ManifestationKind
    {
        Weapon,
        Command,
    }

    /// <summary>A discovered, executable skill plus how the player wields it — a
    /// synthesized-magic <see cref="ManifestationKind.Weapon"/> or an invoked
    /// <see cref="ManifestationKind.Command"/> — and the equipment it is bound to
    /// (<see cref="RequiredEquipment"/>, ADR 0005). The skill is usable only while the
    /// loadout holds that equipment; empty = no requirement.</summary>
    public sealed record DiscoveredSkill(
        string Name,
        ManifestationKind Manifestation,
        Skill Skill,
        System.Collections.Generic.IReadOnlyList<string>? RequiredEquipment = null);
}
