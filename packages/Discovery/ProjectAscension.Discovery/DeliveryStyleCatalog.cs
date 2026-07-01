namespace ProjectAscension.SkillForge;

/// <summary>
/// The delivery styles the composer may choose — HOW the skill reaches the world, composed
/// alongside its effect so a discovery's manifestation is AI-authored too (not merely
/// inferred from its effect primitives). Each style maps (in the runtime that executes
/// combat) to delivery axes + DB-tuned numbers; adding a style here, plus its axis mapping,
/// extends the vocabulary and the AI can then compose it. Names are lowercase single words
/// for stable parsing.
/// </summary>
public static class DeliveryStyleCatalog
{
    public static readonly IReadOnlyList<(string Style, string Blurb)> All = new[]
    {
        ("projectile", "fast straight-flying bolts — rapid fire from a set stance"),
        ("arc", "a lobbed shot that arcs through the air and drops — thrown on the move"),
        ("beam", "a focused, held ray that strikes along the aim line — a charged shot, standing ground"),
        ("burst", "an area that erupts at the point you aim at — close, committed strikes"),
        ("nova", "an area that erupts in a ring around YOU — for weaving and leaping while you charge"),
    };

    private static readonly HashSet<string> Known =
        new(All.Select(s => s.Style), StringComparer.OrdinalIgnoreCase);

    /// <summary>Normalize an AI-proposed style to a known one, or "" when missing/unknown —
    /// the executor then falls back to deriving the delivery from the skill's primitives.</summary>
    public static string Normalize(string? style)
    {
        var trimmed = style?.Trim();
        return !string.IsNullOrEmpty(trimmed) && Known.Contains(trimmed)
            ? trimmed.ToLowerInvariant()
            : string.Empty;
    }
}
