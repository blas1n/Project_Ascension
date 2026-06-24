using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Fires once when the player gets within range. Uses a distance check rather
    /// than physics triggers — a CharacterController does not reliably raise
    /// OnTriggerEnter, so proximity is the robust approach for the slice.
    /// </summary>
    public abstract class PlayerTriggerVolume : MonoBehaviour
    {
        [SerializeField] private float radius = 1.8f;

        private Transform _player;
        private bool _fired;

        protected virtual void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
                Debug.LogWarning($"[{name}] No 'Player'-tagged object found; proximity disabled.", this);
            else
                _player = player.transform;
        }

        private void Update()
        {
            if (_fired || _player == null) return;

            var delta = _player.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= radius * radius)
            {
                _fired = true;
                Debug.Log($"[{name}] player reached — firing.", this);
                OnPlayerEntered();
            }
        }

        protected abstract void OnPlayerEntered();
    }
}
