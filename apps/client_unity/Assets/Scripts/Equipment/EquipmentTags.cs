using System.Collections.Generic;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// The equipment-category tags a loadout contributes to discovery context and skill
    /// binding (ADR 0005). One vocabulary, shared by the discovery observer and the
    /// skill-use gate, so "discovered with a firearm" and "a firearm is equipped" speak
    /// the same words.
    /// </summary>
    public static class EquipmentTags
    {
        public const string Melee = "melee";
        public const string Firearm = "firearm";
        public const string Bow = "bow";
        public const string Arcane = "arcane";

        public static readonly IReadOnlyCollection<string> Vocabulary =
            new HashSet<string> { Melee, Firearm, Bow, Arcane };

        public static string For(WeaponData data)
        {
            if (data == null) return null;
            // A discovered weapon contributes its own context tag, so equipping it opens
            // further discoveries — discover → weapon → discover-again loop (ADR 0005).
            if (!string.IsNullOrEmpty(data.ContextTag)) return data.ContextTag;
            switch (data.EquipmentType)
            {
                case EquipmentType.Weapon: return Melee;
                case EquipmentType.Firearm: return Firearm;
                case EquipmentType.Bow: return Bow;
                case EquipmentType.Catalyst: return Arcane;
                default: return null;
            }
        }

        /// <summary>The equipment tags currently held across both hands.</summary>
        public static HashSet<string> CurrentTags(Loadout loadout)
        {
            var tags = new HashSet<string>();
            if (loadout == null) return tags;
            AddTag(tags, loadout.LeftSlot?.Current?.Data);
            AddTag(tags, loadout.RightSlot?.Current?.Data);
            return tags;
        }

        private static void AddTag(HashSet<string> tags, WeaponData data)
        {
            var tag = For(data);
            if (tag != null) tags.Add(tag);
        }
    }
}
