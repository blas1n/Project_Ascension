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
        private readonly List<WeaponData> _owned;
        public IReadOnlyList<WeaponData> OwnedWeapons => _owned;
        public WeaponData SelectedLeft { get; private set; }
        public WeaponData SelectedRight { get; private set; }

        public PlayerStateService(IReadOnlyList<WeaponData> ownedWeapons)
        {
            _owned = new List<WeaponData>(ownedWeapons);
            if (_owned.Count > 0) SelectedLeft = _owned[0];
            if (_owned.Count > 1) SelectedRight = _owned[1];
        }

        public void SetLeft(WeaponData weapon) => SelectedLeft = weapon;
        public void SetRight(WeaponData weapon) => SelectedRight = weapon;

        /// <summary>Add a weapon to the inventory (e.g. a discovered weapon). Persists
        /// for the session, so it survives City&lt;-&gt;Frontier and can be re-equipped.</summary>
        public void AddWeapon(WeaponData weapon)
        {
            if (weapon != null && !_owned.Contains(weapon)) _owned.Add(weapon);
        }
    }
}
