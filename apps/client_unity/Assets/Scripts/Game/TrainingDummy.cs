using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// A training-ground dummy: it takes hits, flinches so the blow reads, and never dies — it just
    /// stands back up. The training ground needs something to hit, because the doc's first hour teaches
    /// the verbs by DOING them (and a discovery arises from that doing), not by reading a tooltip.
    ///
    /// Rendering/feel only: damage is still resolved by HitReceiver + the sim.
    /// </summary>
    [RequireComponent(typeof(HitReceiver))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        private const float FlinchSeconds = 0.12f;
        private const float ResetAfterSeconds = 1.5f; // stand back up shortly after the last hit

        private HitReceiver _health;
        private Renderer _renderer;
        private Color _baseColor;
        private Vector3 _baseScale;
        private float _flinch;
        private float _resetAt;

        private void Awake()
        {
            _health = GetComponent<HitReceiver>();
            _health.Damaged += OnDamaged;
            _renderer = GetComponentInChildren<Renderer>();
            _baseScale = transform.localScale;
        }

        private void Start()
        {
            if (_renderer != null) _baseColor = _renderer.material.color;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Damaged -= OnDamaged;
        }

        private void OnDamaged(HitReceiver _, float __)
        {
            _flinch = 1f;
            _resetAt = Time.time + ResetAfterSeconds;
        }

        private void Update()
        {
            if (_flinch > 0f)
            {
                _flinch = Mathf.MoveTowards(_flinch, 0f, Time.deltaTime / FlinchSeconds);
                // Squash + redden on impact so a hit is unmistakable even without animation.
                if (_renderer != null)
                    _renderer.material.color = Color.Lerp(_baseColor, new Color(1f, 0.4f, 0.35f), _flinch);
                transform.localScale = new Vector3(
                    _baseScale.x * (1f + 0.12f * _flinch),
                    _baseScale.y * (1f - 0.15f * _flinch),
                    _baseScale.z * (1f + 0.12f * _flinch));
            }

            // A dummy that stays dead teaches nothing — put it back up.
            if (_resetAt > 0f && Time.time >= _resetAt)
            {
                _resetAt = 0f;
                _health.Revive();
            }
        }
    }
}
