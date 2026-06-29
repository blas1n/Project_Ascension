using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Connects the server discovery path to skill execution: when the discovery trigger
    /// fires (<see cref="DiscoveryReporter.Fired"/>), the <see cref="SkillCaster"/> fetches
    /// the composed skill and mints/equips it. Without this, a fired discovery never becomes
    /// a usable weapon/command. A scene-wiring component so the reporter and caster stay
    /// decoupled.
    /// </summary>
    public sealed class DiscoverySkillBinder : MonoBehaviour
    {
        private DiscoveryReporter _reporter;
        private SkillCaster _caster;

        private void Start()
        {
            _reporter = FindAnyObjectByType<DiscoveryReporter>();
            _caster = FindAnyObjectByType<SkillCaster>();
            if (_reporter != null && _caster != null)
                _reporter.Fired += _caster.LoadSkill;
            else
                Debug.LogWarning("[DiscoverySkillBinder] Missing DiscoveryReporter or SkillCaster — discovered skills won't be minted.");
        }

        private void OnDestroy()
        {
            if (_reporter != null && _caster != null)
                _reporter.Fired -= _caster.LoadSkill;
        }
    }
}
