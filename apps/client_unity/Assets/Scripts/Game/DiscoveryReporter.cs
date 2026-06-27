using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Equipment;
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
        [SerializeField] private string actorId = "11111111-1111-1111-1111-111111111111";
        [SerializeField] private string regionId = "22222222-2222-2222-2222-222222222222";
        [SerializeField] private float flushInterval = 5f;

        private const float MonsterContextWindow = 10f; // a recent kill flavors discovery for this long

        private readonly BehaviorAccumulator _accumulator = new();
        private readonly Dictionary<string, float> _recentMonsters = new(); // tag -> expiry time
        private DiscoveryApiClient _api;
        private Loadout _loadout;
        private int _persistence;

        /// <summary>Raised with the discovery id when the server fires a discovery.</summary>
        public event Action<string> Fired;

        private void Start()
        {
            _loadout = FindAnyObjectByType<Loadout>();
            if (string.IsNullOrWhiteSpace(serverUrl)) return; // offline: BehaviorTracker still drives the local catalog
            _api = new DiscoveryApiClient(serverUrl);
            StartCoroutine(FlushLoop());
        }

        private void OnEnable()
        {
            GameplayEvents.Jumped += OnJumped;
            GameplayEvents.Dodged += OnDodged;
            GameplayEvents.Attacked += OnAttacked;
            GameplayEvents.ChargedAttacked += OnChargedAttacked;
            GameplayEvents.MonsterKilled += OnMonsterKilled;
        }

        private void OnDisable()
        {
            GameplayEvents.Jumped -= OnJumped;
            GameplayEvents.Dodged -= OnDodged;
            GameplayEvents.Attacked -= OnAttacked;
            GameplayEvents.ChargedAttacked -= OnChargedAttacked;
            GameplayEvents.MonsterKilled -= OnMonsterKilled;
        }

        private void OnJumped() => _accumulator.Record(BehaviorKind.Jump);
        private void OnDodged() => _accumulator.Record(BehaviorKind.Dodge);
        private void OnAttacked(bool isMelee) =>
            _accumulator.Record(isMelee ? BehaviorKind.MeleeAttack : BehaviorKind.RangedAttack);
        private void OnChargedAttacked() => _accumulator.Record(BehaviorKind.ChargedAttack);

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

            _persistence++;
            var request = BuildRequest();
            _accumulator.Reset();
            yield return _api.Evaluate(request, OnEvaluated);
        }

        private void OnEvaluated(EvaluateResponseDto response)
        {
            if (response != null && response.fired && !string.IsNullOrEmpty(response.discoveryId))
                Fired?.Invoke(response.discoveryId);
        }

        private EvaluateRequestDto BuildRequest()
        {
            var behaviors = new List<BehaviorCountDto>();
            foreach (var kv in _accumulator.Counts)
                behaviors.Add(new BehaviorCountDto { behavior = kv.Key, count = kv.Value });

            var tags = new List<string>(_accumulator.Tags);
            return new EvaluateRequestDto
            {
                actorId = actorId,
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
