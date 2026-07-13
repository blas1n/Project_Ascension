using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Discovery;
using ProjectAscension.Net;

namespace ProjectAscension.Game
{
    /// <summary>
    /// SLICE SCAFFOLDING (ADR 0004). Reports the player's behavior signature to the
    /// server's discovery trigger (POST /api/discoveries/evaluate) by accumulating
    /// gameplay facts client-side and flushing on an interval. This exists only because
    /// the slice has no authoritative server simulation — in the MMO the server runs
    /// GameSimulation and observes behavior directly, so this reporter (and the client
    /// REST path) is DELETED and the trigger subscribes to server-sim events instead.
    /// Client-asserted behavior is not authoritative; the slice trades that for a
    /// playable loop. Disabled when no <see cref="serverUrl"/> is set.
    /// </summary>
    public sealed class DiscoveryReporter : MonoBehaviour
    {
        [SerializeField] private string serverUrl = ""; // empty → disabled (offline)
        // The slice's single frontier region — WORLD data (seeded server-side), same for every
        // player, so it stays a fixed default here unlike the actor id below.
        [SerializeField] private string regionId = "22222222-2222-2222-2222-222222222222";
        [SerializeField] private float flushInterval = 5f;

        // NOT serialized, NOT defaulted: the actor id is PLAYER identity, minted once by character
        // creation and owned by GameSession (GameSession.ActorId) — a second hardcoded copy here is
        // exactly how a fresh player's evaluate call used to 500 (it posted an actor id nothing ever
        // created). Read live so a reporter that starts before creation finishes still catches up.
        private static string ActorId => GameSession.Instance != null ? GameSession.Instance.ActorId : "";

        private const float MonsterContextWindow = 10f; // a recent kill flavors discovery for this long

        private readonly BehaviorAccumulator _accumulator = new();
        // The COMPOSITE behaviours (air-attack / repeated jump / weapon fusion) are derived by a rule,
        // not by this glue — what counts as a composite is one tested answer (CompositionDeriver).
        // ONE grammar (ADR 0009) instead of four bespoke observers. It knows nothing about catalysts
        // or jumps — it composes whatever acts arrive, so a new act needs no new observer.
        private readonly CompositionDeriver _grammar = new();
        private readonly List<string> _composites = new();
        private readonly Dictionary<string, float> _recentMonsters = new(); // tag -> expiry time
        private DiscoveryApiClient _api;
        private Loadout _loadout;
        private int _persistence;

        /// <summary>Raised with the discovery id when the server fires a discovery.</summary>
        public event Action<string> Fired;

        private void Start()
        {
            _loadout = FindAnyObjectByType<Loadout>();
            if (string.IsNullOrWhiteSpace(serverUrl)) return; // offline: no discovery (server is authoritative)
            _api = new DiscoveryApiClient(serverUrl);
            StartCoroutine(FlushLoop());
        }

        private void OnEnable()
        {
            GameplayEvents.Jumped += OnJumped;
            GameplayEvents.Attacked += OnAttacked;
            GameplayEvents.ActPerformed += OnActPerformed;
            GameplayEvents.MonsterKilled += OnMonsterKilled;
        }

        private void OnDisable()
        {
            GameplayEvents.Jumped -= OnJumped;
            GameplayEvents.Attacked -= OnAttacked;
            GameplayEvents.ActPerformed -= OnActPerformed;
            GameplayEvents.MonsterKilled -= OnMonsterKilled;
        }

        // The RAW counts stay raw: what, and how many times. Everything compositional — fusions,
        // air attacks, chains — is the grammar's business now (ADR 0009).
        private void OnJumped() => _accumulator.Record(BehaviorKind.Jump);
        private void OnAttacked(bool isMelee) =>
            _accumulator.Record(isMelee ? BehaviorKind.MeleeAttack : BehaviorKind.RangedAttack);

        private void OnActPerformed(Act act)
        {
            // Provenance (ADR 0011): WHICH instrument made this act. Scores nothing — it is evidence,
            // not achievement — but it is how a skill knows which weapons actually took part in it, so
            // that carrying a catalyst you never used cannot lay claim to the gun's discovery.
            if (act.IsValid) _accumulator.Record(SkillBinding.UsePrefix + act.Token);

            _composites.Clear();
            _grammar.Observe(act, _composites);
            foreach (var composite in _composites) _accumulator.Record(composite);
        }

        // A defeated monster flavors the discovery context for a window (몬스터는 발견의 촉매).
        private void OnMonsterKilled(GameObject monster)
        {
            if (monster != null && monster.TryGetComponent<IMonsterInfo>(out var info) && !string.IsNullOrEmpty(info.DiscoveryTag))
                _recentMonsters[info.DiscoveryTag] = Time.time + MonsterContextWindow;
        }

        private IEnumerator FlushLoop()
        {
            var wait = new WaitForSeconds(flushInterval);
            while (true)
            {
                yield return wait;
                yield return Flush();
            }
        }

        private IEnumerator Flush()
        {
            _accumulator.SetContext(BuildContextTags());
            if (!_accumulator.HasActivity)
            {
                _persistence = 0; // sustained activity drives the persistence axis
                yield break;
            }

            // No identity yet (character creation hasn't returned) — nothing to post as. Keep the
            // accumulated behavior for the next window instead of dropping it or posting a bogus id.
            if (string.IsNullOrWhiteSpace(ActorId)) yield break;

            _persistence++;
            var request = BuildRequest();
            _accumulator.Reset();
            yield return _api.Evaluate(request, OnEvaluated);
        }

        private void OnEvaluated(EvaluateResponseDto response)
        {
            if (response != null && response.fired && !string.IsNullOrEmpty(response.discoveryId))
            {
                Fired?.Invoke(response.discoveryId);
                // A discovery just fired; drop the persistence so the score falls back below
                // threshold — the next discovery must be built up again, not re-fired every
                // window off the same sustained play (which produced duplicate discoveries).
                _persistence = 0;
            }
        }

        private EvaluateRequestDto BuildRequest()
        {
            var behaviors = new List<BehaviorCountDto>();
            foreach (var kv in _accumulator.Counts)
                behaviors.Add(new BehaviorCountDto { behavior = kv.Key, count = kv.Value });

            var tags = new List<string>(_accumulator.Tags);
            return new EvaluateRequestDto
            {
                actorId = ActorId,
                regionId = regionId,
                type = "Skill",
                theme = "an expedition discovery",
                contextTags = tags.ToArray(),
                primaryBehavior = PrimaryBehavior(),
                behaviors = behaviors.ToArray(),
                persistence = _persistence,
            };
        }

        private IEnumerable<string> BuildContextTags()
        {
            // Equipment (shared EquipmentTags — includes a discovered weapon's own tag,
            // so equipping it opens further discoveries) + recent monster encounters.
            var tags = new List<string>(EquipmentTags.CurrentTags(_loadout));
            AddRecentMonsters(tags);
            return tags;
        }

        private void AddRecentMonsters(ICollection<string> tags)
        {
            float now = Time.time;
            var expired = new List<string>();
            foreach (var kv in _recentMonsters)
            {
                if (kv.Value > now) tags.Add(kv.Key);
                else expired.Add(kv.Key);
            }
            foreach (var tag in expired) _recentMonsters.Remove(tag);
        }

        // The composition seed for the skill's primary effect — a PrimitiveKind name.
        private string PrimaryBehavior()
        {
            var data = _loadout?.RightSlot?.Current?.Data ?? _loadout?.LeftSlot?.Current?.Data;
            if (data == null) return "Projectile";
            return data.EquipmentType switch
            {
                EquipmentType.Weapon => "Dash",
                EquipmentType.Catalyst => "Beam",
                _ => "Projectile",
            };
        }
    }
}
