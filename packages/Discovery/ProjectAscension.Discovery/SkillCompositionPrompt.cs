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
        var primitives = string.Join(
            "\n", PrimitiveCatalog.All.Select(p => $"- {p.Kind} (cost {p.BaseCost}): {p.Blurb}"));
        var tags = request.ContextTags.Count > 0 ? string.Join(", ", request.ContextTags) : "none";

        return
$@"You are composing a unique combat skill for a discovery in a fantasy MMOFPS.

Theme: {request.Theme}
Context (equipment / situation): {tags}
Primary behavior to center the skill on: {request.PrimaryBehavior}
Power budget: {request.Budget.Total}.

Build the skill ONLY from these effect primitives:
{primitives}

Rules:
- List 1 to 4 primitives in PRIORITY ORDER (most important first), each with a desired magnitude from 1 to {CompositionValidator.MaxMagnitude}.
- Center the skill on the primary behavior's mechanic.
- You do NOT need to do the budget math: the engine scales magnitudes down to fit the power budget, keeping your highest-priority primitives. Focus on a cohesive composition and an evocative name + one-sentence description.
- Write the name and description in English.

Respond with ONLY a JSON object — no prose, no markdown fences:
{{""name"":""..."",""description"":""..."",""primitives"":[{{""kind"":""Projectile"",""magnitude"":1}}]}}
Each ""kind"" must be exactly one of the primitive names listed above.";
    }
}
