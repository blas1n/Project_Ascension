using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectAscension.SkillForge;

/// <summary>What the AI needs to compose a skill's effect graph (ADR 0007): the theme, how the
/// player fought (steers the root trigger), the power budget, and a seed so the composition is
/// reproducible per discovery.</summary>
public sealed record EffectGraphRequest(
    string Theme,
    IReadOnlyList<BehaviorWeight> Profile,
    PowerBudget Budget,
    long Seed);

/// <summary>Composes a skill's STRUCTURE as an effect graph (ADR 0007). The AI owns which trigger
/// and which effects, in what order; the engine owns the numbers and executes deterministically
/// (ADR 0002). Returns null when the model produces nothing valid within budget — the graph is
/// additive to the primitive skill for now, so a null means "no runtime graph yet", not a defer.</summary>
public interface IEffectGraphComposer
{
    Task<EffectNode?> ComposeAsync(EffectGraphRequest request, CancellationToken ct = default);
}
