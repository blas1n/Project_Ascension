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
        public float slowPerMagnitude;
        public float knockbackPerMagnitude;
        public float chargedAttackThreshold;
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

    [Serializable]
    public sealed class MonsterDefinitionDto
    {
        public string key;
        public float maxHealth;
        public float moveSpeed;
        public float aggroRange;
        public float attackRange;
        public float attackCooldown;
        public float damage;
        public float projectileSpeed;
        public float scale;
    }

    [Serializable]
    public sealed class MonsterDefinitionListDto
    {
        public MonsterDefinitionDto[] items;
    }

    [Serializable]
    public sealed class ContractDto
    {
        public string title;
        public string description;
        public string purpose; // ContractPurpose name
        public int targetCount;
        public int rewardCurrency;
    }

    [Serializable]
    public sealed class ContractListDto
    {
        public ContractDto[] items;
    }

    [Serializable]
    public sealed class PlayerDefinitionDto
    {
        public float maxHealth;
        public float moveSpeed;
        public float jumpVelocity;
        public float gravity;
        public float dodgeSpeed;
        public float dodgeDuration;
        public float maxFocus;
        public float focusRegenPerSecond;
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

        public IEnumerator GetMonsters(Action<MonsterDefinitionDto[]> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/monsters",
                json => onResult?.Invoke(JsonUtility.FromJson<MonsterDefinitionListDto>("{\"items\":" + json + "}").items));
        }

        public IEnumerator GetPlayer(Action<PlayerDefinitionDto> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/player",
                json => onResult?.Invoke(JsonUtility.FromJson<PlayerDefinitionDto>(json)));
        }

        public IEnumerator GetContracts(string regionId, Action<ContractDto[]> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/contracts?regionId={regionId}",
                json => onResult?.Invoke(JsonUtility.FromJson<ContractListDto>("{\"items\":" + json + "}").items));
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
