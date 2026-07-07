using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectAscension.SkillForge;

/// <summary>A composed ancestor a new discovery builds on (RAG over the discovery graph) — its
/// AI-authored name + description (flavor to extend) and its graph JSON (structure to evolve).</summary>
public sealed record SkillLineage(string Name, string Description, string? GraphJson);

/// <summary>What the AI needs to compose a skill (ADR 0007): the theme, how the player fought
/// (steers the root trigger), the power budget, a seed so the composition is reproducible per
/// discovery, the structural signatures to AVOID (actor-wide dedup — don't reproduce an existing
/// skill's graph shape), and the composed LINEAGE to build on (evolve prior discoveries).</summary>
public sealed record EffectGraphRequest(
    string Theme,
    IReadOnlyList<BehaviorWeight> Profile,
    PowerBudget Budget,
    long Seed,
    IReadOnlyList<string>? Avoid = null,
    IReadOnlyList<SkillLineage>? Lineage = null);

/// <summary>A composed skill: its AI-authored name + description (lore/flavor) and its effect
/// GRAPH (structure). The AI owns concept + structure; the engine owns numbers and executes
/// deterministically (ADR 0002).</summary>
public sealed record SkillGraphComposition(string Name, string Description, EffectNode Graph);

/// <summary>Composes a full skill — name, description, and effect graph — in one call (ADR 0007
/// Phase 4c). Returns null when the model produces nothing valid within budget, which defers the
/// discovery (no deterministic fallback, ADR 0002): the graph is the sole composed artifact.</summary>
public interface IEffectGraphComposer
{
    Task<SkillGraphComposition?> ComposeAsync(EffectGraphRequest request, CancellationToken ct = default);
}
