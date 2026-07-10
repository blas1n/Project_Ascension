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
        protected float Damage = 8f;
        protected float ProjectileSpeed = 0f;

        // The AI decision lives in GameSimulation (MonsterAi, headless-tested); this MonoBehaviour
        // only reads its result to move/attack/render. Knockback decay is a GameSimulation constant.
        private MonsterState _state = MonsterState.Idle;
        private Transform _target;
        private IDamageable _targetDamageable;
        private HitReceiver _health;
        private float _nextAttackTime;
        private StatusState _status = StatusState.None;
        private Vector3 _knockback;

        protected Transform Target => _target;
        protected IDamageable TargetDamageable => _targetDamageable;

        /// <summary>Discovery context tag (e.g. "monster:elite"), set by the factory —
        /// defeating this monster flavors the player's discovery context.</summary>
        public string DiscoveryTag { get; set; }

        /// <summary>Resource dropped on death (set by the factory). Empty = none.</summary>
        public string DropItemKey { get; set; } = "";
        public int DropAmount { get; set; }

        public void Configure(float moveSpeed, float aggroRange, float attackRange, float attackCooldown, float damage, float projectileSpeed)
        {
            _moveSpeed = moveSpeed;
            _aggroRange = aggroRange;
            _attackRange = attackRange;
            _attackCooldown = attackCooldown;
            Damage = damage;
            ProjectileSpeed = projectileSpeed;
        }

        private void Awake()
        {
            _health = GetComponent<HitReceiver>();
            _health.Died += OnDied;
        }

        private void Start()
        {
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
            var settings = new MonsterAiSettings(_moveSpeed, _aggroRange, _attackRange, _attackCooldown);

            // The decision is GameSimulation's (headless-tested); this shell only enacts the result.
            var step = MonsterAi.Step(_state, settings, dist, _target != null, _status.IsStunned, Time.time, _nextAttackTime);
            _state = step.State;
            _nextAttackTime = step.NextAttackTime;

            if (_state != MonsterState.Idle) FaceTarget();
            if (step.Move) MoveTowardTarget();
            if (step.Attack && (_targetDamageable == null || !_targetDamageable.IsDead)) PerformAttack();
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
