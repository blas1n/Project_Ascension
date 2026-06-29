using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        private PassiveModifiers _passives;
        private FocusPool _focus;
        private Skill _skill;

        public bool HasSkill => _skill != null && _skill.Primitives.Count > 0;
        public string SkillName => _skill?.Name ?? "(none)";

        /// <summary>A synthesized-magic weapon (a new equippable, aim + fire) vs an
        /// invoked command — the input layer binds it accordingly.</summary>
        public bool IsWeapon => _manifestation == ManifestationKind.Weapon;

        private ManifestationKind _manifestation = ManifestationKind.Command;

        private void Awake()
        {
            _self = GetComponent<HitReceiver>();
            _effects = GetComponent<SkillEffects>(); // optional presentation stub
            _passives = GetComponent<PassiveModifiers>(); // optional, for passive lifesteal
            _focus = GetComponent<FocusPool>();           // optional, gates casts by focus
            if (!string.IsNullOrWhiteSpace(serverUrl)) _api = new DiscoveryApiClient(serverUrl);
        }

        // A discovered weapon (SpellWeapon) routes its cast here when fired.
        private void OnEnable() => GameplayEvents.SkillCastRequested += ExecuteSkill;
        private void OnDisable() => GameplayEvents.SkillCastRequested -= ExecuteSkill;

        /// <summary>Fetch a discovered skill from the server and equip it for casting. The
        /// content is composed asynchronously by the AI, so this polls until it's Ready.</summary>
        public void LoadSkill(string discoveryId)
        {
            if (_api == null || string.IsNullOrEmpty(discoveryId)) return;
            StartCoroutine(PollSkill(discoveryId));
        }

        private IEnumerator PollSkill(string discoveryId)
        {
            for (int attempt = 0; attempt < 20; attempt++) // ~40s for async AI composition
            {
                SkillResponseDto fetched = null;
                yield return _api.GetSkill(discoveryId, dto => fetched = dto);
                if (fetched != null && fetched.status == "Ready" && fetched.primitives != null)
                {
                    OnSkillReady(fetched);
                    yield break;
                }
                yield return new WaitForSeconds(2f); // still Pending — wait and retry
            }
            Debug.LogWarning($"[SkillCaster] Discovery {discoveryId} did not compose in time.");
        }

        private void OnSkillReady(SkillResponseDto dto)
        {
            _skill = SkillParser.Parse(string.IsNullOrEmpty(dto.name) ? "Discovery" : dto.name, dto.primitives);
            _manifestation = System.Enum.TryParse<ManifestationKind>(dto.manifestation, ignoreCase: true, out var kind)
                ? kind
                : ManifestationKind.Command;

            // Register into the session's set — weapon (a new equippable) or command.
            var discovered = new DiscoveredSkill(_skill.Name, _manifestation, _skill);
            GameSession.Instance?.DiscoveredSkills?.Add(discovered);

            // A command is invoked by its assigned combo; a passive applies continuously.
            if (_manifestation == ManifestationKind.Command)
            {
                var combo = InputCombo.Parse(dto.invocationCombo ?? new string[0]);
                FindAnyObjectByType<ComboInvoker>()?.RegisterCommand(combo, discovered);
            }
            else if (_manifestation == ManifestationKind.Passive)
            {
                FindAnyObjectByType<PassiveModifiers>()?.Refresh();
            }
            else if (_manifestation == ManifestationKind.Weapon)
            {
                // Mint a new equippable weapon, add it to inventory, select it (so it
                // persists across City<->Frontier), and equip it now — equipping
                // contributes its context tag, opening further discoveries (the loop).
                var weapon = WeaponData.CreateDiscovered(_skill.Name, _skill, "spell:" + Slug(_skill.Name));
                var state = GameSession.Instance?.PlayerState;
                if (state != null)
                {
                    state.AddWeapon(weapon);
                    state.SetLeft(weapon);
                }
                FindAnyObjectByType<Loadout>()?.EquipLeft(weapon);
            }

            Debug.Log($"[SkillCaster] Discovered \"{_skill.Name}\" as {_manifestation} ({_skill.Primitives.Count} primitives).");
        }

        /// <summary>Execute the equipped weapon skill against nearby targets.</summary>
        public void Cast()
        {
            if (HasSkill) ExecuteSkill(_skill);
        }

        private static string Slug(string name)
        {
            if (string.IsNullOrEmpty(name)) return "discovery";
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
            return sb.ToString();
        }

        /// <summary>Resolve a skill against nearby targets and apply its effects. Shared
        /// by weapon fire (<see cref="Cast"/>) and combo-invoked commands
        /// (<see cref="ComboInvoker"/>).</summary>
        public void ExecuteSkill(Skill skill)
        {
            if (skill == null) return;

            // DB-driven combat balance (fetched at startup; Default offline).
            var tuning = CombatTuningCatalog.Current;

            // Skills cost focus (combat-framework 집중력); refuse the cast when short.
            if (_focus != null && !_focus.TrySpend(FocusCost.Of(skill, tuning)))
            {
                Debug.Log($"[SkillCaster] Not enough focus to cast \"{skill.Name}\".");
                return;
            }

            var targets = FindTargets();
            var resolution = SkillResolver.Resolve(skill, targets.Count, tuning);
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
                if (hit.Control != ControlKind.None) PlayControl(target, hit.Control, hit.ControlDuration, hit.ControlStrength);
            }

            if (resolution.SelfHeal > 0f) _self.Heal(resolution.SelfHeal);
            if (resolution.SelfShield > 0f) GrantShield(resolution.SelfShield);
            if (resolution.DashDistance > 0f) PlayDash(resolution.DashDistance);

            // Passive lifesteal: a fraction of the damage dealt returns as health.
            if (_passives != null && _passives.Lifesteal > 0f && resolution.ImmediateDamage > 0f)
                _self.Heal(resolution.ImmediateDamage * _passives.Lifesteal);
        }

        // Apply the control to the target's status receiver (real slow/stun/knockback);
        // strength is skill-defined. The SkillEffects stub still plays placeholder VFX.
        private void PlayControl(IDamageable target, ControlKind kind, float duration, float strength)
        {
            if (target is Component c && c.TryGetComponent<IStatusReceiver>(out var receiver))
                receiver.ApplyControl(kind, duration, strength, transform.position);
            if (_effects != null) _effects.PlayControl(target, kind);
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
