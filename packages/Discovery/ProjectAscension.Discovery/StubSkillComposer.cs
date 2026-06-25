namespace ProjectAscension.SkillForge;

/// <summary>
/// A deterministic stand-in for the LLM composer. Same request → same skill,
/// always within budget and using only whitelisted primitives. It lets the whole
/// discovery pipeline (fact/content separation, validation, persistence, retry)
/// be built and tested without a model; the LLM composer replaces it in the API
/// shell. Uses a stable hash (not string.GetHashCode, which is per-process random)
/// so output is reproducible across runs.
/// </summary>
public sealed class StubSkillComposer : ISkillComposer
{
    public Task<SkillComposition> ComposeAsync(CompositionRequest request, CancellationToken ct = default)
        => Task.FromResult(Compose(request));

    public static SkillComposition Compose(CompositionRequest request)
    {
        int budget = request.Budget.Total;
        var picked = new List<ComposedPrimitive>();
        int spent = 0;

        // Seed with the behavior's primary mechanic when it fits.
        int primaryCost = PrimitiveCatalog.BaseCostOf(request.PrimaryBehavior);
        if (primaryCost <= budget)
        {
            picked.Add(new ComposedPrimitive(request.PrimaryBehavior, 1));
            spent += primaryCost;
        }

        // Fill the remaining budget in a seed-derived (deterministic) order, so
        // different themes/contexts yield genuinely different skills.
        int seed = StableHash(request.Theme) ^ StableHash(string.Join(",", request.ContextTags));
        foreach (var def in OrderBySeed(PrimitiveCatalog.All, seed))
        {
            if (def.Kind == request.PrimaryBehavior) continue;
            if (spent + def.BaseCost > budget) continue;
            picked.Add(new ComposedPrimitive(def.Kind, 1));
            spent += def.BaseCost;
        }

        // Guarantee a non-empty skill even if the primary didn't fit the budget.
        if (picked.Count == 0)
            picked.Add(new ComposedPrimitive(Cheapest().Kind, 1));

        string name = NameFor(request.ContextTags, picked[0].Kind);
        return new SkillComposition(name, DescribeFor(name, picked), picked);
    }

    private static IEnumerable<PrimitiveDefinition> OrderBySeed(IReadOnlyList<PrimitiveDefinition> defs, int seed)
        => defs.OrderBy(d => unchecked((uint)(StableHash(d.Kind.ToString()) ^ seed)));

    private static PrimitiveDefinition Cheapest()
    {
        var best = PrimitiveCatalog.All[0];
        foreach (var d in PrimitiveCatalog.All)
            if (d.BaseCost < best.BaseCost) best = d;
        return best;
    }

    private static string NameFor(IReadOnlyList<string> tags, PrimitiveKind lead)
    {
        string adj =
            Has(tags, "arcane") ? "Arcane" :
            Has(tags, "firearm") ? "Leaden" :
            Has(tags, "melee") ? "Honed" :
            Has(tags, "bow") ? "Feathered" : "Wild";

        string noun = lead switch
        {
            PrimitiveKind.Projectile => "Bolt",
            PrimitiveKind.Homing => "Seeker",
            PrimitiveKind.Pierce => "Lance",
            PrimitiveKind.Area => "Burst",
            PrimitiveKind.DamageOverTime => "Brand",
            PrimitiveKind.Dash => "Step",
            PrimitiveKind.Knockback => "Shove",
            PrimitiveKind.Shield => "Ward",
            _ => "Art",
        };
        return $"{adj} {noun}";
    }

    private static string DescribeFor(string name, IReadOnlyList<ComposedPrimitive> primitives)
    {
        var parts = primitives.Select(p =>
            PrimitiveCatalog.TryGet(p.Kind, out var d) && d is not null ? d.Blurb : p.Kind.ToString());
        return $"{name} — {string.Join(", ", parts)}.";
    }

    private static bool Has(IReadOnlyList<string> tags, string tag)
    {
        for (int i = 0; i < tags.Count; i++)
            if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // FNV-1a — stable across processes (unlike string.GetHashCode in .NET Core).
    private static int StableHash(string s)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in s)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }
    }
}
