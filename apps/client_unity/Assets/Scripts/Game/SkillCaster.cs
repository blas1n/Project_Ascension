using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.Net;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Executes a discovered skill in combat: fetches the frozen skill from the server
    /// (GET /api/discoveries/{id}/skill), parses its primitives into an executable
    /// <see cref="Skill"/>, and on cast resolves it with the deterministic
    /// <see cref="SkillResolver"/> and applies the per-target effects. This is the
    /// final link — an AI-composed discovery actually acting in combat.
    ///
    /// Damage, damage-over-time, and the Leech self-heal are applied for real; control,
    /// shield, and dash need assets/animation, so they route to stubs for now
    /// (see SkillEffects, Task 3).
    /// </summary>
    [RequireComponent(typeof(HitReceiver))]
    public sealed class SkillCaster : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "";
        [SerializeField] private Transform aimSource;
        [SerializeField] private float radius = 6f;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private float dotInterval = 1f;

        private DiscoveryApiClient _api;
        private HitReceiver _self;
        private SkillEffects _effects;
        private Loadout _loadout;
        private Skill _skill;
        private string[] _requiredEquipment;

        public bool HasSkill => _skill != null && _skill.Primitives.Count > 0;
        public string SkillName => _skill?.Name ?? "(none)";

        /// <summary>A synthesized-magic weapon (aim + fire) vs an invoked command —
        /// the input layer binds it accordingly.</summary>
        public bool IsWeapon => _manifestation == ManifestationKind.Weapon;

        private ManifestationKind _manifestation = ManifestationKind.Command;

        private void Awake()
        {
            _self = GetComponent<HitReceiver>();
            _effects = GetComponent<SkillEffects>(); // optional presentation stub
            _loadout = FindAnyObjectByType<Loadout>();
            if (!string.IsNullOrWhiteSpace(serverUrl)) _api = new DiscoveryApiClient(serverUrl);
        }

        /// <summary>Fetch a discovered skill from the server and equip it for casting.</summary>
        public void LoadSkill(string discoveryId)
        {
            if (_api == null || string.IsNullOrEmpty(discoveryId)) return;
            StartCoroutine(_api.GetSkill(discoveryId, OnSkillFetched));
        }

        private void OnSkillFetched(SkillResponseDto dto)
        {
            if (dto == null || dto.primitives == null || dto.status != "Ready") return; // still Pending → ignore
            _skill = SkillParser.Parse(string.IsNullOrEmpty(dto.name) ? "Discovery" : dto.name, dto.primitives);
            _manifestation = System.Enum.TryParse<ManifestationKind>(dto.manifestation, ignoreCase: true, out var kind)
                ? kind
                : ManifestationKind.Command;

            // The skill is bound to the equipment it was discovered with (ADR 0005):
            // keep only the equipment-category tags from its context.
            _requiredEquipment = (dto.contextTags ?? new string[0])
                .Where(EquipmentTags.Vocabulary.Contains)
                .ToArray();

            // Register into the session's set — weapon (synthesized magic) or command.
            var discovered = new DiscoveredSkill(_skill.Name, _manifestation, _skill, _requiredEquipment);
            GameSession.Instance?.DiscoveredSkills?.Add(discovered);

            // A command is invoked by re-performing its discovery combo — register it.
            if (_manifestation == ManifestationKind.Command)
            {
                var combo = ComboBuilder.FromBehaviors(dto.behaviors ?? new string[0]);
                FindAnyObjectByType<ComboInvoker>()?.RegisterCommand(combo, discovered);
            }

            var bind = _requiredEquipment.Length > 0 ? string.Join("+", _requiredEquipment) : "any";
            Debug.Log($"[SkillCaster] Equipped \"{_skill.Name}\" as {_manifestation} (needs {bind}, {_skill.Primitives.Count} primitives).");
        }

        /// <summary>Whether the skill can be cast with the gear in hand right now
        /// (ADR 0005 — a discovery is bound to its weapon).</summary>
        public bool CanCast()
        {
            if (!HasSkill) return false;
            if (_requiredEquipment == null || _requiredEquipment.Length == 0) return true;
            var equipped = EquipmentTags.CurrentTags(_loadout);
            // Both hands must be the exact equipment the skill was discovered with.
            return equipped.Count == _requiredEquipment.Length && _requiredEquipment.All(equipped.Contains);
        }

        /// <summary>Execute the equipped weapon skill against nearby targets.</summary>
        public void Cast()
        {
            if (!HasSkill) return;
            if (!CanCast())
            {
                Debug.Log($"[SkillCaster] \"{SkillName}\" needs {string.Join("+", _requiredEquipment)} equipped — cannot cast with the current weapon.");
                return;
            }

            ExecuteSkill(_skill);
        }

        /// <summary>Resolve a skill against nearby targets and apply its effects. Shared
        /// by weapon fire (<see cref="Cast"/>) and combo-invoked commands
        /// (<see cref="ComboInvoker"/>); the caller is responsible for equipment gating.</summary>
        public void ExecuteSkill(Skill skill)
        {
            if (skill == null) return;
            var targets = FindTargets();
            var resolution = SkillResolver.Resolve(skill, targets.Count);
            Apply(resolution, targets);
        }

        private List<IDamageable> FindTargets()
        {
            var origin = aimSource != null ? aimSource.position : transform.position;
            var list = new List<IDamageable>();
            foreach (var col in Physics.OverlapSphere(origin, radius, targetMask))
                if (col.TryGetComponent<IDamageable>(out var d) && !ReferenceEquals(d, _self) && !d.IsDead)
                    list.Add(d);

            // Nearest-first so index 0 is the primary target the resolver focuses on.
            list.Sort((a, b) => SqrDistance(a, origin).CompareTo(SqrDistance(b, origin)));
            return list;
        }

        private static float SqrDistance(IDamageable d, Vector3 origin)
            => d is Component c ? (c.transform.position - origin).sqrMagnitude : float.MaxValue;

        private void Apply(SkillResolution resolution, List<IDamageable> targets)
        {
            foreach (var hit in resolution.Hits)
            {
                if (hit.TargetIndex >= targets.Count) continue;
                var target = targets[hit.TargetIndex];

                if (hit.Damage > 0f) target.TakeDamage(hit.Damage, gameObject);
                if (hit.DamageOverTimePerTick > 0f && hit.DamageOverTimeTicks > 0)
                    StartCoroutine(DamageOverTime(target, hit.DamageOverTimePerTick, hit.DamageOverTimeTicks));
                if (hit.Control != ControlKind.None) PlayControl(target, hit.Control);
            }

            if (resolution.SelfHeal > 0f) _self.Heal(resolution.SelfHeal);
            if (resolution.SelfShield > 0f) GrantShield(resolution.SelfShield);
            if (resolution.DashDistance > 0f) PlayDash(resolution.DashDistance);
        }

        // Non-damage effects need assets — route to the SkillEffects stub layer, or log
        // if it is absent (Task 3 placeholder).
        private void PlayControl(IDamageable target, ControlKind kind)
        {
            if (_effects != null) _effects.PlayControl(target, kind);
            else Debug.Log($"[SkillCaster] {kind} on target (stub).");
        }

        private void GrantShield(float amount)
        {
            if (_effects != null) _effects.GrantShield(amount);
            else Debug.Log($"[SkillCaster] Shield {amount:F0} (stub).");
        }

        private void PlayDash(float distance)
        {
            var direction = aimSource != null ? aimSource.forward : transform.forward;
            if (_effects != null) _effects.PlayDash(direction, distance);
            else Debug.Log($"[SkillCaster] Dash {distance:F0} (stub).");
        }

        private IEnumerator DamageOverTime(IDamageable target, float perTick, int ticks)
        {
            var wait = new WaitForSeconds(dotInterval);
            for (int i = 0; i < ticks && !target.IsDead; i++)
            {
                yield return wait;
                target.TakeDamage(perTick, gameObject);
            }
        }
    }
}
