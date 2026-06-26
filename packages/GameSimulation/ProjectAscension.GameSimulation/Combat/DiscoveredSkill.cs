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
    /// <see cref="ManifestationKind.Command"/>.</summary>
    public sealed record DiscoveredSkill(string Name, ManifestationKind Manifestation, Skill Skill);
}
