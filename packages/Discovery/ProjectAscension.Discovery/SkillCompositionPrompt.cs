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
        var deliveries = string.Join("\n", DeliveryStyleCatalog.All.Select(d => $"- {d.Style}: {d.Blurb}"));

        var profile = request.BehaviorProfile ?? Array.Empty<BehaviorWeight>();
        var behaviorSection = profile.Count == 0
            ? string.Empty
            : "\nHOW THE PLAYER FOUGHT — this is the fingerprint that must make this skill UNIQUE. Read the emphasis and let it drive BOTH the effects AND the delivery. Two players with the same equipment who fought differently MUST get mechanically different skills:\n"
              + string.Join("\n", profile.OrderByDescending(b => b.Count).Select(b => $"- {b.Behavior}: {b.Count}"))
              + "\nGuidance (adapt, don't copy):\n"
              + "- sustained ChargedAttack -> a heavy, focused, high-magnitude payload (a beam or one big hit)\n"
              + "- rapid RangedAttack -> many light, fast-flying projectiles\n"
              + "- MeleeAttack -> close-range burst / area effects\n"
              + "- lots of Dodge / Jump -> fast, evasive, mobile delivery (short darts, dash-linked, homing)\n";

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
{behaviorSection}{lineageSection}
Build the skill ONLY from these effect primitives:
{primitives}

Choose how the skill is DELIVERED — pick the style that matches HOW THEY FOUGHT (see above), not a default. The delivery is independent of the effects (a burst can carry damage-over-time, a projectile can carry an area effect):
{deliveries}

Rules:
- List 1 to 4 primitives in PRIORITY ORDER (most important first). For each give: magnitude (potency, 1 to {CompositionValidator.MaxMagnitude}), and optionally range (reach/area, 0 to {CompositionValidator.MaxParameterTier}) and duration (persistence, 0 to {CompositionValidator.MaxParameterTier}). Omit range/duration (or use 0) when they don't suit the effect.
- Let the play pattern above drive the choice of primitives AND delivery — a charging player and a mobile skirmisher with the same weapon should read as clearly different skills. Do NOT default to the same composition every time.
- You do NOT need to do the budget math: the engine scales magnitude and parameters down to fit the power budget, keeping your highest-priority primitives. Focus on a cohesive composition and an evocative name + one-sentence description.
- Write the name and description in English.

Respond with ONLY a JSON object — no prose, no markdown fences:
{{""name"":""..."",""description"":""..."",""delivery"":""projectile"",""primitives"":[{{""kind"":""Projectile"",""magnitude"":2,""range"":1,""duration"":0}}]}}
Each ""kind"" must be exactly one of the primitive names listed above; ""delivery"" must be exactly one of the delivery styles listed above.";
    }
}
