using System;
using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// What a discovered skill is BOUND to (ADR 0011, revising ADR 0005).
    ///
    /// A skill found by USING a weapon belongs to that weapon. "화기 + 술식 → 마력 탄환" is not a thing you
    /// know; it is a thing you can do WITH A GUN IN YOUR HAND. Put the gun down and it is gone — not
    /// forgotten, just unusable, the way a swordsman's technique means nothing to a bowman. That is what
    /// stops a player hoarding every style's tricks and carrying them all at once.
    ///
    /// A skill found WITHOUT a weapon belongs to the body. Double-jumping was learned by jumping, not by
    /// shooting, and nobody takes it away when you sheathe your sword.
    ///
    /// And it binds ONLY the weapons that actually TOOK PART. Holding a catalyst while you shoot does
    /// not make the catalyst complicit — if you never wove it in, the skill is the gun's alone. Only a
    /// genuine fusion of two hands is owed to both. So the evidence is the BEHAVIOUR (what you did), not
    /// the loadout (what you happened to be carrying) — and ADR 0009's grammar already carries the
    /// weapon in the name: "Fuse:arcane&gt;firearm" implicates two; "Chain:firearm" implicates one.
    ///
    /// The same rule serves commands and passives. Pure and headless-tested (ADR: Unity is a shell).
    /// </summary>
    public static class SkillBinding
    {
        /// <summary>Provenance keys — "Use:firearm" — that record WHICH instrument an act was made with.
        /// They score nothing (they are evidence, not achievement); they exist so a skill can be bound
        /// to the weapons that actually made it.</summary>
        public const string UsePrefix = "Use:";

        /// <summary>The weapon CATEGORIES that can bind a skill. A discovered weapon's own "spell:" tag
        /// does not (a skill must not be bound to the skill that made it), nor do situation tags.</summary>
        public static readonly IReadOnlyCollection<string> WeaponTags =
            new HashSet<string> { "melee", "firearm", "bow", "arcane" };

        /// <summary>
        /// The weapons that TOOK PART in this discovery — read out of the behaviours, which name their
        /// instrument. Empty when nothing was used but the player's own body, in which case the skill is
        /// theirs to keep.
        /// </summary>
        public static IReadOnlyCollection<string> RequiredEquipment(IReadOnlyList<string>? behaviors)
        {
            if (behaviors == null) return Array.Empty<string>();

            var required = new List<string>();
            foreach (var b in behaviors)
            {
                if (string.IsNullOrEmpty(b)) continue;
                foreach (var weapon in WeaponTags)
                {
                    // A behaviour NAMES its instrument ("Use:firearm", "Fuse:arcane>firearm",
                    // "Seq:jump>melee"). Only the ones named took part.
                    if (!required.Contains(weapon) && NamesWeapon(b, weapon))
                        required.Add(weapon);
                }
            }
            return required;
        }

        /// <summary>Whether the skill can be used with what is in the player's hands right now.</summary>
        public static bool Usable(IReadOnlyList<string>? behaviors, ICollection<string>? equipped)
        {
            var required = RequiredEquipment(behaviors);
            if (required.Count == 0) return true; // the body's, not the weapon's

            if (equipped == null) return false;
            foreach (var r in required)
                if (!equipped.Contains(r)) return false;
            return true;
        }

        // Match on token boundaries so "melee" isn't found inside some longer word, and so a weapon is
        // implicated only where the grammar actually named it.
        private static bool NamesWeapon(string behavior, string weapon)
        {
            int i = behavior.IndexOf(weapon, StringComparison.Ordinal);
            while (i >= 0)
            {
                bool startOk = i == 0 || !char.IsLetter(behavior[i - 1]);
                int end = i + weapon.Length;
                bool endOk = end == behavior.Length || !char.IsLetter(behavior[end]);
                if (startOk && endOk) return true;
                i = behavior.IndexOf(weapon, i + 1, StringComparison.Ordinal);
            }
            return false;
        }
    }
}
