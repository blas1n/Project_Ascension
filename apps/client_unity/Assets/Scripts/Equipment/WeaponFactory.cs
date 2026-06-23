using UnityEngine;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// Builds a weapon instance from WeaponData. The concrete WeaponBase type is
    /// chosen from the shared EquipmentType. Visuals are placeholder primitives
    /// (no art yet); replace with prefabs when models exist.
    /// </summary>
    public static class WeaponFactory
    {
        public static WeaponBase Create(WeaponData data)
        {
            var go = new GameObject(data.DisplayName);
            WeaponBase weapon = data.EquipmentType switch
            {
                EquipmentType.Bow => go.AddComponent<BowWeapon>(),
                EquipmentType.Firearm => go.AddComponent<PistolWeapon>(),
                EquipmentType.Catalyst => go.AddComponent<CatalystWeapon>(),
                _ => go.AddComponent<SwordWeapon>(), // Weapon / default = melee
            };
            weapon.Configure(data);
            AddPlaceholderVisual(go, data.EquipmentType);
            return weapon;
        }

        private static void AddPlaceholderVisual(GameObject parent, EquipmentType type)
        {
            var (primitive, scale, localPos) = type switch
            {
                EquipmentType.Bow => (PrimitiveType.Capsule, new Vector3(0.06f, 0.4f, 0.06f), new Vector3(0f, 0f, 0.3f)),
                EquipmentType.Firearm => (PrimitiveType.Cube, new Vector3(0.08f, 0.12f, 0.25f), new Vector3(0f, 0f, 0.15f)),
                EquipmentType.Catalyst => (PrimitiveType.Sphere, new Vector3(0.15f, 0.15f, 0.15f), new Vector3(0f, 0f, 0.2f)),
                _ => (PrimitiveType.Cube, new Vector3(0.05f, 0.05f, 0.7f), new Vector3(0f, 0f, 0.4f)), // sword blade
            };

            var view = GameObject.CreatePrimitive(primitive);
            view.name = "View";
            Object.Destroy(view.GetComponent<Collider>());
            view.transform.SetParent(parent.transform, worldPositionStays: false);
            view.transform.localScale = scale;
            view.transform.localPosition = localPos;
        }
    }
}
