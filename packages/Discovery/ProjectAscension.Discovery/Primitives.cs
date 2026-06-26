namespace ProjectAscension.SkillForge;

/// <summary>
/// The atomic mechanics the engine can actually execute. AI composition builds a
/// skill by combining these within a power budget — it cannot invent mechanics
/// outside this whitelist (ADR 0002 core 3). New engine capability = a new kind here.
/// </summary>
public enum PrimitiveKind
{
    Projectile,
    Homing,
    Pierce,
    Area,
    DamageOverTime,
    Dash,
    Knockback,
    Shield,
}

/// <summary>A primitive's fixed power cost (per magnitude step) and a short blurb
/// used when describing a composed skill.</summary>
public sealed record PrimitiveDefinition(PrimitiveKind Kind, int BaseCost, string Blurb);

/// <summary>
/// The whitelist of composable primitives with their power costs. This is the
/// deterministic vocabulary the AI composes from; the rule engine owns it.
/// </summary>
public static class PrimitiveCatalog
{
    public static readonly IReadOnlyList<PrimitiveDefinition> All = new[]
    {
        new PrimitiveDefinition(PrimitiveKind.Projectile,     10, "a traveling bolt"),
        new PrimitiveDefinition(PrimitiveKind.Homing,          8, "that seeks a target"),
        new PrimitiveDefinition(PrimitiveKind.Pierce,          6, "punching through targets"),
        new PrimitiveDefinition(PrimitiveKind.Area,           12, "across a zone"),
        new PrimitiveDefinition(PrimitiveKind.DamageOverTime,  8, "leaving lingering harm"),
        new PrimitiveDefinition(PrimitiveKind.Dash,            7, "with a mobile burst"),
        new PrimitiveDefinition(PrimitiveKind.Knockback,       5, "that shoves targets back"),
        new PrimitiveDefinition(PrimitiveKind.Shield,         10, "raising a protective ward"),
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
