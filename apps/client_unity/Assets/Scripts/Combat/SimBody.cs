using UnityEngine;
using ProjectAscension.GameSimulation.Physics;
using NumVec3 = System.Numerics.Vector3;
using NumQuat = System.Numerics.Quaternion;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// Describes this GameObject's collider (or CharacterController — it IS a Collider in Unity's
    /// hierarchy) into <see cref="SimWorld"/> as its equivalent <see cref="HitBody"/>. Every
    /// gameplay collider needs one of these registered, or it does not exist to the game (ADR
    /// 0013): it can look perfectly solid on screen and a bullet will still pass through it.
    ///
    /// Re-describes itself every Update — cheap at this project's object counts, and never goes
    /// stale (some geometry, like the windmill's blades, keeps moving after it's built, and most
    /// blockout boxes are never Unity-<c>isStatic</c>-flagged even though they never move, so
    /// "update once" can't safely be inferred from that flag). Unregisters on disable/destroy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimBody : MonoBehaviour
    {
        private int _actorId; // 0 = static level geometry, until Configure says otherwise
        private int _bodyId;
        private Collider _collider;
        private CharacterController _characterController;
        private bool _registered;

        public int ActorId => _actorId;

        /// <summary>Attach this body to an actor (player/monster/anything damageable) — call once,
        /// right after AddComponent&lt;SimBody&gt;(). Leave uncalled (actor id 0) for static level
        /// geometry.</summary>
        public void Configure(int ownerActorId) => _actorId = ownerActorId;

        private void Awake()
        {
            _bodyId = SimWorld.AllocateBodyId();
            _characterController = GetComponent<CharacterController>();
            _collider = GetComponent<Collider>();
        }

        private void OnEnable() => Describe();

        private void Update() => Describe();

        private void OnDisable() => Unregister();

        private void OnDestroy() => Unregister();

        private void Unregister()
        {
            if (!_registered) return;
            SimWorld.Collision.Remove(_bodyId);
            _registered = false;
        }

        private void Describe()
        {
            if (TryBuildBody(out var body))
            {
                SimWorld.Collision.AddOrUpdate(body);
                _registered = true;
            }
            else
            {
                Unregister();
            }
        }

        private bool TryBuildBody(out HitBody body)
        {
            body = default;

            // The player uses a CharacterController, never a CapsuleCollider — it needs its own
            // radius/height/center reading even though it IS technically also a Collider.
            if (_characterController != null)
            {
                if (!_characterController.enabled) return false;
                float scale = UniformScale();
                var center = transform.TransformPoint(_characterController.center);
                body = HitBody.Capsule(_bodyId, _actorId, ToNum(center),
                    _characterController.radius * scale, _characterController.height * scale, ToNum(transform.up));
                return true;
            }

            if (_collider == null || !_collider.enabled || _collider.isTrigger) return false;

            switch (_collider)
            {
                case SphereCollider sphere:
                {
                    float scale = UniformScale();
                    var center = transform.TransformPoint(sphere.center);
                    body = HitBody.Sphere(_bodyId, _actorId, ToNum(center), sphere.radius * scale);
                    return true;
                }
                case CapsuleCollider capsule:
                {
                    float scale = UniformScale();
                    var center = transform.TransformPoint(capsule.center);
                    var localAxis = capsule.direction switch
                    {
                        0 => Vector3.right,
                        2 => Vector3.forward,
                        _ => Vector3.up,
                    };
                    body = HitBody.Capsule(_bodyId, _actorId, ToNum(center),
                        capsule.radius * scale, capsule.height * scale, ToNum(transform.TransformDirection(localAxis)));
                    return true;
                }
                case BoxCollider box:
                {
                    var center = transform.TransformPoint(box.center);
                    var halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
                    body = HitBody.Box(_bodyId, _actorId, ToNum(center), ToNum(halfExtents), ToNumQuat(transform.rotation));
                    return true;
                }
                default:
                    return false; // mesh colliders etc. — not used by the slice's procedural geometry
            }
        }

        // Non-uniform scale isn't meaningfully supported by sphere/capsule colliders in Unity
        // either; every capsule/sphere body this project actually creates scales uniformly
        // (MonsterFactory, the player), so the average axis scale is exact for real usage and a
        // reasonable approximation otherwise.
        private float UniformScale()
        {
            var s = transform.lossyScale;
            return (Mathf.Abs(s.x) + Mathf.Abs(s.y) + Mathf.Abs(s.z)) / 3f;
        }

        private static NumVec3 ToNum(Vector3 v) => new NumVec3(v.x, v.y, v.z);
        private static NumQuat ToNumQuat(Quaternion q) => new NumQuat(q.x, q.y, q.z, q.w);
    }
}
