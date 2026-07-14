using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.Net;
using NumVec3 = System.Numerics.Vector3;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Executes a discovered skill in combat: fetches the frozen skill from the server
    /// (GET /api/discoveries/{id}/skill), reads its effect graph, and on cast resolves it with the
    /// deterministic <see cref="GraphSkillResolver"/> (ADR 0007) and applies the per-target
    /// effects. This is the final link — an AI-composed discovery actually acting in combat.
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

        // A loaded skill is castable via its effect graph (ADR 0007) — graph-only skills carry no
        // primitives, so DON'T gate on them (that dropped weapon firing for composed skills).
        public bool HasSkill => _skill != null;
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
                // A composed skill is graph-only now (ADR 0007 Phase 4c) — don't require primitives
                // (the same legacy guard that dropped the session-start restore).
                if (fetched != null && fetched.status == "Ready")
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
            if (discovered == null) return; // not Ready (shouldn't reach here; defensive)
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

            // Announce the discovery with the SERVER-composed name (the frontier toast + any other
            // observer). The client no longer names discoveries locally — server is authoritative.
            GameplayEvents.RaiseSkillDiscovered(_skill.Name, _manifestation);

            if (_manifestation == ManifestationKind.Passive)
                (GetComponent<PassiveModifiers>() ?? FindAnyObjectByType<PassiveModifiers>())?.Refresh();
            else if (_manifestation == ManifestationKind.Weapon && weapon != null)
            {
                // A discovery is NOT an equip: the weapon goes to inventory only. Auto-equipping a
                // fresh discovery took the choice away from the player, and — because equipping it
                // changed the discovery ladder's style key — is exactly what fed the "discover ->
                // equip -> discover" ladder-multiplication bug. The player equips it deliberately
                // at the Equipment Station (CLAUDE.md Phase 7).
                var state = GameSession.Instance?.PlayerState;
                if (state != null)
                {
                    state.AddWeapon(weapon);
                    Debug.Log($"[SkillCaster] \"{_skill.Name}\" added to inventory ({state.OwnedWeapons.Count} owned) — equip it at the Equipment Station.");
                }
                else
                {
                    // No session (e.g. Frontier played standalone) means no PlayerState to hold it —
                    // there is nowhere to store the weapon, so it is not minted into the world.
                    Debug.LogWarning($"[SkillCaster] No GameSession — \"{_skill.Name}\" discovered but not stored (no PlayerState).");
                }
            }

            Debug.Log($"[SkillCaster] Discovered \"{_skill.Name}\" as {_manifestation} (delivery={_deliveryStyle}).");
        }

        /// <summary>Execute the equipped weapon skill against nearby targets.</summary>
        public void Cast()
        {
            if (HasSkill) ExecuteSkill(_skill);
        }

        /// <summary>Resolve a skill against nearby targets and apply its effects. Shared
        /// by weapon fire (<see cref="Cast"/>) and hotkey-cast commands
        /// (<see cref="AbilitySlots"/>). This <see cref="Skill"/>-only overload backs the cast
        /// EVENT (Action&lt;Skill&gt;); it supplies a graph (the held weapon's, or a translation of
        /// this skill's primitives) so the runtime is always graph-driven (ADR 0007 Phase 4c-4).</summary>
        public void ExecuteSkill(Skill skill)
            => ExecuteSkill(skill, ReferenceEquals(skill, _skill) && _graph != null
                ? _graph
                : PrimitiveGraphTranslator.Translate(skill));

        /// <summary>As above, driven entirely by the skill's effect GRAPH (ADR 0007) — combat via
        /// <see cref="GraphSkillResolver"/>, delivery/homing/VFX from the graph. The graph is always
        /// present (composed or translated), so there is no primitive fallback path.</summary>
        public void ExecuteSkill(Skill skill, EffectNode graph)
        {
            if (skill == null) return;

            // DB-driven combat balance (fetched at startup; Default offline).
            var tuning = CombatTuningCatalog.Current;

            // Skills cost focus (combat-framework 집중력); refuse the cast when short — from the graph.
            if (_focus != null && !_focus.TrySpend(FocusCost.Of(graph, tuning)))
            {
                Debug.Log($"[SkillCaster] Not enough focus to cast \"{skill.Name}\".");
                return;
            }

            // Delivery SHAPE from the graph's Emit (ADR 0007); the held weapon's composed style is
            // the fallback when the graph emits nothing (movement/ward). A projectile flies, a beam
            // hitscans, a burst lands. Numbers are resolved in ResolveAt.
            var graphStyle = EffectGraphQuery.DeliveryStyle(graph);
            var style = !string.IsNullOrEmpty(graphStyle) ? graphStyle : _deliveryStyle;
            var spec = DeliveryStyles.ForStyle(style, tuning) ?? DeliveryInference.From(skill, tuning);
            var origin = aimSource != null ? aimSource.position : transform.position;
            var dir = aimSource != null ? aimSource.forward : transform.forward;

            // Composed VFX: the delivery is the SHAPE, the skill's theme the element COLOUR,
            // its power the INTENSITY — assembled per skill (see SkillVfx).
            var color = SkillVfx.ElementColor(skill.Name);

            if (spec.Motion == DeliveryMotion.Projectile)
            {
                bool homing = EffectGraphQuery.HasHoming(graph);
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

        // Resolve a skill's effects against everything within the delivery's footprint at a point.
        // Shared by instant deliveries (now) and a projectile's impact (callback). Always via the
        // graph (ADR 0007 Phase 4c-4 — every skill has one, composed or translated).
        private void ResolveAt(Skill skill, EffectNode graph, Vector3 point, DeliverySpec spec)
        {
            if (this == null) return; // caster gone (e.g. projectile outlived the scene)
            var targets = TargetsAround(point, spec.Radius);
            var resolution = GraphSkillResolver.Resolve(graph, targets.Count, CombatTuningCatalog.Current);
            Apply(resolution, targets);

            // Composed VFX: the skill's primitives add impact accents (chain arcs, fork
            // streaks, a knockback shockwave, a lingering damage-over-time pool, a leech
            // tether) on top of the delivery shape.
            var points = new List<Vector3>(targets.Count);
            foreach (var t in targets)
                if (t is Component c) points.Add(c.transform.position);
            SkillVfx.PlayImpactModifiers(graph, skill.Name, point, points, transform.position, _intensity);
        }


        private List<IDamageable> TargetsAround(Vector3 point, float radius)
        {
            var list = new List<IDamageable>();
            int selfActorId = SimWorld.ActorIdOf(gameObject);
            foreach (var actorId in SimWorld.Collision.OverlapSphere(ToNum(point), radius, selfActorId))
                if (SimWorld.TryGetDamageable(actorId, out var d) && !ReferenceEquals(d, _self) && !d.IsDead)
                    list.Add(d);
            // Nearest-to-impact first so index 0 is the primary target the resolver focuses on.
            list.Sort((a, b) => SqrDistance(a, point).CompareTo(SqrDistance(b, point)));
            return list;
        }

        // Where an instant delivery resolves: the first thing aimed at, or the far reach.
        private Vector3 AimPoint(Vector3 origin, Vector3 dir, float range)
        {
            int selfActorId = SimWorld.ActorIdOf(gameObject);
            var from = origin + dir * 0.5f;
            return SimWorld.Collision.SweepSphere(ToNum(from), ToNum(origin + dir * range), 0f, selfActorId, out var hit)
                ? ToUnity(hit.Point)
                : origin + dir * range;
        }

        private void SpawnProjectile(Vector3 origin, Vector3 dir, DeliverySpec spec, Color color, bool homing, System.Action<Vector3> onImpact)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SkillProjectile";
            Destroy(go.GetComponent<Collider>()); // the projectile does its own sweep
            go.AddComponent<SkillProjectile>().Launch(
                origin + dir * 0.6f, dir, spec.Speed, spec.Gravity, spec.Range, SimWorld.ActorIdOf(gameObject), onImpact, color, _intensity);
            if (homing) SkillVfx.HomingAccent(go, color, _intensity); // curling motes trail
        }

        private static NumVec3 ToNum(Vector3 v) => new NumVec3(v.x, v.y, v.z);
        private static Vector3 ToUnity(NumVec3 v) => new Vector3(v.X, v.Y, v.Z);

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
