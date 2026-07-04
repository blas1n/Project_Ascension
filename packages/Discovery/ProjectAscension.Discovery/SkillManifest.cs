namespace ProjectAscension.SkillForge;

/// <summary>How a discovered skill manifests for the player to use.</summary>
public enum ManifestationKind
{
    /// <summary>A castable, offensive composition — synthesized magic that becomes a
    /// new equippable weapon the player aims and fires.</summary>
    Weapon,

    /// <summary>A mobility / control technique — a command the player actively invokes
    /// by a button combo (double jump, dodge-slash, a stun burst).</summary>
    Command,

    /// <summary>A defensive composition — an always-on passive (persistent ward /
    /// damage reduction / lifesteal), not invoked.</summary>
    Passive,
}

/// <summary>
/// Classifies a composed skill into how the player wields it (design note: "magic
/// synthesized from magic becomes a new weapon; everything else is a command"). The
/// decision is deterministic and server-authoritative — driven by which primitive
/// category dominates the composition's power, not by the AI.
/// </summary>
public static class SkillManifest
{
    public static ManifestationKind Classify(SkillComposition composition)
    {
        int offensive = 0;
        int control = 0;
        int mobility = 0;
        int defensive = 0;
        foreach (var p in composition.Primitives)
        {
            if (!PrimitiveCatalog.TryGet(p.Kind, out var def) || def is null) continue;
            switch (def.Category)
            {
                case PrimitiveCategory.Offensive: offensive += p.Magnitude; break;
                case PrimitiveCategory.Control: control += p.Magnitude; break;
                case PrimitiveCategory.Mobility: mobility += p.Magnitude; break;
                case PrimitiveCategory.Defensive: defensive += p.Magnitude; break;
            }
        }

        // Offensive-dominant → a Weapon you aim and fire. Otherwise:
        //   Control-dominant → a Command you actively invoke (stun burst, knockback).
        //   Mobility-dominant → a Passive movement CAPABILITY (double jump, extra dash) —
        //     used via the movement input, not cast, so it's not a hotkey ability.
        //   Defensive-dominant → a Passive ward/sustain.
        if (offensive >= control && offensive >= mobility && offensive >= defensive)
            return ManifestationKind.Weapon;
        if (control >= mobility && control >= defensive)
            return ManifestationKind.Command;
        return ManifestationKind.Passive; // mobility or defensive
    }
}
