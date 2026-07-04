using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Applies the player's discovered passives continuously (no invocation): the
    /// aggregate damage reduction is pushed onto the <see cref="HitReceiver"/>, and the
    /// aggregate lifesteal is exposed for the caster to apply when dealing damage.
    /// Recomputed when a passive is discovered. (HitReceiver lives in the Combat
    /// assembly, which can't see the session, so the value is pushed in from here.)
    /// </summary>
    [RequireComponent(typeof(HitReceiver))]
    public sealed class PassiveModifiers : MonoBehaviour
    {
        private HitReceiver _self;

        /// <summary>Aggregate lifesteal fraction from discovered passives.</summary>
        public float Lifesteal { get; private set; }

        private void Awake() => _self = GetComponent<HitReceiver>();
        private void Start() => Refresh();

        /// <summary>Recompute from the session's discovered passives — call when one loads.</summary>
        public void Refresh()
        {
            var set = GameSession.Instance != null ? GameSession.Instance.DiscoveredSkills : null;
            var effect = set != null ? set.AggregatePassive() : PassiveEffect.None;
            if (_self != null) _self.DamageReduction = effect.DamageReduction;
            Lifesteal = effect.Lifesteal;
            MovementCapabilityCatalog.Set(effect.ExtraJumps); // e.g. double jump from a mobility passive
        }
    }
}
