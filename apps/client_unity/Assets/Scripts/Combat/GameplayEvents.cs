using System;
using UnityEngine;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// The single stream of gameplay facts — things that actually happened, not
    /// inputs or intents. Each fact is raised by the system that owns the action at
    /// the moment it executes (past grounded/cooldown gates, on death, on pickup).
    /// Facts are domain-level and carry no consumer semantics: a fact is produced
    /// once and any number of observers (discovery relay, contracts, future
    /// achievements/analytics) subscribe. New observable thing = a new fact here.
    /// </summary>
    public static class GameplayEvents
    {
        // Player execution facts.
        public static event Action Jumped;
        public static event Action Dodged;
        public static event Action<bool> Attacked; // isMelee
        public static event Action ChargedAttacked; // a high-charge shot was loosed
        public static event Action AirAttacked;     // a blow struck while airborne (공중 공격)
        /// <summary>A weapon of this context ("arcane", "firearm", "melee", "bow") was just used.
        /// Carries WHAT was used, not merely that something was — so a fusion of the two hands can be
        /// observed instead of inferred from the loadout (ADR 0008).</summary>
        public static event Action<string> WeaponUsed;

        // Raw attack-button inputs (left/right click) — feed the command combo
        // recognizer, which needs the button press, not the weapon-fire execution.
        public static event Action LeftClicked;
        public static event Action RightClicked;

        // A discovered weapon asks the skill executor to run its skill (routing event;
        // the weapon lives in the Equipment assembly, the executor in Game).
        public static event Action<Skill> SkillCastRequested;

        // The player died (and will respawn) — the delegation tutorial uses this as its
        // teachable moment.
        public static event Action PlayerDied;

        // A discovery finished composing on the server and its skill loaded — carries the
        // AI-composed NAME. The single source of a discovery's identity: the client no longer
        // names discoveries locally (the server is authoritative — ADR 0002/0004).
        public static event Action<string> SkillDiscovered;

        // World facts (argument is the subject GameObject).
        public static event Action<GameObject> MonsterKilled;
        public static event Action<GameObject> SampleCollected;
        public static event Action<GameObject> MarkerSurveyed;

        public static void RaiseJumped() => Jumped?.Invoke();
        public static void RaiseDodged() => Dodged?.Invoke();
        public static void RaiseAttacked(bool isMelee) => Attacked?.Invoke(isMelee);
        public static void RaiseChargedAttacked() => ChargedAttacked?.Invoke();
        public static void RaiseAirAttacked() => AirAttacked?.Invoke();
        public static void RaiseWeaponUsed(string contextTag) => WeaponUsed?.Invoke(contextTag);

        public static void RaiseLeftClicked() => LeftClicked?.Invoke();
        public static void RaiseRightClicked() => RightClicked?.Invoke();

        public static void RaiseSkillCastRequested(Skill skill) => SkillCastRequested?.Invoke(skill);

        public static void RaisePlayerDied() => PlayerDied?.Invoke();

        public static void RaiseSkillDiscovered(string name) => SkillDiscovered?.Invoke(name);

        public static void RaiseMonsterKilled(GameObject monster) => MonsterKilled?.Invoke(monster);
        public static void RaiseSampleCollected(GameObject sample) => SampleCollected?.Invoke(sample);
        public static void RaiseMarkerSurveyed(GameObject marker) => MarkerSurveyed?.Invoke(marker);
    }
}
