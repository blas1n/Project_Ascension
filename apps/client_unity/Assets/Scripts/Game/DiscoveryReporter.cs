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

        private readonly BehaviorAccumulator _accumulator = new();
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
        }

        private void OnDisable()
        {
            GameplayEvents.Jumped -= OnJumped;
            GameplayEvents.Dodged -= OnDodged;
            GameplayEvents.Attacked -= OnAttacked;
        }

        private void OnJumped() => _accumulator.Record(BehaviorKind.Jump);
        private void OnDodged() => _accumulator.Record(BehaviorKind.Dodge);
        private void OnAttacked(bool isMelee) =>
            _accumulator.Record(isMelee ? BehaviorKind.MeleeAttack : BehaviorKind.RangedAttack);

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
            var tags = new List<string>();
            if (_loadout == null) return tags;
            AddTags(tags, _loadout.LeftSlot?.Current?.Data);
            AddTags(tags, _loadout.RightSlot?.Current?.Data);
            return tags;
        }

        private static void AddTags(ICollection<string> tags, WeaponData data)
        {
            if (data == null) return;
            switch (data.EquipmentType)
            {
                case EquipmentType.Weapon: tags.Add("melee"); break;
                case EquipmentType.Firearm: tags.Add("firearm"); break;
                case EquipmentType.Bow: tags.Add("bow"); break;
                case EquipmentType.Catalyst: tags.Add("arcane"); break;
            }
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
