using UnityEngine;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// Common behaviour for an equippable weapon. Phase 2 handles only equip/
    /// unequip and identity; the action hooks are wired to input and the
    /// simulation in Phase 3 (combat).
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour, IEquippable
    {
        private WeaponData _data;

        public WeaponData Data => _data;

        /// <summary>Assigns the data when the weapon is spawned by the Loadout.</summary>
        public void Configure(WeaponData data) => _data = data;

        public virtual void OnEquip(Transform handAnchor)
        {
            transform.SetParent(handAnchor, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        public virtual void OnUnequip()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Primary use (e.g. swing / fire / cast). No-op until Phase 3.</summary>
        public virtual void PrimaryAction() { }

        /// <summary>Secondary use (e.g. aim / block / charge). No-op until Phase 3.</summary>
        public virtual void SecondaryAction() { }
    }
}
