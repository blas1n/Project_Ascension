using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Contracts;
using ProjectAscension.Net;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Cross-scene game state. Created in the Bootstrap scene and kept alive
    /// (DontDestroyOnLoad) so contract progress, currency, and the chosen loadout
    /// survive City&lt;-&gt;Frontier transitions. Accessed via <see cref="Instance"/>.
    /// (A pragmatic singleton for the slice; can move under VContainer later.)
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private WeaponData[] ownedWeapons;
        [SerializeField] private string serverUrl = ""; // empty → offline (defaults/SO assets)
        [SerializeField] private string actorId = "11111111-1111-1111-1111-111111111111"; // for restoring discoveries

        private const string RegionId = "22222222-2222-2222-2222-222222222222"; // the slice's frontier

        public static GameSession Instance { get; private set; }

        /// <summary>The API base URL (empty = offline) — lets UI (e.g. contract issuing)
        /// make its own requests.</summary>
        public string ServerUrl => serverUrl;

        public ContractService Contracts { get; private set; }
        public PlayerStateService PlayerState { get; private set; }

        /// <summary>Skills the player has discovered, split into weapons (synthesized
        /// magic) and commands (techniques). Populated as discoveries are fetched.</summary>
        public DiscoveredSkillSet DiscoveredSkills { get; private set; }

        /// <summary>The player's ability bar — discovered Commands bound to the AbilitySlots
        /// hotkeys (Q/E/R/F). Session-persistent (like the weapon loadout); defaults to the
        /// first discovered commands until the player customises it in the city.</summary>
        public DiscoveredSkill[] CommandSlots { get; } = new DiscoveredSkill[AbilitySlots.SlotCount];

        private bool _slotDefaultsApplied;

        /// <summary>Bind a discovered command (or null) to a hotkey slot.</summary>
        public void AssignCommandSlot(int index, DiscoveredSkill command)
        {
            if (index < 0 || index >= CommandSlots.Length) return;
            CommandSlots[index] = command;
            _slotDefaultsApplied = true; // player took control — stop auto-defaulting
        }

        /// <summary>The slot a command is bound to, or -1 if unassigned.</summary>
        public int SlotOf(DiscoveredSkill command)
        {
            if (command == null) return -1;
            for (int i = 0; i < CommandSlots.Length; i++)
                if (ReferenceEquals(CommandSlots[i], command)) return i;
            return -1;
        }

        /// <summary>Seed empty slots with the first discovered commands, once, when the player
        /// hasn't customised the bar (retried until commands finish loading).</summary>
        public void EnsureDefaultCommandSlots()
        {
            if (_slotDefaultsApplied) return;
            var commands = DiscoveredSkills?.Commands;
            if (commands == null || commands.Count == 0) return;
            GameSimulation.Combat.CommandSlotDefaults.Seed(CommandSlots, commands);
            _slotDefaultsApplied = true;
        }

        // The combat balance the resolvers use is DB-driven, fetched once at startup into
        // the shared CombatTuningCatalog (so the Player layer can read it too); offline
        // keeps CombatTuning.Default.

        // Authored weapon definitions by DisplayName (DB-driven stats), fetched at start.
        private readonly Dictionary<string, WeaponDefinitionDto> _weaponDefs = new();

        // The city shop catalog (DB-driven buy/sell prices), fetched at start.
        public List<ItemDefinitionDto> ShopItems { get; } = new();

        /// <summary>The frontier outpost's development (server-persistent), fetched at start
        /// and refreshed after each resource delivery. Null until/unless fetched.</summary>
        public SettlementDto Settlement { get; private set; }
        public void SetSettlement(SettlementDto s) { if (s != null) Settlement = s; }

        /// <summary>The city's NPC roster (shop / guard / contract clerk), fetched at start.</summary>
        public List<NpcDto> Npcs { get; } = new();

        /// <summary>The DB-driven definition for an authored weapon, by display name —
        /// null when offline or unknown (caller falls back to the authored asset).</summary>
        public WeaponDefinitionDto WeaponDefinition(string displayName)
            => displayName != null && _weaponDefs.TryGetValue(displayName, out var d) ? d : null;

        /// <summary>Set when the player dies — the city surfaces a delegation hint
        /// (the tutorial's teachable moment). Cleared once acted on.</summary>
        public bool SuggestDelegation { get; set; }

        private CatalogApiClient _api; // reused for fetch + save (null offline)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Contracts = new ContractService();
            PlayerState = new PlayerStateService(ownedWeapons ?? new WeaponData[0]);
            DiscoveredSkills = new DiscoveredSkillSet();

            Combat.GameplayEvents.PlayerDied += OnPlayerDied;
            Combat.GameplayEvents.MonsterKilled += OnMonsterKilled;

            if (!string.IsNullOrWhiteSpace(serverUrl))
            {
                _api = new CatalogApiClient(serverUrl);
                StartCoroutine(FetchCatalog(_api));
                // Restore previously-discovered skills at SESSION START (not on frontier entry)
                // so the city shows them and their weapons are in inventory immediately. A
                // discovery's claim persists server-side, so re-playing returns fired=false and
                // the reporter never re-loads it — without this they'd be lost every restart.
                StartCoroutine(RestoreDiscoveredSkills(new DiscoveryApiClient(serverUrl)));
            }
        }

        private IEnumerator RestoreDiscoveredSkills(DiscoveryApiClient discoveryApi)
        {
            DiscoveryListItemDto[] list = null;
            yield return discoveryApi.GetByActor(actorId, r => list = r);
            if (list == null) yield break;
            foreach (var item in list)
            {
                if (string.IsNullOrEmpty(item.id)) continue;
                SkillResponseDto dto = null;
                yield return discoveryApi.GetSkill(item.id, r => dto = r);
                if (dto == null) continue;

                // Acceptance lives in SkillRestore (headless, contract-tested): a Ready skill is
                // built regardless of primitives (graph-only skills have none, ADR 0007 Phase 4c);
                // Build returns null when it isn't Ready.
                var discovered = DiscoveredSkillFactory.Build(dto, out var weapon);
                if (discovered == null) continue;
                DiscoveredSkills.Add(discovered);
                if (weapon != null) PlayerState.AddWeapon(weapon); // discovered weapon back in inventory
            }

            // Push the restored skills' movement capability (double jump, wall-climb) so it's
            // active from session start — not only after a frontier PassiveModifiers runs.
            // Graph-driven (ADR 0007). GameSession persists, so this survives the City<->Frontier loop.
            var movement = DiscoveredSkills.AggregateMovement();
            GameSimulation.Player.MovementCapabilityCatalog.Set(movement);
            Debug.Log($"[Restore] {DiscoveredSkills.Weapons.Count}W/{DiscoveredSkills.Commands.Count}C/{DiscoveredSkills.Passives.Count}P restored; extraJumps={movement.ExtraJumps} wallClimb={movement.WallClimb}");
        }

        /// <summary>Persist the player's progress (currency, standing, materials, sold
        /// knowledge). No-op offline. Called when leaving the city / on demand.</summary>
        public void Save()
        {
            if (_api == null) return;
            var dto = new PlayerStateDto
            {
                currency = PlayerState.Currency,
                reputation = PlayerState.Reputation,
                resources = BuildResourceArray(),
                soldKnowledge = new List<string>(PlayerState.SoldKnowledge).ToArray(),
            };
            StartCoroutine(_api.SavePlayerState(dto, _ => { }));
        }

        private ResourceCountDto[] BuildResourceArray()
        {
            var list = new List<ResourceCountDto>();
            foreach (var kv in PlayerState.Resources)
                if (kv.Value > 0) list.Add(new ResourceCountDto { key = kv.Key, count = kv.Value });
            return list.ToArray();
        }

        private void ApplyPlayerState(PlayerStateDto dto)
        {
            if (dto == null) return;
            PlayerState.Currency = dto.currency;
            PlayerState.Reputation = dto.reputation;
            PlayerState.Resources.Clear();
            if (dto.resources != null)
                foreach (var r in dto.resources) PlayerState.AddResource(r.key, r.count);
            PlayerState.SoldKnowledge.Clear();
            if (dto.soldKnowledge != null)
                foreach (var k in dto.soldKnowledge) PlayerState.SoldKnowledge.Add(k);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Combat.GameplayEvents.PlayerDied -= OnPlayerDied;
            Combat.GameplayEvents.MonsterKilled -= OnMonsterKilled;
        }

        // Collect a defeated monster's resource drop into the player's inventory.
        private void OnMonsterKilled(GameObject monster)
        {
            if (monster != null && monster.TryGetComponent<Combat.IMonsterInfo>(out var info))
                PlayerState.AddResource(info.DropItemKey, info.DropAmount);
        }

        private void OnPlayerDied()
        {
            // A contract that fails on death does so now; otherwise death is just the
            // delegation tutorial's teachable moment.
            var failed = Contracts?.FailActiveOnDeath();
            if (failed != null) ApplyFailure(failed, "died");
            else SuggestDelegation = true;
        }

        // Advance the contractor (delegated contracts) and the active contract's deadline
        // each frame, across scenes. Letting a deadline lapse fails the contract and costs
        // standing — accepting one is a real commitment.
        private void Update()
        {
            if (Contracts == null) return;
            float dt = Time.deltaTime;
            Contracts.TickDelegations(dt);
            var failed = Contracts.TickActive(dt);
            if (failed != null) ApplyFailure(failed, "expired");
        }

        private void ApplyFailure(ContractInstance contract, string reason)
        {
            int penalty = Mathf.Min(PlayerState.Reputation, contract.RewardReputation);
            PlayerState.Reputation -= penalty;
            Contracts.FailedRecently.Add($"{contract.Title} ({reason}, -{penalty} rep)");
        }

        // Pull the DB-driven balance once at startup. Any failure leaves the defaults in
        // place, so the slice stays playable offline.
        private IEnumerator FetchCatalog(CatalogApiClient api)
        {
            yield return api.GetCombatTuning(dto =>
            {
                if (dto != null) CombatTuningCatalog.Set(ToCombatTuning(dto));
            });
            yield return api.GetWeapons(defs =>
            {
                if (defs == null) return;
                foreach (var d in defs)
                    if (!string.IsNullOrEmpty(d.displayName)) _weaponDefs[d.displayName] = d;
            });
            yield return api.GetMonsters(defs =>
            {
                if (defs == null) return;
                foreach (var d in defs)
                    Combat.MonsterStatsCatalog.Set(d.key, new Combat.MonsterStats(
                        d.maxHealth, d.moveSpeed, d.aggroRange, d.attackRange,
                        d.attackCooldown, d.damage, d.projectileSpeed, d.scale, d.dropItemKey, d.dropAmount));
            });
            yield return api.GetPlayer(d =>
            {
                if (d != null)
                    GameSimulation.Player.PlayerStatsCatalog.Set(new GameSimulation.Player.PlayerStats(
                        d.maxHealth, d.moveSpeed, d.jumpVelocity, d.gravity,
                        d.dodgeSpeed, d.dodgeDuration, d.maxFocus, d.focusRegenPerSecond));
            });
            yield return api.GetShop(items =>
            {
                if (items == null) return;
                ShopItems.Clear();
                ShopItems.AddRange(items);
            });
            yield return api.GetSettlement(s => { if (s != null) Settlement = s; });
            yield return api.GetNpcs(npcs =>
            {
                if (npcs == null) return;
                Npcs.Clear();
                Npcs.AddRange(npcs);
            });
            yield return api.GetPlayerState(ApplyPlayerState); // load saved progress
            yield return api.GetContracts(RegionId, defs =>
            {
                if (defs == null) return;
                var board = new List<ContractInstance>(defs.Length);
                foreach (var d in defs)
                    board.Add(GameSimulation.Contracts.ContractMapping.FromFields(
                        d.purpose, d.title, d.description, d.targetCount, d.rewardCurrency, d.target,
                        d.issuer, d.delegationAllowed, d.rewardReputation, d.minReputation,
                        d.timeLimitSeconds, d.failOnTimeout, d.failOnDeath));
                Contracts.SetAvailable(board);
            });
        }

        private static CombatTuning ToCombatTuning(CombatTuningDto d) => new(
            d.projectileDamage, d.beamDamage, d.areaDamage, d.dotDamagePerTick, d.spreadFalloff,
            d.baseDotTicks, d.shieldPerMagnitude, d.dashPerMagnitude, d.leechFractionPerMagnitude,
            d.controlDurationPerMagnitude, d.passiveShieldReduction, d.passiveBarrierReduction,
            d.passiveLeech, d.focusCostPerPoint,
            d.slowPerMagnitude, d.knockbackPerMagnitude, d.chargedAttackThreshold,
            d.deliveryProjectileSpeed, d.deliveryProjectileGravity, d.deliveryRange,
            d.deliveryAreaRadius, d.deliveryHitscanRadius);
    }
}
