using UnityEngine;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Combat
{
    /// <summary>Something that can receive a control status from a skill — slow / stun
    /// (timed) or knockback (an impulse away from <paramref name="sourcePosition"/>). The
    /// <paramref name="strength"/> is defined by the skill (slow fraction / knockback impulse).</summary>
    public interface IStatusReceiver
    {
        void ApplyControl(ControlKind kind, float duration, float strength, Vector3 sourcePosition);
    }
}
