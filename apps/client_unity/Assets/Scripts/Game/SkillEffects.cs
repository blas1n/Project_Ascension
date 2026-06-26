using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Placeholder presentation for a skill's non-damage effects — control, shield, and
    /// mobility. Real feedback needs VFX/animation assets; until those exist these are
    /// code stubs that log and apply the minimal functional change, so a cast still
    /// reads correctly and an asset-driven view can replace each method later.
    /// Optional component on the caster; <see cref="SkillCaster"/> falls back to logs
    /// when it is absent.
    /// </summary>
    public sealed class SkillEffects : MonoBehaviour
    {
        public float ActiveShield { get; private set; }

        public void PlayControl(IDamageable target, ControlKind kind)
        {
            // TODO(assets): knockback impulse / slow / stun VFX + a hook into target AI.
            Debug.Log($"[SkillEffects] {kind} applied to {NameOf(target)} (stub).", this);
        }

        public void GrantShield(float amount)
        {
            // TODO(assets): shield-bubble VFX + real damage absorption. Tracked for now.
            if (amount <= 0f) return;
            ActiveShield += amount;
            Debug.Log($"[SkillEffects] Shield +{amount:F0} (total {ActiveShield:F0}) (stub).", this);
        }

        public void PlayDash(Vector3 direction, float distance)
        {
            // Functional placeholder: snap forward. Real dash = animation + trail VFX +
            // a swept collision check.
            if (distance <= 0f) return;
            transform.position += direction.normalized * distance;
            Debug.Log($"[SkillEffects] Dash {distance:F0} (stub).", this);
        }

        private static string NameOf(IDamageable d) => d is Component c ? c.gameObject.name : "target";
    }
}
