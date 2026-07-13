using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Services;

/// <summary>
/// Derives a skill's delivery from HOW the player fought. The LLM composes the effect well
/// but its own delivery pick converges to one style (the variety simulation measures ~2 of
/// 5), so the manifestation is decided here instead: the dominant attack behavior maps to a
/// delivery, giving reliable, play-driven variety. Deterministic and pure — unit-tested.
/// </summary>
public static class DeliveryHeuristics
{
    // The attack behaviors that define a skill's character (movement only flavors it).
    private static readonly string[] AttackBehaviors = { "RangedAttack", "MeleeAttack" };

    /// <summary>Charging is no longer a behaviour of its own — since ADR 0009 it is a QUALITY of the
    /// act ("While:firearm@charged"). This branch used to test for a literal "ChargedAttack" that the
    /// game stopped sending, so charged play could never reach beam or nova and the delivery variety
    /// was quietly halved. Read the quality the grammar actually emits.</summary>
    private const string ChargedQuality = "@charged";

    /// <summary>The name of the derived charged signal — kept as the label the delivery grid speaks.</summary>
    public const string ChargedAttack = "ChargedAttack";

    private static int ChargedCount(IReadOnlyList<BehaviorCount> behaviors)
    {
        int charged = 0;
        foreach (var b in behaviors)
            if (b.Count > 0 && b.Behavior != null && b.Behavior.Contains(ChargedQuality, StringComparison.Ordinal))
                charged += b.Count;
        return charged;
    }

    /// <summary>The dominant attack behavior, or "-" when the player didn't attack. Used both
    /// to derive the delivery and (indirectly) to keep the claim keyed on play style.</summary>
    public static string DominantAttack(IReadOnlyList<BehaviorCount> behaviors)
    {
        BehaviorCount? top = null;
        foreach (var b in behaviors)
            if (b.Count > 0 && Array.IndexOf(AttackBehaviors, b.Behavior) >= 0 && (top is null || b.Count > top.Count))
                top = b;

        int charged = ChargedCount(behaviors);
        if (charged > 0 && (top is null || charged > top.Count)) return ChargedAttack;

        return top?.Behavior ?? "-";
    }

    /// <summary>The fallback manifestation when the LLM omits a delivery — the same grid the
    /// prompt guides the model with: attack decides beam/projectile, mobility the mobile
    /// variant. Charged+still → beam, charged+mobile → nova, rapid+still → projectile,
    /// rapid+mobile → arc, melee → burst.</summary>
    private static readonly string[] Movement = { "Jump" };

    public static string ForBehavior(IReadOnlyList<BehaviorCount> behaviors)
    {
        var attack = DominantAttack(behaviors);
        if (attack == "-") return "beam";

        int charged = ChargedCount(behaviors);
        int mobility = 0, totalAttacks = charged, dominantCount = attack == ChargedAttack ? charged : 0;
        foreach (var b in behaviors)
        {
            if (Array.IndexOf(Movement, b.Behavior) >= 0) mobility += b.Count;
            else if (Array.IndexOf(AttackBehaviors, b.Behavior) >= 0) totalAttacks += b.Count;
            if (b.Behavior == attack) dominantCount += b.Count;
        }

        // Movement-dominated play (over 1.5x the attacks) is a self-cast technique (a Command);
        // it erupts around the caster. Mirrors the prompt's classification.
        if (mobility * 2 > totalAttacks * 3) return "nova";
        if (attack == "MeleeAttack") return "burst";

        bool high = mobility * 2 >= dominantCount; // movement at least half the attack count
        return attack switch
        {
            ChargedAttack => high ? "nova" : "beam",
            "RangedAttack" => high ? "arc" : "projectile",
            _ => "beam",
        };
    }
}
