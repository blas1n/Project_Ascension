namespace ProjectAscension.SkillForge;

/// <summary>How a discovered skill manifests for the player to use.</summary>
public enum ManifestationKind
{
    /// <summary>A castable, offensive composition — synthesized magic that becomes a
    /// new equippable weapon the player aims and fires.</summary>
    Weapon,

    /// <summary>A mobility / control / defensive technique — a command the player
    /// invokes (double jump, dodge-slash, a ward).</summary>
    Command,
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
        int other = 0;
        foreach (var p in composition.Primitives)
        {
            if (!PrimitiveCatalog.TryGet(p.Kind, out var def) || def is null) continue;
            if (def.Category == PrimitiveCategory.Offensive) offensive += p.Magnitude;
            else other += p.Magnitude;
        }

        // An offensive-dominant composition is a spell you aim and fire → a weapon.
        // Anything else (mobility/control/defensive) → a command you invoke.
        return offensive > other ? ManifestationKind.Weapon : ManifestationKind.Command;
    }
}
