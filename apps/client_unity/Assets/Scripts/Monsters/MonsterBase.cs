using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Monsters
{
    /// <summary>
    /// Shared monster AI: Idle -> Chase -> Attack -> Dead. Targets the "Player"
    /// tagged object. Movement is simple XZ MoveTowards (no navmesh for the slice).
    /// Subclasses implement the actual attack. Receives control statuses from skills
    /// (slowed = moves slower, stunned = can't act, knocked back = pushed).
    /// </summary>
    [RequireComponent(typeof(HitReceiver))]
    public abstract class MonsterBase : MonoBehaviour, IStatusReceiver, IMonsterInfo
    {
        private float _moveSpeed = 3f;
        private float _aggroRange = 20f;
        private float _attackRange = 2f;
        private float _attackCooldown = 1f;
        private float _attackWindup = 0f; // telegraph seconds before a strike (DB-driven)
        protected float Damage = 8f;
        protected float ProjectileSpeed = 0f;

        // The AI decision lives in GameSimulation (MonsterAi, headless-tested); this MonoBehaviour
        // only reads its result to move/attack/render. Knockback decay is a GameSimulation constant.
        private MonsterState _state = MonsterState.Idle;
        private Transform _target;
        private IDamageable _targetDamageable;
        private HitReceiver _health;
        private float _nextAttackTime;
        private float _windupEndTime; // carried across ticks so the AI knows when the wind-up strikes
        private StatusState _status = StatusState.None;
        private Vector3 _knockback;

        // The attack tell (rendering only): the monster flashes hot and swells as it winds up, so the
        // player can read + dodge the incoming strike. A code-driven placeholder until real anim/VFX.
        private static readonly Color TelegraphColor = new Color(1f, 0.92f, 0.6f); // hot flash, contrasts all bodies
        private Renderer _renderer;
        private Color _baseColor;
        private Vector3 _baseScale;
        private bool _telegraphing;

        protected Transform Target => _target;
        protected IDamageable TargetDamageable => _targetDamageable;

        /// <summary>Discovery context tag (e.g. "monster:elite"), set by the factory —
        /// defeating this monster flavors the player's discovery context.</summary>
        public string DiscoveryTag { get; set; }

        /// <summary>Resource dropped on death (set by the factory). Empty = none.</summary>
        public string DropItemKey { get; set; } = "";
        public int DropAmount { get; set; }

        public void Configure(float moveSpeed, float aggroRange, float attackRange, float attackCooldown, float attackWindup, float damage, float projectileSpeed)
        {
            _moveSpeed = moveSpeed;
            _aggroRange = aggroRange;
            _attackRange = attackRange;
            _attackCooldown = attackCooldown;
            _attackWindup = attackWindup;
            Damage = damage;
            ProjectileSpeed = projectileSpeed;
        }

        private void Awake()
        {
            _health = GetComponent<HitReceiver>();
            _health.Died += OnDied;
            _renderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            // Capture the tell's rest state now — the factory sets the body colour/scale before the
            // first frame, so this sees the final look to flash from and restore to.
            if (_renderer != null) _baseColor = _renderer.material.color;
            _baseScale = transform.localScale;

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                _target = playerGo.transform;
                playerGo.TryGetComponent(out _targetDamageable);
            }
        }

        private void Update()
        {
            if (_state == MonsterState.Dead) return;

            _status = StatusRules.Tick(_status, Time.deltaTime);
            ApplyKnockback();

            float dist = _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;
            var settings = new MonsterAiSettings(_moveSpeed, _aggroRange, _attackRange, _attackCooldown, _attackWindup);

            // The decision is GameSimulation's (headless-tested); this shell only enacts the result.
            var step = MonsterAi.Step(_state, settings, dist, _target != null, _status.IsStunned, Time.time, _nextAttackTime, _windupEndTime);
            _state = step.State;
            _nextAttackTime = step.NextAttackTime;
            _windupEndTime = step.WindupEndTime;

            if (_state != MonsterState.Idle) FaceTarget();
            UpdateTelegraph(step.Telegraph, Time.time);
            if (step.Move) MoveTowardTarget();
            if (step.Attack && (_targetDamageable == null || !_targetDamageable.IsDead)) PerformAttack();
        }

        // The wind-up tell (rendering only): flash hot + swell as the strike nears, restore when the
        // telegraph ends (the strike landed or the player dodged it). No gameplay effect — the AI owns
        // the timing; this only makes it readable.
        private void UpdateTelegraph(bool telegraphing, float time)
        {
            if (telegraphing)
            {
                float progress = _attackWindup > 0f
                    ? Mathf.Clamp01(1f - (_windupEndTime - time) / _attackWindup)
                    : 1f;
                if (_renderer != null)
                    _renderer.material.color = Color.Lerp(_baseColor, TelegraphColor, 0.65f * progress);
                transform.localScale = _baseScale * (1f + 0.12f * progress);
                _telegraphing = true;
            }
            else if (_telegraphing)
            {
                if (_renderer != null) _renderer.material.color = _baseColor;
                transform.localScale = _baseScale;
                _telegraphing = false;
            }
        }

        private void MoveTowardTarget()
        {
            var p = transform.position;
            var goal = new Vector3(_target.position.x, p.y, _target.position.z);
            float speed = _moveSpeed * _status.SpeedMultiplier; // slowed = move less (skill-defined)
            transform.position = Vector3.MoveTowards(p, goal, speed * Time.deltaTime);
            FaceTarget();
        }

        private void ApplyKnockback()
        {
            if (_knockback.sqrMagnitude < 0.01f) return;
            transform.position += _knockback * Time.deltaTime;
            _knockback = Vector3.MoveTowards(_knockback, Vector3.zero, MonsterAiSettings.KnockbackDecay * Time.deltaTime);
        }

        /// <summary>Receive a control status from a skill. The strength (slow fraction /
        /// knockback impulse) is defined by the skill, not fixed here.</summary>
        public void ApplyControl(ControlKind kind, float duration, float strength, Vector3 sourcePosition)
        {
            if (_state == MonsterState.Dead) return;
            if (kind == ControlKind.Knockback)
            {
                var away = transform.position - sourcePosition;
                away.y = 0f;
                _knockback = (away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward) * strength;
            }
            else
            {
                _status = StatusRules.Apply(_status, kind, duration, strength);
            }
        }

        protected void FaceTarget()
        {
            if (_target == null) return;
            var look = new Vector3(_target.position.x, transform.position.y, _target.position.z) - transform.position;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look);
        }

        private void OnDied(HitReceiver _)
        {
            _state = MonsterState.Dead;
            GameplayEvents.RaiseMonsterKilled(gameObject);
            Destroy(gameObject, 0.1f);
        }

        protected abstract void PerformAttack();
    }
}
