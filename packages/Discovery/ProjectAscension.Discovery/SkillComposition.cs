namespace ProjectAscension.SkillForge;

/// <summary>The total power a discovery's skill may spend across its primitives.
/// Derived by the rule engine (flat for now; from rarity/context later).</summary>
public sealed record PowerBudget(int Total);

// (The flat primitive composition — SkillComposition / PriorArt / ComposedPrimitive — was retired
// with primitive GENERATION and STORAGE in ADR 0007 Phase 4c; the effect graph is the sole composed
// artifact. These request/budget types remain for the graph composition request.)

/// <summary>
/// How the player actually fought, as weighted behavior counts — the signal that makes
/// the "same combination → different skill" promise real (CLAUDE.md / discovery.md). Two
/// players with the same equipment but different play (sustained charging vs. mobile
/// skirmishing) yield different profiles, so the composer shapes different skills.
/// </summary>
public sealed record BehaviorWeight(string Behavior, int Count);

/// <summary>
/// The seed a discovery's composition works from: the triggered discovery's theme/context, how
/// the player fought (<see cref="BehaviorProfile"/>), a power budget, and a deterministic
/// <see cref="Seed"/> derived from the discovery's identity (so the composition is reproducible
/// yet distinct per discovery). Built per pending skill; the graph composer reads the seed +
/// profile from it (ADR 0007 Phase 4c).
/// </summary>
public sealed record CompositionRequest(
    string Theme,
    IReadOnlyList<string> ContextTags,
    PrimitiveKind PrimaryBehavior,
    PowerBudget Budget,
    IReadOnlyList<BehaviorWeight>? BehaviorProfile = null,
    long Seed = 0);
