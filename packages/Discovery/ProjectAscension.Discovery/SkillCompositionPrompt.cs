namespace ProjectAscension.SkillForge;

/// <summary>
/// Builds the LLM prompt for composing a discovery skill: the theme/context, the
/// whitelisted primitives and their costs, the power budget, and the exact JSON
/// schema to return. Pure and deterministic so it can be unit-tested and reused
/// across providers (Ollama/OpenAI/Claude) via Microsoft.Extensions.AI.
/// </summary>
public static class SkillCompositionPrompt
{
    public static string Build(CompositionRequest request)
    {
        var primitives = string.Join("\n\n", PrimitiveCatalog.All
            .GroupBy(p => p.Category)
            .Select(g => $"{g.Key}:\n" + string.Join(
                "\n", g.Select(p => $"- {p.Kind} (cost {p.BaseCost}): {p.Blurb}"))));
        var tags = request.ContextTags.Count > 0 ? string.Join(", ", request.ContextTags) : "none";

        var lineage = request.Lineage ?? Array.Empty<PriorArt>();
        var lineageSection = lineage.Count == 0
            ? string.Empty
            : "\nThis discovery builds on the player's prior discoveries — extend this lineage, evolve it, do not merely repeat it:\n"
              + string.Join("\n", lineage.Select(a =>
                  $@"- ""{a.Name}"": {a.Description} [{string.Join(", ", a.Primitives.Select(p => $"{p.Kind} x{p.Magnitude}"))}]"))
              + "\n";

        return
$@"You are composing a unique combat skill for a discovery in a fantasy MMOFPS.

Theme: {request.Theme}
Context (equipment / situation): {tags}
Primary behavior to center the skill on: {request.PrimaryBehavior}
Power budget: {request.Budget.Total}.
{lineageSection}
Build the skill ONLY from these effect primitives:
{primitives}

Rules:
- List 1 to 4 primitives in PRIORITY ORDER (most important first). For each give: magnitude (potency, 1 to {CompositionValidator.MaxMagnitude}), and optionally range (reach/area, 0 to {CompositionValidator.MaxParameterTier}) and duration (persistence, 0 to {CompositionValidator.MaxParameterTier}). Omit range/duration (or use 0) when they don't suit the effect.
- Center the skill on the primary behavior's mechanic.
- You do NOT need to do the budget math: the engine scales magnitude and parameters down to fit the power budget, keeping your highest-priority primitives. Focus on a cohesive composition and an evocative name + one-sentence description.
- Write the name and description in English.

Respond with ONLY a JSON object — no prose, no markdown fences:
{{""name"":""..."",""description"":""..."",""primitives"":[{{""kind"":""Projectile"",""magnitude"":2,""range"":1,""duration"":0}}]}}
Each ""kind"" must be exactly one of the primitive names listed above.";
    }
}
