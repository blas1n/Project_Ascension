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

        // Raw attack-button inputs (left/right click) — feed the command combo
        // recognizer, which needs the button press, not the weapon-fire execution.
        public static event Action LeftClicked;
        public static event Action RightClicked;

        // A discovered weapon asks the skill executor to run its skill (routing event;
        // the weapon lives in the Equipment assembly, the executor in Game).
        public static event Action<Skill> SkillCastRequested;

        // World facts (argument is the subject GameObject).
        public static event Action<GameObject> MonsterKilled;
        public static event Action<GameObject> SampleCollected;
        public static event Action<GameObject> MarkerSurveyed;

        public static void RaiseJumped() => Jumped?.Invoke();
        public static void RaiseDodged() => Dodged?.Invoke();
        public static void RaiseAttacked(bool isMelee) => Attacked?.Invoke(isMelee);
        public static void RaiseChargedAttacked() => ChargedAttacked?.Invoke();

        public static void RaiseLeftClicked() => LeftClicked?.Invoke();
        public static void RaiseRightClicked() => RightClicked?.Invoke();

        public static void RaiseSkillCastRequested(Skill skill) => SkillCastRequested?.Invoke(skill);

        public static void RaiseMonsterKilled(GameObject monster) => MonsterKilled?.Invoke(monster);
        public static void RaiseSampleCollected(GameObject sample) => SampleCollected?.Invoke(sample);
        public static void RaiseMarkerSurveyed(GameObject marker) => MarkerSurveyed?.Invoke(marker);
    }
}
