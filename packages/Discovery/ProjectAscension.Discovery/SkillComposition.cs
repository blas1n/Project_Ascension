namespace ProjectAscension.SkillForge;

/// <summary>The total power a discovery's skill may spend across its primitives.
/// Derived by the rule engine (flat for now; from rarity/context later).</summary>
public sealed record PowerBudget(int Total);

/// <summary>One primitive chosen for a skill, scaled by <see cref="Magnitude"/>
/// (1..N). Cost = base cost × magnitude.</summary>
public sealed record ComposedPrimitive(PrimitiveKind Kind, int Magnitude);

/// <summary>
/// A composed skill: AI-authored identity (name/description) over a whitelisted,
/// budget-bounded set of primitives. Immutable — once created and validated it is
/// frozen into a deterministic entity (ADR 0002).
/// </summary>
public sealed record SkillComposition(string Name, string Description, IReadOnlyList<ComposedPrimitive> Primitives);

/// <summary>
/// The seed a composer works from: the triggered discovery's theme/context and a
/// power budget. The composer proposes a <see cref="SkillComposition"/>;
/// <see cref="CompositionValidator"/> enforces the guardrails.
/// </summary>
public sealed record CompositionRequest(
    string Theme,
    IReadOnlyList<string> ContextTags,
    PrimitiveKind PrimaryBehavior,
    PowerBudget Budget);
