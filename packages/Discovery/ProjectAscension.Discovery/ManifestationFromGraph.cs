using System;
namespace ProjectAscension.SkillForge;

/// <summary>
/// Derives how a skill manifests (Weapon / Command / Passive) from its effect GRAPH (ADR 0007)
/// rather than the flat primitive bag — so the taxonomy follows the structure the AI actually
/// composed. Same rules as <see cref="SkillManifest"/>, sourced from the graph's top trigger and
/// the category its effects weigh toward:
/// <list type="bullet">
/// <item>A movement trigger (OnJumpInAir/OnWallContact) → Passive (a movement capability).</item>
/// <item>OnDodge → Command if it attacks/controls (a dodge tech), else Passive (a movement dodge).</item>
/// <item>Continuous / OnHit → Passive (an always-on ward or an on-hit rider).</item>
/// <item>OnCast (and default) → offensive-dominant becomes a Weapon only when magic-from-magic
///   (ADR 0005), else a Command; control-dominant → Command; mobility/defensive → Passive.</item>
/// </list>
/// Deterministic and server-authoritative (the AI chose structure, not the classification).
/// Returns null when there is no graph — the caller falls back to the primitive classifier.
/// </summary>
public static class ManifestationFromGraph
{
    /// <summary>
    /// A WEAPON is not "you were carrying a catalyst". Making a weapon is how the game expresses MAGIC
    /// SYNTHESIS — "화기 + 술식 → 마력 탄환" — so it takes an actual FUSION: two hands woven into one act,
    /// one of them magic (ADR 0011). A single spell, however fierce, is a technique you invoke: a COMMAND.
    ///
    /// Judged on what the player DID (ADR 0009's Fuse:), never on what they happened to be holding.
    /// </summary>
    public static bool IsMagicFusion(IReadOnlyList<string>? behaviors)
    {
        if (behaviors == null) return false;
        foreach (var b in behaviors)
        {
            if (b == null || !b.StartsWith("Fuse:", StringComparison.Ordinal)) continue;
            var pair = b.Substring("Fuse:".Length).Split('>');
            if (pair.Length != 2) continue;
            // Two DIFFERENT hands, one of them magic: that is a synthesis, and a synthesis makes a thing.
            if (pair[0] != pair[1] && (pair[0] == "arcane" || pair[1] == "arcane")) return true;
        }
        return false;
    }

    public static ManifestationKind? Classify(EffectNode? graph, bool magicFusion)
    {
        if (graph is not Trigger trigger) return null;

        var (offensive, control, mobility, defensive) = Weigh(trigger.Child);

        switch (trigger.Kind)
        {
            case TriggerKind.OnJumpInAir:
            case TriggerKind.OnWallContact:
                return ManifestationKind.Passive; // a movement capability

            case TriggerKind.OnDodge:
                // A dodge that also attacks/controls is an invoked tech; a pure movement dodge is a passive.
                return offensive > 0 || control > 0 ? ManifestationKind.Command : ManifestationKind.Passive;

            case TriggerKind.Continuous:
            case TriggerKind.OnHit:
                return ManifestationKind.Passive; // always-on ward / on-hit rider
        }

        // A graph with no weighted effect at all (shouldn't pass validation) is never a weapon.
        if (offensive == 0 && control == 0 && mobility == 0 && defensive == 0)
            return ManifestationKind.Passive;

        // OnCast (and any default): dominant category decides, mirroring SkillManifest.
        if (offensive >= control && offensive >= mobility && offensive >= defensive)
            return magicFusion ? ManifestationKind.Weapon : ManifestationKind.Command;
        if (control >= mobility && control >= defensive)
            return ManifestationKind.Command;
        return ManifestationKind.Passive; // mobility or defensive
    }

    // Weigh the effect subtree by category (tier-weighted). The engine owns these weights.
    private static (int Offensive, int Control, int Mobility, int Defensive) Weigh(EffectNode node)
    {
        switch (node)
        {
            case Sequence s:
                int off = 0, ctrl = 0, mob = 0, def = 0;
                foreach (var step in s.Steps)
                {
                    var (o, c, m, d) = Weigh(step);
                    off += o; ctrl += c; mob += m; def += d;
                }
                return (off, ctrl, mob, def);
            case Emit e: return (e.Tier + 1, 0, 0, 0);
            case Damage dm: return (dm.Tier + 1, 0, 0, 0);
            case Dot dot: return (dot.Tier + 1, 0, 0, 0);  // damage over time — offensive
            case Spread sp: return (sp.Tier + 1, 0, 0, 0); // extra targets — an offensive rider
            case Homing h: return (h.Tier + 1, 0, 0, 0);   // seeking — an offensive rider
            case Control c2: return (0, c2.Tier + 1, 0, 0);
            case Impulse i: return (0, 0, i.Tier + 1, 0);
            case Ward w: return (0, 0, 0, w.Tier + 1);
            case Trigger t: return Weigh(t.Child); // shouldn't nest, but be total
            default: return (0, 0, 0, 0);
        }
    }
}
