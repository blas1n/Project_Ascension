using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Tracks WHEN the player was last actively fighting — the shell's half of the binding-lock
    /// rule (GameSimulation.Combat.BindingRules owns the DECISION; this only supplies the clock).
    /// Combat activity is a fact the game already knows: the player's own <see cref="HitReceiver"/>
    /// firing <see cref="HitReceiver.Damaged"/> (damage taken) and <see cref="GameplayEvents"/>
    /// firing <see cref="GameplayEvents.Attacked"/> (a weapon actually executed — damage dealt).
    /// Reusing these is the point: there is no second notion of "in combat" anywhere else.
    ///
    /// Self-installs like CombatHud/DiscoveryJournalHud, so no scene wiring is needed and it
    /// survives the City&lt;-&gt;Frontier transition. Rebinds to the player's HitReceiver on scene
    /// changes the same way CombatHud does (a destroyed receiver compares == null via Unity's
    /// fake-null, and there may be no player at all in the city).
    /// </summary>
    public sealed class CombatActivityClock : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<CombatActivityClock>() != null) return;
            var go = new GameObject("CombatActivityClock");
            DontDestroyOnLoad(go);
            go.AddComponent<CombatActivityClock>();
        }

        /// <summary>Time.time of the player's last combat activity (damage taken OR dealt), or
        /// null if it has never happened this session — BindingRules.CanRebind treats null as
        /// "always free".</summary>
        public static float? LastCombatTime { get; private set; }

        private HitReceiver _player;

        private void OnEnable() => GameplayEvents.Attacked += OnAttacked;
        private void OnDisable()
        {
            GameplayEvents.Attacked -= OnAttacked;
            Unbind();
        }

        private void Update()
        {
            if (_player == null)
            {
                var playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null && playerGo.TryGetComponent<HitReceiver>(out var hr))
                    Bind(hr);
            }
        }

        private void Bind(HitReceiver hr)
        {
            Unbind();
            _player = hr;
            _player.Damaged += OnDamaged;
        }

        private void Unbind()
        {
            if (_player == null) return;
            _player.Damaged -= OnDamaged;
            _player = null;
        }

        private void OnDamaged(HitReceiver _, float __) => Mark();
        private void OnAttacked(bool _) => Mark();

        private static void Mark() => LastCombatTime = Time.time;
    }
}
