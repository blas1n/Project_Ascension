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
        int offensive = 0; // Offensive
        int active = 0;    // Mobility + Control
        int defensive = 0; // Defensive
        foreach (var p in composition.Primitives)
        {
            if (!PrimitiveCatalog.TryGet(p.Kind, out var def) || def is null) continue;
            switch (def.Category)
            {
                case PrimitiveCategory.Offensive: offensive += p.Magnitude; break;
                case PrimitiveCategory.Defensive: defensive += p.Magnitude; break;
                default: active += p.Magnitude; break; // Mobility / Control
            }
        }

        // Offensive-dominant → a weapon you aim and fire. Otherwise mobility/control
        // (an actively invoked technique) → a command; defensive-dominant (persistent
        // protection/sustain) → an always-on passive.
        if (offensive >= active && offensive >= defensive) return ManifestationKind.Weapon;
        return active >= defensive ? ManifestationKind.Command : ManifestationKind.Passive;
    }
}
