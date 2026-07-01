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

    /// <summary>Charged → a focused beam, rapid ranged → flying projectiles, melee → a close
    /// burst — so how the player fought visibly shapes how the skill manifests.</summary>
    public static string ForBehavior(IReadOnlyList<BehaviorCount> behaviors) => DominantAttack(behaviors) switch
    {
        "ChargedAttack" => "beam",
        "MeleeAttack" => "burst",
        "RangedAttack" => "projectile",
        _ => "beam",
    };
}
