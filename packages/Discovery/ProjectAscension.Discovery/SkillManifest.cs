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
    /// <param name="magicContext">Whether the discovery was made with MAGIC (an arcane
    /// catalyst / a spell-weapon). A new WEAPON is "magic synthesized from magic" (ADR 0005) —
    /// so only an offensive discovery in a magic context becomes an equippable weapon; a
    /// NON-magic offensive discovery (firearm/bow/blade) is a cast hotkey Command instead.</param>
    public static ManifestationKind Classify(SkillComposition composition, bool magicContext)
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

        // Offensive-dominant → a WEAPON only when magic-synthesized-from-magic (ADR 0005);
        //   a non-magic offensive discovery is a cast hotkey COMMAND, not an equippable weapon.
        // Control-dominant → a Command (actively invoked: stun burst, knockback).
        // Mobility-dominant → a Passive movement CAPABILITY (double jump), used via movement.
        // Defensive-dominant → a Passive ward/sustain.
        if (offensive >= control && offensive >= mobility && offensive >= defensive)
            return magicContext ? ManifestationKind.Weapon : ManifestationKind.Command;
        if (control >= mobility && control >= defensive)
            return ManifestationKind.Command;
        return ManifestationKind.Passive; // mobility or defensive
    }

    /// <summary>Does a discovery's context indicate MAGIC — an arcane catalyst or a
    /// synthesized spell-weapon (its "spell:" tag)? Only then can an offensive synthesis
    /// become a new weapon.</summary>
    public static bool IsMagicContext(IEnumerable<string> contextTags)
    {
        if (contextTags is null) return false;
        foreach (var t in contextTags)
            if (string.Equals(t, "arcane", System.StringComparison.OrdinalIgnoreCase)
                || (t != null && t.StartsWith("spell:", System.StringComparison.OrdinalIgnoreCase)))
                return true;
        return false;
    }
}
