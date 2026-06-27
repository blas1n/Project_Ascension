using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectAscension.Net
{
    // Read-only server catalog: the combat tuning and authored weapon definitions the
    // client runs the slice with. DTOs mirror ProjectAscension.Contracts (camelCase +
    // string enums). In the MMO the server runs combat with these directly; in the slice
    // the client fetches them so DB balance edits reach client-run resolvers/weapons.

    [Serializable]
    public sealed class CombatTuningDto
    {
        public float projectileDamage;
        public float beamDamage;
        public float areaDamage;
        public float dotDamagePerTick;
        public float spreadFalloff;
        public int baseDotTicks;
        public float shieldPerMagnitude;
        public float dashPerMagnitude;
        public float leechFractionPerMagnitude;
        public float controlDurationPerMagnitude;
        public float passiveShieldReduction;
        public float passiveBarrierReduction;
        public float passiveLeech;
        public float focusCostPerPoint;
    }

    [Serializable]
    public sealed class WeaponDefinitionDto
    {
        public string key;
        public string displayName;
        public string equipmentType; // EquipmentType name
        public string slotType;      // SlotType name
        public float damage;
        public float range;
        public float projectileSpeed;
        public float projectileGravity;
        public float cooldown;
        public float chargeTime;
        public float maxChargeMultiplier;
        public float spreadMin;
        public float spreadMax;
        public float spreadPerShot;
        public float spreadRecovery;
    }

    [Serializable]
    public sealed class WeaponDefinitionListDto
    {
        public WeaponDefinitionDto[] items;
    }

    /// <summary>Thin UnityWebRequest client for the read-only server catalog. Coroutine-
    /// based so a MonoBehaviour can drive it. On any failure it simply doesn't invoke the
    /// callback, so callers keep their offline defaults.</summary>
    public sealed class CatalogApiClient
    {
        private readonly string _baseUrl;

        public CatalogApiClient(string baseUrl) => _baseUrl = baseUrl?.TrimEnd('/');

        public IEnumerator GetCombatTuning(Action<CombatTuningDto> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/combat/tuning",
                json => onResult?.Invoke(JsonUtility.FromJson<CombatTuningDto>(json)));
        }

        public IEnumerator GetWeapons(Action<WeaponDefinitionDto[]> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/weapons",
                // JsonUtility can't parse a top-level array — wrap it in an object first.
                json => onResult?.Invoke(JsonUtility.FromJson<WeaponDefinitionListDto>("{\"items\":" + json + "}").items));
        }

        private static IEnumerator GetJson(string url, Action<string> onOk)
        {
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onOk?.Invoke(req.downloadHandler.text);
            else
                Debug.LogWarning($"[Catalog] GET {url} failed: {req.error}");
        }
    }
}
