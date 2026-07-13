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
        /// <summary>What the player called themselves (stage 0). Empty until they do.</summary>
        public string CharacterName = "";

        public int Currency;
        public int Reputation; // 명성 — standing earned from contracts; gates higher-tier ones

        /// <summary>Resource materials by key (monster drops), the itemization base for the
        /// shop, contract collection, and settlement supply.</summary>
        public readonly Dictionary<string, int> Resources = new();

        /// <summary>Items the player OWNS — possessions, not materials. The first hour's survey pays in
        /// a map, and "지도는 UI가 아니라 아이템이다": you hold it, you can lose it, you can trade it.
        /// Rules live in GameSimulation (ADR: Unity is a shell).</summary>
        public readonly GameSimulation.Items.Inventory Inventory = new();

        /// <summary>Discovered skills whose knowledge license has been sold (by name) — each
        /// sells once. The discovery itself is kept (first-discoverer is permanent).</summary>
        public readonly HashSet<string> SoldKnowledge = new();

        public void AddResource(string key, int amount)
        {
            if (string.IsNullOrEmpty(key) || amount <= 0) return;
            Resources.TryGetValue(key, out var have);
            Resources[key] = have + amount;
        }

        /// <summary>Spend resources if available; returns false (spending nothing) otherwise.</summary>
        public bool SpendResource(string key, int amount)
        {
            if (string.IsNullOrEmpty(key) || amount <= 0) return false;
            Resources.TryGetValue(key, out var have);
            if (have < amount) return false;
            Resources[key] = have - amount;
            return true;
        }

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
