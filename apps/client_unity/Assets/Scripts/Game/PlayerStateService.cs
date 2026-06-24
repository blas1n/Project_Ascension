using System.Collections.Generic;
using ProjectAscension.Equipment;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Session-wide player state: currency, owned weapons, and the selected
    /// loadout (chosen in the City, applied in the Frontier).
    /// </summary>
    public sealed class PlayerStateService
    {
        public int Currency;
        public IReadOnlyList<WeaponData> OwnedWeapons { get; }
        public WeaponData SelectedLeft { get; private set; }
        public WeaponData SelectedRight { get; private set; }

        public PlayerStateService(IReadOnlyList<WeaponData> ownedWeapons)
        {
            OwnedWeapons = ownedWeapons;
            if (ownedWeapons.Count > 0) SelectedLeft = ownedWeapons[0];
            if (ownedWeapons.Count > 1) SelectedRight = ownedWeapons[1];
        }

        public void SetLeft(WeaponData weapon) => SelectedLeft = weapon;
        public void SetRight(WeaponData weapon) => SelectedRight = weapon;
    }
}
