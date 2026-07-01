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
    private static readonly string[] AttackBehaviors = { "ChargedAttack", "RangedAttack", "MeleeAttack" };

    /// <summary>The dominant attack behavior, or "-" when the player didn't attack. Used both
    /// to derive the delivery and (indirectly) to keep the claim keyed on play style.</summary>
    public static string DominantAttack(IReadOnlyList<BehaviorCount> behaviors)
    {
        BehaviorCount? top = null;
        foreach (var b in behaviors)
            if (b.Count > 0 && Array.IndexOf(AttackBehaviors, b.Behavior) >= 0 && (top is null || b.Count > top.Count))
                top = b;
        return top?.Behavior ?? "-";
    }

    /// <summary>The fallback manifestation when the LLM omits a delivery — the same grid the
    /// prompt guides the model with: attack decides beam/projectile, mobility the mobile
    /// variant. Charged+still → beam, charged+mobile → nova, rapid+still → projectile,
    /// rapid+mobile → arc, melee → burst.</summary>
    public static string ForBehavior(IReadOnlyList<BehaviorCount> behaviors)
    {
        var attack = DominantAttack(behaviors);
        if (attack == "MeleeAttack") return "burst";
        if (attack == "-") return "beam";

        int mobility = 0, attackCount = 0;
        foreach (var b in behaviors)
        {
            if (b.Behavior is "Jump" or "Dodge") mobility += b.Count;
            else if (b.Behavior == attack) attackCount += b.Count;
        }
        bool high = mobility * 2 >= attackCount; // movement at least half the attack count

        return attack switch
        {
            "ChargedAttack" => high ? "nova" : "beam",
            "RangedAttack" => high ? "arc" : "projectile",
            _ => "beam",
        };
    }
}
