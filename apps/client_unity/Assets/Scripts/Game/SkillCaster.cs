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
        [SerializeField] private string actorId = "11111111-1111-1111-1111-111111111111"; // for restoring prior discoveries
        [SerializeField] private Transform aimSource;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private float dotInterval = 1f;

        private DiscoveryApiClient _api;
        private HitReceiver _self;
        private SkillEffects _effects;
        private PassiveModifiers _passives;
        private FocusPool _focus;
        private Skill _skill;
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
            // Discovered-skill drivers. These were never placed in the scene, so a Command
            // never fired (no ComboInvoker to catch its combo) and passives never applied (no
            // PassiveModifiers). Provision them on the player here so discovered Commands and
            // passives actually work; SkillEffects presents dash/shield/control.
            _effects = GetComponent<SkillEffects>() ?? gameObject.AddComponent<SkillEffects>();
            _passives = GetComponent<PassiveModifiers>() ?? gameObject.AddComponent<PassiveModifiers>();
            if (GetComponent<ComboInvoker>() == null) gameObject.AddComponent<ComboInvoker>();
            if (GetComponent<SkillGuideHud>() == null) gameObject.AddComponent<SkillGuideHud>(); // shows each command's combo
            _focus = GetComponent<FocusPool>();           // optional, gates casts by focus
            if (!string.IsNullOrWhiteSpace(serverUrl)) _api = new DiscoveryApiClient(serverUrl);
        }

        // A discovered weapon (SpellWeapon) routes its cast here when fired.
        private void OnEnable() => GameplayEvents.SkillCastRequested += ExecuteSkill;
        private void OnDisable() => GameplayEvents.SkillCastRequested -= ExecuteSkill;

        // Restore previously-discovered skills into the session. A discovery's claim persists
        // server-side, so re-playing the same behavior returns fired=false and the reporter
        // never re-loads it — without this restore, every command/passive/weapon discovered in
        // a past session is lost on restart (empty guide, dead commands). Runs only when the
        // set is empty (a fresh session); re-entering the frontier keeps what's already loaded.
        private void Start()
        {
            if (_api == null || string.IsNullOrEmpty(actorId)) return;
            var set = GameSession.Instance?.DiscoveredSkills;
            if (set == null) return;
            if (set.Weapons.Count > 0 || set.Commands.Count > 0 || set.Passives.Count > 0) return;
            StartCoroutine(RestoreDiscoveredSkills());
        }

        private IEnumerator RestoreDiscoveredSkills()
        {
            DiscoveryListItemDto[] list = null;
            yield return _api.GetByActor(actorId, r => list = r);
            if (list == null) yield break;
            foreach (var d in list)
                if (!string.IsNullOrEmpty(d.id))
                    LoadSkill(d.id); // polls the (already-Ready) skill, adds it to the set + registers it
        }

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
            _skill = SkillParser.Parse(string.IsNullOrEmpty(dto.name) ? "Discovery" : dto.name, dto.primitives);
            _deliveryStyle = dto.delivery ?? string.Empty; // AI-composed manifestation ("" → derive)
            _intensity = SkillVfx.Intensity(dto.powerCost); // grander VFX for rarer/stronger skills
            _manifestation = System.Enum.TryParse<ManifestationKind>(dto.manifestation, ignoreCase: true, out var kind)
                ? kind
                : ManifestationKind.Command;

            // Register into the session's set — weapon (a new equippable) or command. We
            // dedupe by discovery id (LoadSkill, _loaded) but NOT by name: distinct
            // discoveries can share a composed name yet differ mechanically, and dropping
            // them by name meant a genuinely-new discovered weapon never reached inventory.
            var set = GameSession.Instance?.DiscoveredSkills;

            // The command's invocation combo (empty for weapons/passives) — carried on the
            // DiscoveredSkill so the guide HUD can show the player how to trigger it.
            IReadOnlyList<InputToken> combo = _manifestation == ManifestationKind.Command
                ? InputCombo.Parse(dto.invocationCombo ?? new string[0])
                : System.Array.Empty<InputToken>();
            var discovered = new DiscoveredSkill(_skill.Name, _manifestation, _skill, combo);
            set?.Add(discovered);

            // A command is invoked by its assigned combo; a passive applies continuously.
            if (_manifestation == ManifestationKind.Command)
                (GetComponent<ComboInvoker>() ?? FindAnyObjectByType<ComboInvoker>())?.RegisterCommand(combo, discovered);
            else if (_manifestation == ManifestationKind.Passive)
                (GetComponent<PassiveModifiers>() ?? FindAnyObjectByType<PassiveModifiers>())?.Refresh();
            else if (_manifestation == ManifestationKind.Weapon)
            {
                // Mint a new equippable weapon. With a session (the full Bootstrap→City→
                // Frontier flow) it goes to inventory only; the player equips it from the
                // city loadout. WITHOUT a session (the Frontier scene played directly) there
                // is no city inventory to hold it, so equip it now rather than silently drop
                // it — the prior null-safe AddWeapon made discovered weapons vanish.
                var weapon = WeaponData.CreateDiscovered(_skill.Name, _skill, "spell:" + Slug(_skill.Name));
                var state = GameSession.Instance?.PlayerState;
                if (state != null)
                {
                    state.AddWeapon(weapon);
                    Debug.Log($"[SkillCaster] \"{_skill.Name}\" added to inventory ({state.OwnedWeapons.Count} owned) — equip it in the city.");
                }
                else
                {
                    Debug.LogWarning($"[SkillCaster] No GameSession (Frontier played directly) — equipping \"{_skill.Name}\" now. Start from Bootstrap for the inventory/city loop.");
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

            // Manifestation comes from the AI-composed delivery style (how the skill was
            // composed to manifest); when absent it's derived from the primitives. Either
            // way a projectile flies, a beam hitscans, a burst lands — each discovered skill
            // delivers differently. The effect numbers stay with SkillResolver (ResolveAt).
            var spec = DeliveryStyles.ForStyle(_deliveryStyle, tuning) ?? DeliveryInference.From(skill, tuning);
            var origin = aimSource != null ? aimSource.position : transform.position;
            var dir = aimSource != null ? aimSource.forward : transform.forward;

            // Composed VFX: the delivery is the SHAPE, the skill's theme the element COLOUR,
            // its power the INTENSITY — assembled per skill (see SkillVfx).
            var color = SkillVfx.ElementColor(skill.Name);

            if (spec.Motion == DeliveryMotion.Projectile)
            {
                bool homing = HasPrimitive(skill, SkillPrimitiveKind.Homing);
                SpawnProjectile(origin, dir, spec, color, homing, point =>
                {
                    SkillVfx.Burst(point, color, _intensity);
                    ResolveAt(skill, point, spec);
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
            ResolveAt(skill, resolvePoint, spec);
        }

        // Resolve a skill's effects against everything within the delivery's footprint at a
        // point. Shared by instant deliveries (now) and a projectile's impact (callback).
        private void ResolveAt(Skill skill, Vector3 point, DeliverySpec spec)
        {
            if (this == null) return; // caster gone (e.g. projectile outlived the scene)
            var targets = TargetsAround(point, spec.Radius);
            var resolution = SkillResolver.Resolve(skill, targets.Count, CombatTuningCatalog.Current);
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
