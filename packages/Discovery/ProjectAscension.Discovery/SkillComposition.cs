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
/// A composed skill: AI-authored identity (name/description), a whitelisted, budget-bounded
/// set of effect primitives, and the <see cref="Delivery"/> style (how it manifests — a
/// value from <see cref="DeliveryStyleCatalog"/>, or "" to let the executor derive it).
/// Immutable — once created and validated it is frozen into a deterministic entity (ADR 0002).
/// </summary>
public sealed record SkillComposition(
    string Name, string Description, IReadOnlyList<ComposedPrimitive> Primitives, string Delivery = "");

/// <summary>
/// A prior discovered skill this discovery builds on — retrieved from the lineage
/// graph and fed to the composer as context (RAG-style), so a new discovery genuinely
/// extends what came before instead of being composed in isolation
/// (discovery.md 발견 그래프).
/// </summary>
public sealed record PriorArt(string Name, string Description, IReadOnlyList<ComposedPrimitive> Primitives);

/// <summary>
/// How the player actually fought, as weighted behavior counts — the signal that makes
/// the "same combination → different skill" promise real (CLAUDE.md / discovery.md). Two
/// players with the same equipment but different play (sustained charging vs. mobile
/// skirmishing) yield different profiles, so the composer shapes different skills.
/// </summary>
public sealed record BehaviorWeight(string Behavior, int Count);

/// <summary>
/// The seed a composer works from: the triggered discovery's theme/context, how the
/// player fought (<see cref="BehaviorProfile"/>), a power budget, the <see cref="Lineage"/>
/// of prior discoveries it builds on, and a deterministic <see cref="Seed"/> derived from
/// the discovery's identity. The composer proposes a <see cref="SkillComposition"/>;
/// <see cref="CompositionValidator"/> enforces the guardrails.
///
/// Behavior + a per-discovery seed are what break the "static recipe" failure mode: a
/// coarse seed (theme + primary behavior + budget) collapses similar play to one identical
/// skill, because the composition is a deterministic function of its input.
/// </summary>
public sealed record CompositionRequest(
    string Theme,
    IReadOnlyList<string> ContextTags,
    PrimitiveKind PrimaryBehavior,
    PowerBudget Budget,
    IReadOnlyList<PriorArt>? Lineage = null,
    IReadOnlyList<BehaviorWeight>? BehaviorProfile = null,
    long Seed = 0);
