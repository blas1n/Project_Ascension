namespace ProjectAscension.SkillForge;

/// <summary>
/// The atomic mechanics the engine can actually execute. AI composition builds a
/// skill by combining these within a power budget — it cannot invent mechanics
/// outside this whitelist (ADR 0002 core 3). New engine capability = a new kind here.
/// </summary>
public enum PrimitiveKind
{
    // Offensive
    Projectile,
    Homing,
    Pierce,
    Area,
    DamageOverTime,
    Chain,
    Fork,
    Beam,

    // Control
    Knockback,
    Slow,
    Stun,

    // Mobility
    Dash,
    Blink,

    // Defensive
    Shield,
    Barrier,
    Leech,
}

/// <summary>Broad role of a primitive — surfaced to the AI so it composes a coherent
/// mix (an offensive core, maybe some control/defense), not a random pile.</summary>
public enum PrimitiveCategory
{
    Offensive,
    Control,
    Mobility,
    Defensive,
}

/// <summary>A primitive's category, fixed power cost (per magnitude step), and a
/// short blurb used when describing a composed skill.</summary>
public sealed record PrimitiveDefinition(PrimitiveKind Kind, PrimitiveCategory Category, int BaseCost, string Blurb);

/// <summary>
/// The whitelist of composable primitives with their power costs. This is the
/// deterministic vocabulary the AI composes from; the rule engine owns it.
/// </summary>
public static class PrimitiveCatalog
{
    public static readonly IReadOnlyList<PrimitiveDefinition> All = new[]
    {
        // Offensive
        new PrimitiveDefinition(PrimitiveKind.Projectile,     PrimitiveCategory.Offensive, 10, "a traveling bolt"),
        new PrimitiveDefinition(PrimitiveKind.Homing,         PrimitiveCategory.Offensive,  8, "that seeks a target"),
        new PrimitiveDefinition(PrimitiveKind.Pierce,         PrimitiveCategory.Offensive,  6, "punching through targets"),
        new PrimitiveDefinition(PrimitiveKind.Area,           PrimitiveCategory.Offensive, 12, "across a zone"),
        new PrimitiveDefinition(PrimitiveKind.DamageOverTime, PrimitiveCategory.Offensive,  8, "leaving lingering harm"),
        new PrimitiveDefinition(PrimitiveKind.Chain,          PrimitiveCategory.Offensive, 10, "arcing between nearby targets"),
        new PrimitiveDefinition(PrimitiveKind.Fork,           PrimitiveCategory.Offensive,  8, "splitting into several"),
        new PrimitiveDefinition(PrimitiveKind.Beam,           PrimitiveCategory.Offensive, 11, "as a sustained beam"),

        // Control
        new PrimitiveDefinition(PrimitiveKind.Knockback,      PrimitiveCategory.Control,    5, "that shoves targets back"),
        new PrimitiveDefinition(PrimitiveKind.Slow,           PrimitiveCategory.Control,    6, "slowing what it touches"),
        new PrimitiveDefinition(PrimitiveKind.Stun,           PrimitiveCategory.Control,    9, "briefly stunning them"),

        // Mobility
        new PrimitiveDefinition(PrimitiveKind.Dash,           PrimitiveCategory.Mobility,   7, "with a mobile burst"),
        new PrimitiveDefinition(PrimitiveKind.Blink,          PrimitiveCategory.Mobility,   8, "blinking a short distance"),

        // Defensive
        new PrimitiveDefinition(PrimitiveKind.Shield,         PrimitiveCategory.Defensive, 10, "raising a protective ward"),
        new PrimitiveDefinition(PrimitiveKind.Barrier,        PrimitiveCategory.Defensive, 10, "conjuring a blocking wall"),
        new PrimitiveDefinition(PrimitiveKind.Leech,          PrimitiveCategory.Defensive,  8, "siphoning life from the struck"),
    };

    private static readonly Dictionary<PrimitiveKind, PrimitiveDefinition> ByKind =
        All.ToDictionary(d => d.Kind);

    public static bool IsKnown(PrimitiveKind kind) => ByKind.ContainsKey(kind);

    /// <summary>Base power cost of one magnitude step. Throws on an unknown kind.</summary>
    public static int BaseCostOf(PrimitiveKind kind) =>
        ByKind.TryGetValue(kind, out var def)
            ? def.BaseCost
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown primitive kind.");

    public static bool TryGet(PrimitiveKind kind, out PrimitiveDefinition? def) =>
        ByKind.TryGetValue(kind, out def);
}
