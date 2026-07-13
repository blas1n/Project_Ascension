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

// Offensive riders (ADR 0007 Phase 4b) — bring the graph to primitive parity so the offensive
// interpreter doesn't regress combat variety. Dot = damage over time; Spread = extra targets
// (chain/fork/pierce collapse into one — same numbers, delivery carries the look); Homing =
// targeting aid (no numbers, changes projectile behaviour/VFX only).
public sealed record Dot(int Tier, int Duration) : EffectNode;
public sealed record Spread(int Tier) : EffectNode;
public sealed record Homing(int Tier) : EffectNode;

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

    /// <summary>
    /// The cost of a skill is STRUCTURAL, not numeric (ADR 0010). It counts what the skill DOES —
    /// how many effects, and how expressive each is — and deliberately ignores Tier.
    ///
    /// This is the doc, made mechanical. progression-model.md: "신규 플레이어와 100시간 플레이어의 차이는
    /// 숫자가 아니다… 성장은 강함이 아니라 세계 속 위치의 변화이다." And discovery.md: the power budget's
    /// job is that "수치가 클램프된다" — to CLAMP the numbers, not to sell them.
    ///
    /// So magnitude is not for sale. It is bounded flat by <see cref="MaxTier"/> and costs nothing, which
    /// means a significant discovery cannot buy a bigger number — it can only buy a more INTERESTING one.
    /// A legendary skill and a common one hit about as hard. They differ in what they do to you.
    ///
    /// The engine owns these weights, never the AI (ADR 0002). A Trigger is structural (no cost).
    /// </summary>
    public static int Cost(EffectNode node) => node switch
    {
        null => 0,
        Trigger t => Cost(t.Child),
        Sequence s => Sum(s.Steps, Cost),
        Emit => 3,
        Impulse => 4,
        Damage => 3,
        Control => 5,  // bending what an enemy can DO is the most expressive thing a skill can hold
        Ward => 4,
        Dot => 4,
        Spread => 3,
        Homing => 2,
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
