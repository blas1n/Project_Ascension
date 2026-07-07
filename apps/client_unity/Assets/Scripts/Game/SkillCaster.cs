using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
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
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private float dotInterval = 1f;

        private DiscoveryApiClient _api;
        private HitReceiver _self;
        private SkillEffects _effects;
        private PassiveModifiers _passives;
        private FocusPool _focus;
        private Skill _skill;
        private EffectNode _graph;          // the held weapon skill's effect graph (ADR 0007), null if graphless
        private string _deliveryStyle = ""; // AI-composed delivery style for the held weapon skill
        private float _intensity = 1f;      // VFX grandeur, from the skill's power (common..legendary)

        public bool HasSkill => _skill != null && _skill.Primitives.Count > 0;
        public string SkillName => _skill?.Name ?? "(none)";

        /// <summary>A synthesized-magic weapon (a new equippable, aim + fire) vs an
        /// invoked command — the input layer binds it accordingly.</summary>
        public bool IsWeapon => _manifestation == ManifestationKind.Weapon;

        private ManifestationKind _manifestation = ManifestationKind.Command;

        private void Awake()
        {
            _self = GetComponent<HitReceiver>();
            // Discovered-skill drivers, provisioned on the player (they were never placed in a
            // scene). AbilitySlots casts Commands from hotkeys (Q/E/R/F), PassiveModifiers
            // applies passives, SkillEffects presents dash/shield/control.
            _effects = GetComponent<SkillEffects>() ?? gameObject.AddComponent<SkillEffects>();
            _passives = GetComponent<PassiveModifiers>() ?? gameObject.AddComponent<PassiveModifiers>();
            if (GetComponent<AbilitySlots>() == null) gameObject.AddComponent<AbilitySlots>();
            if (GetComponent<SkillGuideHud>() == null) gameObject.AddComponent<SkillGuideHud>(); // shows each command's hotkey
            _focus = GetComponent<FocusPool>();           // optional, gates casts by focus
            if (!string.IsNullOrWhiteSpace(serverUrl)) _api = new DiscoveryApiClient(serverUrl);
        }

        // A discovered weapon (SpellWeapon) routes its cast here when fired.
        private void OnEnable() => GameplayEvents.SkillCastRequested += ExecuteSkill;
        private void OnDisable() => GameplayEvents.SkillCastRequested -= ExecuteSkill;

        /// <summary>Fetch a discovered skill from the server and equip it for casting. The
        /// content is composed asynchronously by the AI, so this polls until it's Ready.</summary>
        private readonly HashSet<string> _loaded = new HashSet<string>();

        public void LoadSkill(string discoveryId)
        {
            if (_api == null || string.IsNullOrEmpty(discoveryId)) return;
            // The discovery trigger can re-report the same id across windows — load each once.
            if (!_loaded.Add(discoveryId)) return;
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
            var discovered = DiscoveredSkillFactory.Build(dto, out var weapon);
            _skill = discovered.Skill;
            _graph = discovered.Graph;
            // Delivery SHAPE prefers the graph's Emit (ADR 0007), falling back to the composed
            // delivery string then to primitive inference.
            var graphStyle = _graph != null ? EffectGraphQuery.DeliveryStyle(_graph) : string.Empty;
            _deliveryStyle = !string.IsNullOrEmpty(graphStyle) ? graphStyle : (dto.delivery ?? string.Empty);
            _intensity = SkillVfx.Intensity(dto.powerCost); // grander VFX for rarer/stronger skills
            _manifestation = discovered.Manifestation;

            // Add to the session's set — the single source of truth. A command is picked up by
            // AbilitySlots (which syncs from the set), a passive by PassiveModifiers. We dedupe
            // by discovery id (LoadSkill, _loaded), NOT by name: distinct discoveries can share
            // a composed name yet differ mechanically.
            GameSession.Instance?.DiscoveredSkills?.Add(discovered);

            if (_manifestation == ManifestationKind.Passive)
                (GetComponent<PassiveModifiers>() ?? FindAnyObjectByType<PassiveModifiers>())?.Refresh();
            else if (_manifestation == ManifestationKind.Weapon && weapon != null)
            {
                // With a session the weapon goes to inventory (equip in the city); without one
                // (Frontier played directly) equip it now rather than drop it.
                var state = GameSession.Instance?.PlayerState;
                if (state != null)
                {
                    state.AddWeapon(weapon);
                    Debug.Log($"[SkillCaster] \"{_skill.Name}\" added to inventory ({state.OwnedWeapons.Count} owned) — equip it in the city.");
                }
                else
                {
                    Debug.LogWarning($"[SkillCaster] No GameSession — equipping \"{_skill.Name}\" now.");
                    FindAnyObjectByType<Loadout>()?.EquipLeft(weapon);
                }
            }

            Debug.Log($"[SkillCaster] Discovered \"{_skill.Name}\" as {_manifestation} ({_skill.Primitives.Count} primitives).");
        }

        /// <summary>Execute the equipped weapon skill against nearby targets.</summary>
        public void Cast()
        {
            if (HasSkill) ExecuteSkill(_skill);
        }

        /// <summary>Resolve a skill against nearby targets and apply its effects. Shared
        /// by weapon fire (<see cref="Cast"/>) and hotkey-cast commands
        /// (<see cref="AbilitySlots"/>). This <see cref="Skill"/>-only overload backs the cast
        /// EVENT (Action&lt;Skill&gt;); the held weapon supplies its own graph.</summary>
        public void ExecuteSkill(Skill skill)
            => ExecuteSkill(skill, ReferenceEquals(skill, _skill) ? _graph : null);

        /// <summary>As above, driven by the skill's effect GRAPH (ADR 0007) when it has one — the
        /// graph resolves combat via <see cref="GraphSkillResolver"/> and picks the delivery/homing;
        /// a graphless skill falls back to the primitive <see cref="SkillResolver"/> path.</summary>
        public void ExecuteSkill(Skill skill, EffectNode graph)
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

            // Delivery SHAPE: from the skill's graph Emit (ADR 0007) when it has one, else the
            // held weapon's composed style, else derived from primitives. Either way a projectile
            // flies, a beam hitscans, a burst lands. Numbers are resolved in ResolveAt.
            var graphStyle = graph != null ? EffectGraphQuery.DeliveryStyle(graph) : string.Empty;
            var style = !string.IsNullOrEmpty(graphStyle) ? graphStyle : _deliveryStyle;
            var spec = DeliveryStyles.ForStyle(style, tuning) ?? DeliveryInference.From(skill, tuning);
            var origin = aimSource != null ? aimSource.position : transform.position;
            var dir = aimSource != null ? aimSource.forward : transform.forward;

            // Composed VFX: the delivery is the SHAPE, the skill's theme the element COLOUR,
            // its power the INTENSITY — assembled per skill (see SkillVfx).
            var color = SkillVfx.ElementColor(skill.Name);

            if (spec.Motion == DeliveryMotion.Projectile)
            {
                bool homing = graph != null ? EffectGraphQuery.HasHoming(graph) : HasPrimitive(skill, SkillPrimitiveKind.Homing);
                SpawnProjectile(origin, dir, spec, color, homing, point =>
                {
                    SkillVfx.Burst(point, color, _intensity);
                    ResolveAt(skill, graph, point, spec);
                });
                return;
            }

            // Instant deliveries — a ring around the caster (Self/nova), an eruption at the
            // aimed point (AimPoint/burst), or a beam of light (Muzzle+Line/beam).
            var resolvePoint = spec.Origin == DeliveryOrigin.Self
                ? transform.position
                : AimPoint(origin, dir, spec.Range);
            if (spec.Origin == DeliveryOrigin.Self)
                SkillVfx.Nova(resolvePoint, color, spec.Radius, _intensity);
            else if (spec.Shape == DeliveryShape.Line)
                SkillVfx.Beam(origin, resolvePoint, color, _intensity);
            else
                SkillVfx.Burst(resolvePoint, color, _intensity);
            ResolveAt(skill, graph, resolvePoint, spec);
        }

        // Resolve a skill's effects against everything within the delivery's footprint at a
        // point. Shared by instant deliveries (now) and a projectile's impact (callback). The
        // graph resolves via GraphSkillResolver (ADR 0007); a graphless skill via primitives.
        private void ResolveAt(Skill skill, EffectNode graph, Vector3 point, DeliverySpec spec)
        {
            if (this == null) return; // caster gone (e.g. projectile outlived the scene)
            var targets = TargetsAround(point, spec.Radius);
            var resolution = graph != null
                ? GraphSkillResolver.Resolve(graph, targets.Count, CombatTuningCatalog.Current)
                : SkillResolver.Resolve(skill, targets.Count, CombatTuningCatalog.Current);
            Apply(resolution, targets);

            // Composed VFX: the skill's primitives add impact accents (chain arcs, fork
            // streaks, a knockback shockwave, a lingering damage-over-time pool, a leech
            // tether) on top of the delivery shape.
            var points = new List<Vector3>(targets.Count);
            foreach (var t in targets)
                if (t is Component c) points.Add(c.transform.position);
            SkillVfx.PlayImpactModifiers(skill, point, points, transform.position, _intensity);
        }

        private static bool HasPrimitive(Skill skill, SkillPrimitiveKind kind)
        {
            foreach (var p in skill.Primitives)
                if (p.Kind == kind) return true;
            return false;
        }

        private List<IDamageable> TargetsAround(Vector3 point, float radius)
        {
            var list = new List<IDamageable>();
            foreach (var col in Physics.OverlapSphere(point, radius, targetMask))
                if (col.TryGetComponent<IDamageable>(out var d) && !ReferenceEquals(d, _self) && !d.IsDead)
                    list.Add(d);
            // Nearest-to-impact first so index 0 is the primary target the resolver focuses on.
            list.Sort((a, b) => SqrDistance(a, point).CompareTo(SqrDistance(b, point)));
            return list;
        }

        // Where an instant delivery resolves: the first thing aimed at, or the far reach.
        private Vector3 AimPoint(Vector3 origin, Vector3 dir, float range)
            => Physics.Raycast(origin + dir * 0.5f, dir, out var hit, range, targetMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : origin + dir * range;

        private void SpawnProjectile(Vector3 origin, Vector3 dir, DeliverySpec spec, Color color, bool homing, System.Action<Vector3> onImpact)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SkillProjectile";
            Destroy(go.GetComponent<Collider>()); // the projectile does its own linecast
            go.AddComponent<SkillProjectile>().Launch(
                origin + dir * 0.6f, dir, spec.Speed, spec.Gravity, spec.Range, targetMask, onImpact, color, _intensity);
            if (homing) SkillVfx.HomingAccent(go, color, _intensity); // curling motes trail
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
