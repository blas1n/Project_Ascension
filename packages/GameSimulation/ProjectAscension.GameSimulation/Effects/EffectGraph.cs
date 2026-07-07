using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Effects
{
    // ADR 0007 — the client-side mirror of a discovered skill's effect graph. Mirrors the server
    // model (ProjectAscension.SkillForge.EffectGraph); the names/tokens match so the JSON the API
    // serves (EffectGraphReader) parses straight in. Data only — the runtime interprets it
    // (movement now; offense in a later phase). Record CLASSES (not struct) for Unity C# 9.

    /// <summary>When a skill's effect runs. A NEW movement mechanic (e.g. wall-climb) is just a
    /// new trigger + a stock effect — no bespoke field.</summary>
    public enum TriggerKind { OnCast, OnJumpInAir, OnDodge, OnHit, OnWallContact, Continuous }

    public enum EmitDelivery { Projectile, Beam, Burst, Nova }
    public enum ImpulseDirection { Up, Forward, Aim }
    public enum ControlEffect { Knockback, Slow, Stun }
    public enum WardEffect { Shield, Barrier, Heal, Leech }

    /// <summary>A node in the effect graph. A skill is a <see cref="Trigger"/> at the root whose
    /// child is the effect — a leaf or a <see cref="Sequence"/> of them.</summary>
    public abstract record EffectNode;

    public sealed record Trigger(TriggerKind Kind, EffectNode Child) : EffectNode;
    public sealed record Sequence(IReadOnlyList<EffectNode> Steps) : EffectNode;
    public sealed record Emit(EmitDelivery Delivery, int Tier) : EffectNode;
    public sealed record Impulse(ImpulseDirection Direction, int Tier) : EffectNode;
    public sealed record Damage(int Tier) : EffectNode;
    public sealed record Control(ControlEffect Effect, int Tier) : EffectNode;
    public sealed record Ward(WardEffect Effect, int Tier) : EffectNode;
}
