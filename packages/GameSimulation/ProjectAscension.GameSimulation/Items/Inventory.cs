using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Items
{
    /// <summary>
    /// The items the player OWNS — as opposed to raw resources, which are just material counts the shop
    /// and settlement consume. The distinction is the point: the first hour hands the player a map, and
    /// "지도는 UI가 아니라 아이템이다" (docs/03-gameplay/first-hour-experience.md) — you hold it, you can
    /// lose it, and one day you can trade it. Modelling possession explicitly is what makes losing it
    /// mean something later.
    ///
    /// Pure and headless-testable (ADR: Unity is a shell) — the client holds one of these and renders it.
    /// </summary>
    public sealed class Inventory
    {
        private readonly Dictionary<string, int> _owned = new();

        /// <summary>Every owned item key and its count.</summary>
        public IReadOnlyDictionary<string, int> Owned => _owned;

        public int Count(string key)
            => !string.IsNullOrEmpty(key) && _owned.TryGetValue(key, out var n) ? n : 0;

        public bool Has(string key) => Count(key) > 0;

        /// <summary>Take possession. A non-positive amount or a blank key is ignored, so a contract
        /// with no item reward simply grants nothing.</summary>
        public void Add(string key, int amount = 1)
        {
            if (string.IsNullOrEmpty(key) || amount <= 0) return;
            _owned[key] = Count(key) + amount;
        }

        /// <summary>Give up (or lose) an item. Returns how many were actually removed — you cannot lose
        /// what you never had.</summary>
        public int Remove(string key, int amount = 1)
        {
            if (string.IsNullOrEmpty(key) || amount <= 0) return 0;
            int have = Count(key);
            if (have == 0) return 0;

            int taken = amount < have ? amount : have;
            int left = have - taken;
            if (left > 0) _owned[key] = left;
            else _owned.Remove(key);
            return taken;
        }

        public void Clear() => _owned.Clear();
    }
}
