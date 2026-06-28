using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
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

        public static GameSession Instance { get; private set; }

        public ContractService Contracts { get; private set; }
        public PlayerStateService PlayerState { get; private set; }
        public DiscoveryService Discovery { get; private set; }

        /// <summary>Skills the player has discovered, split into weapons (synthesized
        /// magic) and commands (techniques). Populated as discoveries are fetched.</summary>
        public DiscoveredSkillSet DiscoveredSkills { get; private set; }

        // The combat balance the resolvers use is DB-driven, fetched once at startup into
        // the shared CombatTuningCatalog (so the Player layer can read it too); offline
        // keeps CombatTuning.Default.

        // Authored weapon definitions by DisplayName (DB-driven stats), fetched at start.
        private readonly Dictionary<string, WeaponDefinitionDto> _weaponDefs = new();

        /// <summary>The DB-driven definition for an authored weapon, by display name —
        /// null when offline or unknown (caller falls back to the authored asset).</summary>
        public WeaponDefinitionDto WeaponDefinition(string displayName)
            => displayName != null && _weaponDefs.TryGetValue(displayName, out var d) ? d : null;

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
            Discovery = new DiscoveryService();
            DiscoveredSkills = new DiscoveredSkillSet();

            if (!string.IsNullOrWhiteSpace(serverUrl)) StartCoroutine(FetchCatalog(new CatalogApiClient(serverUrl)));
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
                        d.attackCooldown, d.damage, d.projectileSpeed, d.scale));
            });
            yield return api.GetPlayer(d =>
            {
                if (d != null)
                    GameSimulation.Player.PlayerStatsCatalog.Set(new GameSimulation.Player.PlayerStats(
                        d.maxHealth, d.moveSpeed, d.jumpVelocity, d.gravity,
                        d.dodgeSpeed, d.dodgeDuration, d.maxFocus, d.focusRegenPerSecond));
            });
        }

        private static CombatTuning ToCombatTuning(CombatTuningDto d) => new(
            d.projectileDamage, d.beamDamage, d.areaDamage, d.dotDamagePerTick, d.spreadFalloff,
            d.baseDotTicks, d.shieldPerMagnitude, d.dashPerMagnitude, d.leechFractionPerMagnitude,
            d.controlDurationPerMagnitude, d.passiveShieldReduction, d.passiveBarrierReduction,
            d.passiveLeech, d.focusCostPerPoint,
            d.slowPerMagnitude, d.knockbackPerMagnitude, d.chargedAttackThreshold);
    }
}
