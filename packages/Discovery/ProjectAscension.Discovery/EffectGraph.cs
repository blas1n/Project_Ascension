using System;
using System.Collections.Generic;

namespace ProjectAscension.SkillForge;

// ADR 0007 — a discovered skill's deterministic effect graph (a small AST). The AI composes the
// STRUCTURE (which nodes, under which trigger/order); the engine owns each node's mechanic and
// numbers (ADR 0002). Data only — the interpreter (later phase) executes it deterministically.

/// <summary>When a skill's effect runs. Extensible: a NEW movement mechanic (e.g. wall-climb)
/// is just a new trigger + a stock effect — no bespoke engine field. Wall-climb =
/// Trigger(OnWallContact, Impulse(Up)).</summary>
public enum TriggerKind { OnCast, OnJumpInAir, OnDodge, OnHit, OnWallContact, Continuous }

/// <summary>How an offensive effect is delivered.</summary>
public enum EmitDelivery { Projectile, Beam, Burst, Nova }

/// <summary>Which way a movement impulse pushes.</summary>
public enum ImpulseDirection { Up, Forward, Aim }

public enum ControlEffect { Knockback, Slow, Stun }
public enum WardEffect { Shield, Barrier, Heal, Leech }

/// <summary>A node in the effect graph. A skill is a <see cref="Trigger"/> at the root, whose
/// child is the effect — an <see cref="Emit"/>/<see cref="Impulse"/>/… or a
/// <see cref="Sequence"/> of them.</summary>
public abstract record EffectNode;

public sealed record Trigger(TriggerKind Kind, EffectNode Child) : EffectNode;
public sealed record Sequence(IReadOnlyList<EffectNode> Steps) : EffectNode;
public sealed record Emit(EmitDelivery Delivery, int Tier) : EffectNode;
public sealed record Impulse(ImpulseDirection Direction, int Tier) : EffectNode;
public sealed record Damage(int Tier) : EffectNode;
public sealed record Control(ControlEffect Effect, int Tier) : EffectNode;
public sealed record Ward(WardEffect Effect, int Tier) : EffectNode;

public static class EffectGraph
{
    public const int MaxNodes = 8;
    public const int MaxTier = 3;

    /// <summary>Total nodes in the graph (structural nodes included).</summary>
    public static int NodeCount(EffectNode node) => node switch
    {
        null => 0,
        Trigger t => 1 + NodeCount(t.Child),
        Sequence s => 1 + Sum(s.Steps, NodeCount),
        _ => 1,
    };

    /// <summary>Deterministic power cost (tier-weighted). The engine owns these weights, not the
    /// AI — the AI only chooses tiers/structure (ADR 0002). A Trigger is structural (no cost).</summary>
    public static int Cost(EffectNode node) => node switch
    {
        null => 0,
        Trigger t => Cost(t.Child),
        Sequence s => Sum(s.Steps, Cost),
        Emit e => (e.Tier + 1) * 3,
        Impulse i => (i.Tier + 1) * 4,
        Damage d => (d.Tier + 1) * 3,
        Control c => (c.Tier + 1) * 5,
        Ward w => (w.Tier + 1) * 4,
        _ => 0,
    };

    private static int Sum(IReadOnlyList<EffectNode> nodes, Func<EffectNode, int> f)
    {
        int total = 0;
        if (nodes != null)
            foreach (var n in nodes)
                total += f(n);
        return total;
    }
}
