using System;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
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
        /// <summary>Not shown.</summary>
        Hide,
        /// <summary>Shown greyed out with a lock and a reason; not clickable.</summary>
        ShowLocked,
        /// <summary>Shown locked but clickable: kills with the missing-item text and macro-rewinds to prep start.</summary>
        ShowLockedFatal
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

        [Tooltip("ShowLockedFatal: the item the player should have brought (added to the KnowledgeSet).")]
        [SerializeField] private Item missingItem;

        [Tooltip("ShowLockedFatal: death line, e.g. 'Keşke yanıma alsaydım...'.")]
        [SerializeField] private TextKey missingItemTextKey;

        [Tooltip("ShowLockedFatal: where the missing-item death rewinds to (Checkpoint = floor start).")]
        [SerializeField] private RewindTarget missingItemRewind = RewindTarget.Checkpoint;

        [Tooltip("Seconds subtracted from the building timer when picked.")]
        [Min(0f)]
        [SerializeField] private float timeCost;

        [Tooltip("Stamina spent when picked.")]
        [Min(0f)]
        [SerializeField] private float staminaCost;

        [Tooltip("Heavy action (climbing, forcing, crafting): locked when stamina is 0.")]
        [SerializeField] private bool heavy;

        [Tooltip("A move between rooms/floors: injured family members and exhaustion make it slower.")]
        [SerializeField] private bool isMove;

        [Tooltip("Dialogue nodes: offered while lines are still playing (e.g. 'Kısa kes').")]
        [SerializeField] private bool duringDialogue;

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
        public Item MissingItem => missingItem;
        public TextKey MissingItemTextKey => missingItemTextKey;
        public RewindTarget MissingItemRewind => missingItemRewind;
        public float TimeCost => timeCost;
        public float StaminaCost => staminaCost;
        public bool Heavy => heavy;
        public bool IsMove => isMove;
        public bool DuringDialogue => duringDialogue;
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

        /// <summary>
        /// Makes the choice ShowLockedFatal: requires the item; clicking it without the item kills with
        /// the missing-item line and rewinds to the floor checkpoint (change with FatalRewind).
        /// </summary>
        public Choice FatalWithout(Item item, TextKey lockedReason, TextKey missingText)
        {
            conditions.Add(Condition.HasItem(item));
            failBehaviour = FailBehaviour.ShowLockedFatal;
            lockedReasonKey = lockedReason;
            missingItem = item;
            missingItemTextKey = missingText;
            return this;
        }

        /// <summary>Sets the ShowLockedFatal fallback item and death line without adding requirements.</summary>
        public Choice WithMissingText(Item fallbackItem, TextKey missingText)
        {
            missingItem = fallbackItem;
            missingItemTextKey = missingText;
            return this;
        }

        /// <summary>
        /// Runtime death for clicking a ShowLockedFatal choice without its item. With a state, the missing item
        /// is the first unmet HasItem/HasAnyItem requirement (so the death card names what is really missing);
        /// otherwise the authored missingItem.
        /// </summary>
        public DeathInfo BuildMissingItemDeath(GameState state = null)
        {
            Item missing = state != null ? FirstMissingItem(state) : null;
            if (missing == null) missing = missingItem;
            return new DeathInfo
            {
                Victim = MemberType.Player,
                TextKey = missingItemTextKey.Key,
                TipId = death != null && death.Tip != null ? death.Tip.Id : null,
                Cue = death != null ? death.Cue : null,
                MissingItemId = missing != null ? missing.Id : null,
                RewindTarget = missingItemRewind
            };
        }

        /// <summary>First item requirement that the state does not meet, or null.</summary>
        public Item FirstMissingItem(GameState state)
        {
            foreach (Condition condition in conditions)
            {
                if (condition == null || ConditionEvaluator.Evaluate(condition, state)) continue;
                if (condition.Type == ConditionType.HasItem) return condition.Item;
                if (condition.Type == ConditionType.HasAnyItem && condition.AnyItems.Count > 0) return condition.AnyItems[0];
            }

            return null;
        }

        /// <summary>Adds an outcome.</summary>
        public Choice Then(Outcome outcome) { outcomes.Add(outcome); return this; }

        /// <summary>Adds a Death outcome with its text and tip.</summary>
        public Choice Kills(DeathData deathData) { death = deathData; outcomes.Add(Outcome.Death()); return this; }

        /// <summary>Time (building timer), stamina, heavy and move settings.</summary>
        public Choice Costs(float seconds, float stamina = 0f, bool isHeavy = false, bool move = false)
        {
            timeCost = seconds;
            staminaCost = stamina;
            heavy = isHeavy;
            isMove = move;
            return this;
        }

        /// <summary>Shorthand for a room/floor move costing seconds.</summary>
        public Choice Move(float seconds) => Costs(seconds, 0f, false, true);

        /// <summary>Offer this choice while dialogue lines are still playing.</summary>
        public Choice AsDuringDialogue() { duringDialogue = true; return this; }

        /// <summary>Where a ShowLockedFatal death rewinds to.</summary>
        public Choice FatalRewind(RewindTarget target) { missingItemRewind = target; return this; }

        /// <summary>Sets death data (tip/cue) without adding a Death outcome (used by ShowLockedFatal).</summary>
        public Choice WithDeath(DeathData deathData) { death = deathData; return this; }

        /// <summary>Marks this as the hidden timeout choice.</summary>
        public Choice AsTimeoutOnly() { timeoutOnly = true; return this; }

        /// <summary>Sets the pick sound.</summary>
        public Choice WithCue(AudioCue audioCue) { cue = audioCue; return this; }
    }
}
