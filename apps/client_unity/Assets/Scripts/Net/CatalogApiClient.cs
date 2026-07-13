using System;
using System.Collections;
using System.Text;
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
        public float deliveryProjectileSpeed;
        public float deliveryProjectileGravity;
        public float deliveryRange;
        public float deliveryAreaRadius;
        public float deliveryHitscanRadius;
        public float blockReduction;
        public float blockFrontArcDot;
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
        public float attackWindup;
        public float damage;
        public float projectileSpeed;
        public float scale;
        public string dropItemKey;
        public int dropAmount;
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
        public string target; // optional objective filter (e.g. monster key "elite")
        public bool delegationAllowed;
        public int rewardReputation;
        public int minReputation;
        public int timeLimitSeconds;
        public bool failOnTimeout;
        public bool failOnDeath;
        public string rewardItemKey; // an item paid on completion (the survey pays a map); "" = none
        public int rewardItemAmount;
        public string issuer;
    }

    [Serializable]
    public sealed class ResourceCountDto
    {
        public string key;
        public int count;
    }

    [Serializable]
    public sealed class PlayerStateDto
    {
        public int currency;
        public int reputation;
        public ResourceCountDto[] resources;
        public string[] soldKnowledge;
    }

    [Serializable]
    public sealed class NpcDto
    {
        public string name;
        public string role;
    }

    [Serializable]
    public sealed class NpcListDto
    {
        public NpcDto[] items;
    }

    [Serializable]
    public sealed class ContractListDto
    {
        public ContractDto[] items;
    }

    [Serializable]
    public sealed class ContractQuoteDto
    {
        public int suggestedReward;
        public int minReward;
        public int maxReward;
    }

    [Serializable]
    public sealed class IssueContractDto
    {
        public string issuerActorId;
        public string purpose; // ContractPurpose name
        public string target;  // optional monster key
        public int targetCount;
        public int desiredReward;
        public int durationHours;
    }

    [Serializable]
    public sealed class ItemDefinitionDto
    {
        public string key;
        public string displayName;
        public string description;
        public int sellPrice;
        public int buyPrice;
    }

    [Serializable]
    public sealed class ItemDefinitionListDto
    {
        public ItemDefinitionDto[] items;
    }

    [Serializable]
    public sealed class SettlementDto
    {
        public string name;
        public string stage;
        public int shelterLevel;
        public int marketLevel;
        public int defenseLevel;
        public int totalLevel;
    }

    [Serializable]
    public sealed class DeliverResourceDto
    {
        public string itemKey;
        public int amount;
    }

    [Serializable]
    public sealed class PlayerDefinitionDto
    {
        public float maxHealth;
        public float moveSpeed;
        public float jumpVelocity;
        public float gravity;
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
                // JsonUtility can't parse a top-level array — wrap it in an object first. It also
                // returns null on a malformed/empty body, so coalesce (never deref .items on null).
                json => onResult?.Invoke(JsonUtility.FromJson<WeaponDefinitionListDto>("{\"items\":" + json + "}")?.items ?? Array.Empty<WeaponDefinitionDto>()));
        }

        public IEnumerator GetMonsters(Action<MonsterDefinitionDto[]> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/monsters",
                json => onResult?.Invoke(JsonUtility.FromJson<MonsterDefinitionListDto>("{\"items\":" + json + "}")?.items ?? Array.Empty<MonsterDefinitionDto>()));
        }

        public IEnumerator GetPlayer(Action<PlayerDefinitionDto> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/player",
                json => onResult?.Invoke(JsonUtility.FromJson<PlayerDefinitionDto>(json)));
        }

        public IEnumerator GetPlayerState(Action<PlayerStateDto> onResult)
        {
            yield return GetJson($"{_baseUrl}/api/player-state", json => onResult?.Invoke(JsonUtility.FromJson<PlayerStateDto>(json)));
        }

        public IEnumerator SavePlayerState(PlayerStateDto state, Action<PlayerStateDto> onResult)
        {
            yield return PutJson(
                $"{_baseUrl}/api/player-state",
                JsonUtility.ToJson(state),
                json => onResult?.Invoke(JsonUtility.FromJson<PlayerStateDto>(json)));
        }

        public IEnumerator GetNpcs(Action<NpcDto[]> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/npcs",
                json => onResult?.Invoke(JsonUtility.FromJson<NpcListDto>("{\"items\":" + json + "}")?.items ?? Array.Empty<NpcDto>()));
        }

        public IEnumerator GetSettlement(Action<SettlementDto> onResult)
        {
            yield return GetJson($"{_baseUrl}/api/settlement", json => onResult?.Invoke(JsonUtility.FromJson<SettlementDto>(json)));
        }

        public IEnumerator DeliverResource(DeliverResourceDto request, Action<SettlementDto> onResult)
        {
            yield return PostJson(
                $"{_baseUrl}/api/settlement/deliver",
                JsonUtility.ToJson(request),
                json => onResult?.Invoke(JsonUtility.FromJson<SettlementDto>(json)));
        }

        public IEnumerator GetShop(Action<ItemDefinitionDto[]> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/shop",
                json => onResult?.Invoke(JsonUtility.FromJson<ItemDefinitionListDto>("{\"items\":" + json + "}")?.items ?? Array.Empty<ItemDefinitionDto>()));
        }

        public IEnumerator GetContracts(string regionId, Action<ContractDto[]> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/contracts?regionId={regionId}",
                json => onResult?.Invoke(JsonUtility.FromJson<ContractListDto>("{\"items\":" + json + "}")?.items ?? Array.Empty<ContractDto>()));
        }

        public IEnumerator GetContractQuote(string purpose, string target, int count, Action<ContractQuoteDto> onResult)
        {
            var url = $"{_baseUrl}/api/contracts/quote?purpose={purpose}&count={count}";
            if (!string.IsNullOrEmpty(target)) url += $"&target={target}";
            yield return GetJson(url, json => onResult?.Invoke(JsonUtility.FromJson<ContractQuoteDto>(json)));
        }

        public IEnumerator IssueContract(IssueContractDto request, Action<ContractDto> onResult)
        {
            yield return PostJson(
                $"{_baseUrl}/api/contracts",
                JsonUtility.ToJson(request),
                json => onResult?.Invoke(JsonUtility.FromJson<ContractDto>(json)));
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

        private static IEnumerator PutJson(string url, string body, Action<string> onOk)
        {
            using var req = new UnityWebRequest(url, "PUT")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onOk?.Invoke(req.downloadHandler.text);
            else
                Debug.LogWarning($"[Catalog] PUT {url} failed: {req.error}");
        }

        private static IEnumerator PostJson(string url, string body, Action<string> onOk)
        {
            using var req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onOk?.Invoke(req.downloadHandler.text);
            else
                Debug.LogWarning($"[Catalog] POST {url} failed: {req.error}");
        }
    }
}
