using System;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>Normal choices continue the story; Trap choices are "what NOT to do" and kill.</summary>
    public enum ChoiceType
    {
        Normal,
        Trap
    }

    /// <summary>What to show when a choice's conditions fail.</summary>
    public enum FailBehaviour
    {
        Hide,
        ShowLocked
    }

    /// <summary>One button on a decision node.</summary>
    [Serializable]
    public sealed class Choice
    {
        [Tooltip("Button label.")]
        [SerializeField] private TextKey labelKey;

        [Tooltip("Trap = a do-not-do action that kills.")]
        [SerializeField] private ChoiceType type;

        [Tooltip("If any of these fail the choice is hidden, regardless of fail behaviour (e.g. GasOff already true).")]
        [SerializeField] private List<Condition> showIf = new List<Condition>();

        [Tooltip("Requirements to pick the choice.")]
        [SerializeField] private List<Condition> conditions = new List<Condition>();

        [Tooltip("Hide or show as locked when conditions fail.")]
        [SerializeField] private FailBehaviour failBehaviour = FailBehaviour.Hide;

        [Tooltip("Reason shown on a locked choice. Empty = generic 'Locked'.")]
        [SerializeField] private TextKey lockedReasonKey;

        [Tooltip("Never shown; only fired when the node timer runs out.")]
        [SerializeField] private bool timeoutOnly;

        [Tooltip("Effects in order.")]
        [SerializeField] private List<Outcome> outcomes = new List<Outcome>();

        [Tooltip("Death text/tip used when this choice kills.")]
        [SerializeField] private DeathData death = new DeathData();

        [Tooltip("Optional sound when picked.")]
        [SerializeField] private AudioCue cue;

        public TextKey LabelKey => labelKey;
        public ChoiceType Type => type;
        public IReadOnlyList<Condition> ShowIf => showIf;
        public IReadOnlyList<Condition> Conditions => conditions;
        public FailBehaviour FailBehaviour => failBehaviour;
        public TextKey LockedReasonKey => lockedReasonKey;
        public bool TimeoutOnly => timeoutOnly;
        public IReadOnlyList<Outcome> Outcomes => outcomes;
        public DeathData Death => death;
        public AudioCue Cue => cue;

        public Choice()
        {
        }

        public Choice(TextKey labelKey, ChoiceType type = ChoiceType.Normal)
        {
            this.labelKey = labelKey;
            this.type = type;
        }

        /// <summary>Adds a visibility condition.</summary>
        public Choice ShowWhen(Condition condition) { showIf.Add(condition); return this; }

        /// <summary>Adds a requirement.</summary>
        public Choice Require(Condition condition) { conditions.Add(condition); return this; }

        /// <summary>Sets the fail behaviour and locked reason.</summary>
        public Choice WhenLocked(FailBehaviour behaviour, TextKey reason = default) { failBehaviour = behaviour; lockedReasonKey = reason; return this; }

        /// <summary>Adds an outcome.</summary>
        public Choice Then(Outcome outcome) { outcomes.Add(outcome); return this; }

        /// <summary>Adds a Death outcome with its text and tip.</summary>
        public Choice Kills(DeathData deathData) { death = deathData; outcomes.Add(Outcome.Death()); return this; }

        /// <summary>Marks this as the hidden timeout choice.</summary>
        public Choice AsTimeoutOnly() { timeoutOnly = true; return this; }

        /// <summary>Sets the pick sound.</summary>
        public Choice WithCue(AudioCue audioCue) { cue = audioCue; return this; }
    }
}
