using System;
using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>
    /// Checked when a node is entered. If all conditions hold, its outcomes run.
    /// A delayed bomb = an earlier choice sets (or fails to set) a flag + a later node bomb-checks it.
    /// </summary>
    [Serializable]
    public sealed class BombCheck
    {
        [Tooltip("All must hold for the bomb to go off.")]
        [SerializeField] private List<Condition> conditions = new List<Condition>();

        [Tooltip("Effects (Death, SetMemberState injured, flags...).")]
        [SerializeField] private List<Outcome> outcomes = new List<Outcome>();

        [Tooltip("Death text/tip/rewind target if the outcomes include Death.")]
        [SerializeField] private DeathData death = new DeathData();

        public IReadOnlyList<Condition> Conditions => conditions;
        public IReadOnlyList<Outcome> Outcomes => outcomes;
        public DeathData Death => death;

        public BombCheck When(Condition condition) { conditions.Add(condition); return this; }
        public BombCheck Then(Outcome outcome) { outcomes.Add(outcome); return this; }
        public BombCheck Kills(DeathData deathData) { death = deathData; outcomes.Add(Outcome.Death()); return this; }
    }
}
