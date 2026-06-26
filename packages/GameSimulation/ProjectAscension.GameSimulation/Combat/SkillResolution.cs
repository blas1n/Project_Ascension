using System.Collections.Generic;
using System.Linq;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>A crowd-control effect a skill applies to a hit target.</summary>
    public enum ControlKind
    {
        None,
        Knockback,
        Slow,
        Stun,
    }

    /// <summary>What a skill does to one target: immediate damage, a damage-over-time
    /// stream, and any control. Indexed back to the caller's target list.</summary>
    public sealed record TargetEffect(
        int TargetIndex,
        float Damage,
        float DamageOverTimePerTick,
        int DamageOverTimeTicks,
        ControlKind Control);

    /// <summary>
    /// The deterministic outcome of executing a skill: per-target effects plus
    /// caster-side results (leech heal, shield, mobility). The caller applies the
    /// damage via <see cref="CombatResolver"/>; numbers are computed here so they are
    /// authoritative and identical on server and client.
    /// </summary>
    public sealed record SkillResolution(
        IReadOnlyList<TargetEffect> Hits,
        float SelfHeal,
        float SelfShield,
        float DashDistance)
    {
        public float ImmediateDamage => Hits.Sum(h => h.Damage);

        public static readonly SkillResolution Empty =
            new(new List<TargetEffect>(), 0f, 0f, 0f);
    }
}
