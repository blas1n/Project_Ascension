using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Presentation for a skill's non-damage effects — control, shield, and mobility. The
    /// composed <see cref="SkillVfx"/> supplies the look (control accent, shield bubble,
    /// dash streak); this component applies the functional change and plays the accent.
    /// Optional component on the caster; <see cref="SkillCaster"/> falls back to logs when
    /// it is absent. Damage absorption / animation / swept collision are still simplified.
    /// </summary>
    public sealed class SkillEffects : MonoBehaviour
    {
        public float ActiveShield { get; private set; }

        public void PlayControl(IDamageable target, ControlKind kind)
        {
            if (target is Component c) SkillVfx.ControlAccent(c.transform.position, kind, 1f);
        }

        public void GrantShield(float amount)
        {
            // TODO(assets): real damage absorption. Tracked + a shield bubble for now.
            if (amount <= 0f) return;
            ActiveShield += amount;
            SkillVfx.ShieldBubble(transform.position, 1f);
        }

        public void PlayDash(Vector3 direction, float distance)
        {
            // Functional placeholder: snap forward + a motion streak. Real dash =
            // animation + a swept collision check.
            if (distance <= 0f) return;
            var from = transform.position;
            transform.position += direction.normalized * distance;
            SkillVfx.DashStreak(from, direction, distance, 1f);
        }
    }
}
