using System.Collections.Generic;
using System.Linq;

namespace ProjectAscension.SkillForge;

/// <summary>
/// Builds the prompt for the FULL skill composition (ADR 0007 Phase 4c) — the AI authors the
/// concept (name + description/lore) AND the structure (effect graph) in one call, so the graph is
/// the sole composed artifact (no separate primitive pass). It reuses the graph vocabulary from
/// <see cref="EffectGraphPrompt"/> and adds the name/description ask plus an actor-wide AVOID list
/// so a new skill doesn't reproduce an existing one's shape (the dedup that keeps discoveries
/// distinct). The engine still owns all numbers (tiers → tuning, ADR 0002).
/// </summary>
public static class SkillGraphPrompt
{
    public static string Build(string theme, IReadOnlyList<BehaviorWeight> profile, PowerBudget budget, IReadOnlyList<string>? avoid = null)
    {
        // The graph rules + steer come from EffectGraphPrompt so the two stay in lockstep.
        string graphSpec = EffectGraphPrompt.Build(theme, profile, budget);

        string avoidLine = avoid is { Count: > 0 }
            ? $"\nAVOID reproducing these existing skills' structures (make yours mechanically distinct):\n  {string.Join("\n  ", avoid.Take(12))}"
            : string.Empty;

        return
$@"You are naming and composing a brand-new discovered skill.
{avoidLine}

First invent a short evocative NAME and a one-sentence DESCRIPTION (flavor/lore, like a real
game's skill text — no numbers). Then compose the skill as the effect graph described below.

{graphSpec}

Respond ONLY as JSON of this exact shape (name + description added to the graph):
{{ ""name"": ""<short name>"", ""description"": ""<one sentence>"", ""trigger"": <TRIGGER>, ""effect"": <NODE> }}";
    }
}
