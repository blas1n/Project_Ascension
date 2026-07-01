namespace ProjectAscension.SkillForge;

/// <summary>
/// Builds the LLM prompt for composing a discovery skill: the theme/context, the
/// whitelisted primitives and their costs, the power budget, and the exact JSON
/// schema to return. Pure and deterministic so it can be unit-tested and reused
/// across providers (Ollama/OpenAI/Claude) via Microsoft.Extensions.AI.
/// </summary>
public static class SkillCompositionPrompt
{
    private static readonly string[] Attacks = { "ChargedAttack", "RangedAttack", "MeleeAttack" };

    // Pre-classify the raw counts so the model doesn't have to infer "charged vs rapid" and
    // "mobile vs stationary" itself (it mis-inferred that) — it just applies the delivery grid.
    private static string ClassifyPlay(IReadOnlyList<BehaviorWeight> profile)
    {
        int attacks = profile.Where(b => Attacks.Contains(b.Behavior)).Sum(b => b.Count);
        int mobility = profile.Where(b => b.Behavior is "Jump" or "Dodge").Sum(b => b.Count);
        if (attacks == 0 && mobility == 0) return "no clear pattern";

        // Movement-DOMINATED play (moved far more than attacked — over 1.5x) is a TECHNIQUE,
        // a Command the player invokes, not an offensive weapon. A player who also attacks a
        // lot is an evasive attacker (a weapon), not a technique. This is the "magic + non-
        // magic -> a command, not a new weapon" path.
        if (mobility * 2 > attacks * 3)
            return "a MOBILITY TECHNIQUE (moved far more than attacked). Compose it ONLY from Mobility (Dash, Blink) and Control (Knockback, Slow, Stun) primitives. Do NOT include ANY Offensive primitive (no Projectile, Beam, Area, DamageOverTime, Chain, Fork, Pierce, Homing) — it is an invoked movement/control move (a Command), not an attack";

        var dominant = profile.Where(b => Attacks.Contains(b.Behavior)).OrderByDescending(b => b.Count).First();
        bool high = mobility * 2 >= dominant.Count; // movement is at least half the attack count
        var attack = dominant.Behavior switch
        {
            "ChargedAttack" => "CHARGED/sustained",
            "RangedAttack" => "RAPID ranged",
            "MeleeAttack" => "MELEE",
            _ => dominant.Behavior,
        };
        return dominant.Behavior == "MeleeAttack"
            ? "attack = MELEE"
            : $"attack = {attack}; mobility = {(high ? "HIGH (weaving/leaping)" : "LOW (standing ground)")}";
    }

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
              + $"\nPLAY CLASSIFICATION (use this directly with the delivery grid below): {ClassifyPlay(profile)}\n"
              + "Effect guidance (adapt, don't copy): sustained charge -> a heavy focused payload; rapid -> many light fast hits; melee -> close burst/area; high mobility -> evasive, homing, dash-linked. A MOBILITY TECHNIQUE is the exception: use ONLY Mobility + Control primitives (Dash, Blink, Knockback, Slow, Stun) and NO Offensive primitives at all.\n";

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
Weapon's base mechanic: {request.PrimaryBehavior} (context only — do NOT force the delivery to match it; the PLAY CLASSIFICATION below decides the delivery)
Power budget: {request.Budget.Total}.
{behaviorSection}{lineageSection}
Build the skill ONLY from these effect primitives:
{primitives}

Choose how the skill is DELIVERED strictly from the PLAY CLASSIFICATION above. The ATTACK decides beam vs projectile; the MOBILITY decides the mobile variant. Match this grid EXACTLY:
- attack CHARGED + mobility LOW  -> beam
- attack CHARGED + mobility HIGH -> nova
- attack RAPID   + mobility LOW  -> projectile   (a stream of bolts — NOT a beam; beam is only for CHARGED)
- attack RAPID   + mobility HIGH -> arc
- attack MELEE                   -> burst
- a MOBILITY TECHNIQUE           -> nova (it erupts around the caster)
Do NOT collapse RAPID into beam, and do NOT default everything to one style. Delivery is independent of the effects. Pick exactly ONE:
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
