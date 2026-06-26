namespace ProjectAscension.SkillForge;

/// <summary>The total power a discovery's skill may spend across its primitives.
/// Derived by the rule engine (flat for now; from rarity/context later).</summary>
public sealed record PowerBudget(int Total);

/// <summary>
/// One primitive chosen for a skill, with its scaling parameters:
/// <see cref="Magnitude"/> is potency (1..MaxMagnitude); <see cref="Range"/> is
/// reach/area and <see cref="Duration"/> is persistence (0..MaxParameterTier tiers,
/// 0 = base). Cost = base cost × magnitude + (range + duration) × tier cost.
/// </summary>
public sealed record ComposedPrimitive(PrimitiveKind Kind, int Magnitude, int Range = 0, int Duration = 0);

/// <summary>
/// A composed skill: AI-authored identity (name/description) over a whitelisted,
/// budget-bounded set of primitives. Immutable — once created and validated it is
/// frozen into a deterministic entity (ADR 0002).
/// </summary>
public sealed record SkillComposition(string Name, string Description, IReadOnlyList<ComposedPrimitive> Primitives);

/// <summary>
/// A prior discovered skill this discovery builds on — retrieved from the lineage
/// graph and fed to the composer as context (RAG-style), so a new discovery genuinely
/// extends what came before instead of being composed in isolation
/// (discovery.md 발견 그래프).
/// </summary>
public sealed record PriorArt(string Name, string Description, IReadOnlyList<ComposedPrimitive> Primitives);

/// <summary>
/// The seed a composer works from: the triggered discovery's theme/context, a power
/// budget, and the <see cref="Lineage"/> of prior discoveries it builds on. The
/// composer proposes a <see cref="SkillComposition"/>;
/// <see cref="CompositionValidator"/> enforces the guardrails.
/// </summary>
public sealed record CompositionRequest(
    string Theme,
    IReadOnlyList<string> ContextTags,
    PrimitiveKind PrimaryBehavior,
    PowerBudget Budget,
    IReadOnlyList<PriorArt>? Lineage = null);
