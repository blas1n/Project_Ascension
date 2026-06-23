using UnityEngine;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// A pre-chosen loadout: which weapon sits in the left and right slot. In the
    /// full game this is selected ahead of time from the inventory (tutorial /
    /// City equipment management). For the slice it is authored as an asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Ascension/Loadout Config", fileName = "LoadoutConfig")]
    public sealed class LoadoutConfig : ScriptableObject
    {
        [SerializeField] private WeaponData left;
        [SerializeField] private WeaponData right;

        public WeaponData Left => left;
        public WeaponData Right => right;
    }
}
