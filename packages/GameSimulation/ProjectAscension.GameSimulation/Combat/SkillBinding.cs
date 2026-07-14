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

        /// <summary>A discovered (forged) weapon's own context tag prefix — "spell:emberbrand". Distinct
        /// from <see cref="WeaponTags"/>: those four are a small, STABLE vocabulary; this is an
        /// unbounded one, a fresh word per forged item.</summary>
        public const string SpellPrefix = "spell:";

        /// <summary>The base weapon CATEGORIES — the small, stable vocabulary used to key the discovery
        /// LADDER (<c>SkillCompositionService.RegionKey</c>, ADR 0010/0011). A discovered weapon's own
        /// "spell:" tag is deliberately NOT one of these: filing a discovery under the specific item that
        /// made it would let a player equip their own find and farm a fresh, easy ladder per skill — the
        /// exact snowball ADR 0011 closed. Do not widen this set for that reason; see
        /// <see cref="BoundInstruments"/> for the (different) question of what a COMMAND/PASSIVE needs
        /// equipped to be usable.</summary>
        public static readonly IReadOnlyCollection<string> WeaponTags =
            new HashSet<string> { "melee", "firearm", "bow", "arcane" };

        /// <summary>
        /// The base weapon CATEGORIES that TOOK PART in this discovery — read out of the behaviours,
        /// which name their instrument. Empty when nothing was used but the player's own body (or only a
        /// forged weapon — see <see cref="BoundInstruments"/>), in which case nothing here claims it.
        ///
        /// This is the discovery LADDER's vocabulary, not the use-gate's — see <see cref="WeaponTags"/>.
        /// For "can this skill be CAST right now", use <see cref="BoundInstruments"/> instead.
        /// </summary>
        public static IReadOnlyCollection<string> RequiredEquipment(IReadOnlyList<string>? behaviors)
            => Bound(behaviors, includeForged: false);

        /// <summary>
        /// Everything that actually TOOK PART in this discovery and can gate its USE: the base weapon
        /// categories (<see cref="WeaponTags"/>) AND a specific forged weapon's own "spell:" tag. A
        /// technique discovered by casting a forged weapon (alone, or fused with a body verb like jump)
        /// still names an instrument that took part — ADR 0011 binds a skill to what MADE it, and a
        /// one-off forged weapon is as much a maker as a firearm. Leaving it out of the GATE (as
        /// <see cref="RequiredEquipment"/> must, for the ladder — see its doc) makes such a command
        /// unconditionally assignable no matter what is equipped, which is not "usable with what's in
        /// your hands right now" — it is "usable with anything", the reported bug.
        ///
        /// Equipping a forged weapon already produces this exact tag (<c>EquipmentTags.For</c> returns a
        /// weapon's own ContextTag first), so the vocabulary this reads matches the vocabulary equipping
        /// produces — no new "dead key".
        /// </summary>
        public static IReadOnlyCollection<string> BoundInstruments(IReadOnlyList<string>? behaviors)
            => Bound(behaviors, includeForged: true);

        private static IReadOnlyCollection<string> Bound(IReadOnlyList<string>? behaviors, bool includeForged)
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

                if (includeForged)
                {
                    var forged = ForgedWeaponTag(b);
                    if (forged != null && !required.Contains(forged)) required.Add(forged);
                }
            }
            return required;
        }

        /// <summary>Whether the skill can be used with what is in the player's hands right now.</summary>
        public static bool Usable(IReadOnlyList<string>? behaviors, ICollection<string>? equipped)
        {
            var required = BoundInstruments(behaviors);
            if (required.Count == 0) return true; // the body's, not any weapon's

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

        // Extracts a forged weapon's own tag ("spell:emberbrand") out of a behaviour token
        // ("Use:spell:emberbrand", "Fuse:jump>spell:emberbrand"). Same token-boundary discipline as
        // NamesWeapon: the prefix must start at a word boundary, and the slug runs while it's a
        // slug character (matches DiscoveredSkillFactory.Slug on the client — letters/digits/'-').
        private static string? ForgedWeaponTag(string behavior)
        {
            int i = behavior.IndexOf(SpellPrefix, StringComparison.Ordinal);
            if (i < 0) return null;
            if (i != 0 && char.IsLetter(behavior[i - 1])) return null;

            int end = i + SpellPrefix.Length;
            while (end < behavior.Length && (char.IsLetterOrDigit(behavior[end]) || behavior[end] == '-')) end++;
            return end > i + SpellPrefix.Length ? behavior.Substring(i, end - i) : null;
        }
    }
}
