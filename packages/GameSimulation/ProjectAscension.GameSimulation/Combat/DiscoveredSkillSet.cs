using System.Collections.Generic;
using System.Linq;
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The player's discovered skills, organized by how they are wielded: weapons
    /// (synthesized magic — equipped and fired) and commands (techniques — invoked).
    /// Both execute through the same <see cref="GraphSkillResolver"/> (over each skill's
    /// effect graph); the manifestation only decides where the skill is slotted and how
    /// it is triggered.
    /// </summary>
    public sealed class DiscoveredSkillSet
    {
        private readonly List<DiscoveredSkill> _weapons = new();
        private readonly List<DiscoveredSkill> _commands = new();
        private readonly List<DiscoveredSkill> _passives = new();

        public IReadOnlyList<DiscoveredSkill> Weapons => _weapons;
        public IReadOnlyList<DiscoveredSkill> Commands => _commands;
        public IReadOnlyList<DiscoveredSkill> Passives => _passives;

        /// <summary>Every discovered skill, regardless of manifestation (e.g. for the
        /// knowledge market).</summary>
        public IEnumerable<DiscoveredSkill> All
        {
            get
            {
                foreach (var w in _weapons) yield return w;
                foreach (var c in _commands) yield return c;
                foreach (var p in _passives) yield return p;
            }
        }

        public void Add(DiscoveredSkill skill)
        {
            switch (skill.Manifestation)
            {
                case ManifestationKind.Weapon: _weapons.Add(skill); break;
                case ManifestationKind.Passive: _passives.Add(skill); break;
                default: _commands.Add(skill); break; // Command
            }
        }

        /// <summary>The combined always-on bonuses from every discovered passive — resolved from
        /// each skill's effect graph (ADR 0007; EffectiveGraph is always present).</summary>
        public PassiveEffect AggregatePassive()
        {
            var total = PassiveEffect.None;
            foreach (var passive in _passives)
                total += GraphPassiveResolver.Resolve(passive.EffectiveGraph);
            return total;
        }

        /// <summary>The combined movement capability read off every discovered skill's effect
        /// graph (ADR 0007) — extra air jumps, wall-climb. Graph-driven, not a bespoke field: a
        /// new movement mechanic is a new trigger, no engine change.</summary>
        public MovementCapability AggregateMovement()
            => MovementCapability.From(All.Select(s => s.EffectiveGraph)); // never null (translates legacy)
    }
}
