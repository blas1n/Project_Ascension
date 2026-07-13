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
        public float movingDistanceThreshold;
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
        public int magazineSize;   // 0 = no magazine, never reloads
        public float reloadTime;
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
        public string id; // server contract row id — needed to accept/turn-in/delegate THIS contract later
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
    public sealed class IssueContractResponseDto
    {
        public ContractDto contract;
        public PlayerStateDto playerState;
    }

    [Serializable]
    public sealed class AcceptContractDto
    {
        public string actorId;
    }

    [Serializable]
    public sealed class UpdateContractProgressDto
    {
        public string actorId;
        public int progressCount;
    }

    [Serializable]
    public sealed class TurnInContractDto
    {
        public string actorId;
    }

    [Serializable]
    public sealed class ContractTurnInResponseDto
    {
        public ContractDto contract;
        public PlayerStateDto playerState;
    }

    [Serializable]
    public sealed class DelegateContractDto
    {
        public string actorId;
    }

    [Serializable]
    public sealed class FailContractDto
    {
        public string actorId;
        public string reason;
    }

    [Serializable]
    public sealed class BuyItemDto
    {
        public string itemKey;
        public int quantity;
    }

    [Serializable]
    public sealed class SellItemDto
    {
        public string itemKey;
        public int quantity;
    }

    [Serializable]
    public sealed class LicenseKnowledgeDto
    {
        public string actorId;
        public string discoveryId;
    }

    /// <summary>Mirrors ProjectAscension.Shared.Error — every rejected economy transaction
    /// (ADR 0014) returns one of these as the response body, so the UI can show the SERVER's
    /// reason instead of guessing one.</summary>
    [Serializable]
    public sealed class ApiErrorDto
    {
        public string code;
        public string message;
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
    public sealed class CreateCharacterDto
    {
        public string name;
    }

    /// <summary>Mirrors ProjectAscension.Contracts.Responses.CharacterResponse — the identity a
    /// fresh character creation (or a lookup) returns. <see cref="actorId"/> is what every other
    /// endpoint keys on (ADR 0014); the client never invents one.</summary>
    [Serializable]
    public sealed class CharacterDto
    {
        public string id;
        public string actorId;
        public string name;
        public string currentRegionId;
        public string status;
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

        /// <summary>Reads the server's own reason out of a rejected transaction's response body
        /// (ADR 0014) — never a client-invented guess. Falls back to a generic line only when
        /// the body isn't the expected Error JSON (e.g. no connection at all).</summary>
        public static string ParseErrorMessage(string responseBody)
        {
            if (string.IsNullOrEmpty(responseBody)) return "Request failed — check the connection.";
            try
            {
                var err = JsonUtility.FromJson<ApiErrorDto>(responseBody);
                return !string.IsNullOrEmpty(err?.message) ? err.message : "Request failed.";
            }
            catch (ArgumentException)
            {
                return "Request failed — check the connection.";
            }
        }

        /// <summary>Names a new character — the server mints the Character + its Actor atomically
        /// and returns it (ADR 0014). The only place a client identity is minted; the caller must
        /// take the returned actorId as its own from now on, never invent one.</summary>
        public IEnumerator CreateCharacter(string name, Action<CharacterDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/characters",
                JsonUtility.ToJson(new CreateCharacterDto { name = name }),
                json => onResult?.Invoke(JsonUtility.FromJson<CharacterDto>(json)),
                onError);
        }

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

        public IEnumerator IssueContract(IssueContractDto request, Action<IssueContractResponseDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/contracts",
                JsonUtility.ToJson(request),
                json => onResult?.Invoke(JsonUtility.FromJson<IssueContractResponseDto>(json)),
                onError);
        }

        /// <summary>Accept an OPEN server contract — assigns it to the actor so a later
        /// turn-in/delegate can be validated server-side (ADR 0014).</summary>
        public IEnumerator AcceptContract(string contractId, string actorId, Action<ContractDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/contracts/{contractId}/accept",
                JsonUtility.ToJson(new AcceptContractDto { actorId = actorId }),
                json => onResult?.Invoke(JsonUtility.FromJson<ContractDto>(json)),
                onError);
        }

        /// <summary>Report the assignee's tracked progress — the server stores this and
        /// TurnInContract checks it before paying out (kill/objective credit is still
        /// client-reported; the PAYOUT itself is server-computed, ADR 0014).</summary>
        public IEnumerator UpdateContractProgress(string contractId, string actorId, int progressCount, Action<ContractDto> onResult)
        {
            yield return PostJson(
                $"{_baseUrl}/api/contracts/{contractId}/progress",
                JsonUtility.ToJson(new UpdateContractProgressDto { actorId = actorId, progressCount = progressCount }),
                json => onResult?.Invoke(JsonUtility.FromJson<ContractDto>(json)));
        }

        /// <summary>Hand in a completed contract — the server pays the reward from its own
        /// stored terms and returns the resulting authoritative player state.</summary>
        public IEnumerator TurnInContract(string contractId, string actorId, Action<ContractTurnInResponseDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/contracts/{contractId}/turn-in",
                JsonUtility.ToJson(new TurnInContractDto { actorId = actorId }),
                json => onResult?.Invoke(JsonUtility.FromJson<ContractTurnInResponseDto>(json)),
                onError);
        }

        /// <summary>Hand the active contract to a stub contractor — the server escrows the
        /// reward as the contractor's fee and returns the resulting player state.</summary>
        public IEnumerator DelegateContract(string contractId, string actorId, Action<PlayerStateDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/contracts/{contractId}/delegate",
                JsonUtility.ToJson(new DelegateContractDto { actorId = actorId }),
                json => onResult?.Invoke(JsonUtility.FromJson<PlayerStateDto>(json)),
                onError);
        }

        /// <summary>Report a contract failure (died / deadline expired) — only the INTENT; the
        /// server reads the contract's own stored reward and computes the reputation penalty
        /// itself, returning the resulting authoritative player state (ADR 0014).</summary>
        public IEnumerator FailContract(string contractId, string actorId, string reason, Action<PlayerStateDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/contracts/{contractId}/fail",
                JsonUtility.ToJson(new FailContractDto { actorId = actorId, reason = reason }),
                json => onResult?.Invoke(JsonUtility.FromJson<PlayerStateDto>(json)),
                onError);
        }

        /// <summary>Buy an item from the shop — the server prices it from its own catalog.</summary>
        public IEnumerator BuyItem(string itemKey, int quantity, Action<PlayerStateDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/shop/buy",
                JsonUtility.ToJson(new BuyItemDto { itemKey = itemKey, quantity = quantity }),
                json => onResult?.Invoke(JsonUtility.FromJson<PlayerStateDto>(json)),
                onError);
        }

        /// <summary>Sell an item to the shop — the server prices it from its own catalog.</summary>
        public IEnumerator SellItem(string itemKey, int quantity, Action<PlayerStateDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/shop/sell",
                JsonUtility.ToJson(new SellItemDto { itemKey = itemKey, quantity = quantity }),
                json => onResult?.Invoke(JsonUtility.FromJson<PlayerStateDto>(json)),
                onError);
        }

        /// <summary>Sell a license for an owned, composed discovery — once per discovery
        /// (server-enforced). The server derives price/reputation from the skill's own
        /// composed effect graph, never from this request.</summary>
        public IEnumerator LicenseKnowledge(string actorId, string discoveryId, Action<PlayerStateDto> onResult, Action<string> onError = null)
        {
            yield return PostJson(
                $"{_baseUrl}/api/knowledge/license",
                JsonUtility.ToJson(new LicenseKnowledgeDto { actorId = actorId, discoveryId = discoveryId }),
                json => onResult?.Invoke(JsonUtility.FromJson<PlayerStateDto>(json)),
                onError);
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

        // onError receives the raw response body (an economy rejection is a normal JSON Error
        // body, e.g. {"code":"CONFLICT","message":"..."}) — never invented client-side.
        private static IEnumerator PostJson(string url, string body, Action<string> onOk, Action<string> onError = null)
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
            {
                Debug.LogWarning($"[Catalog] POST {url} failed: {req.error}");
                onError?.Invoke(req.downloadHandler.text);
            }
        }
    }
}
