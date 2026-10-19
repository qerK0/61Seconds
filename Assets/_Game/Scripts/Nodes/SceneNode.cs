using System.Collections.Generic;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>A decision point: text, room, optional timer, choices, on-enter effects and bomb checks.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Scene Node", fileName = "node_new")]
    public class SceneNode : ScriptableObject
    {
        [Tooltip("Unique id (snapshots and rewinds use it).")]
        [SerializeField] private string id;

        [Tooltip("Title text.")]
        [SerializeField] private TextKey titleKey;

        [Tooltip("Body text.")]
        [SerializeField] private TextKey bodyKey;

        [Tooltip("Room shown while this node plays (Room.RoomId).")]
        [SerializeField] private string roomId;

        [Tooltip("Act / quake number (1 = 6.0 floor 6, 2 = 7.0 floor 4, 3 = 8.0 floor 2).")]
        [Range(1, 3)]
        [SerializeField] private int act = 1;

        [Tooltip("Seconds before the timeout choice fires. 0 = untimed.")]
        [SerializeField] private float timerSeconds;

        [Tooltip("Index of the choice fired on timeout. -1 = first Trap choice.")]
        [SerializeField] private int timeoutChoiceIndex = -1;

        [Tooltip("Floor checkpoint: its entry snapshot is the macro rewind target (incl. the building timer).")]
        [SerializeField] private bool isCheckpoint;

        [Tooltip("Show the AFAD alert popup when entering.")]
        [SerializeField] private bool afadAlertOnEnter;

        [Tooltip("Choices offered.")]
        [SerializeField] private List<Choice> choices = new List<Choice>();

        [Tooltip("Applied once when first entered (not on rewind).")]
        [SerializeField] private List<Outcome> onEnter = new List<Outcome>();

        [Tooltip("Checked on enter, before the snapshot. Can kill or injure.")]
        [SerializeField] private List<BombCheck> bombChecks = new List<BombCheck>();

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public TextKey TitleKey => titleKey;
        public TextKey BodyKey => bodyKey;
        public string RoomId => roomId;
        public int Act => act;
        public float TimerSeconds => timerSeconds;
        public int TimeoutChoiceIndex => timeoutChoiceIndex;
        public bool AfadAlertOnEnter => afadAlertOnEnter;
        public IReadOnlyList<Choice> Choices => choices;
        public IReadOnlyList<Outcome> OnEnter => onEnter;
        public IReadOnlyList<BombCheck> BombChecks => bombChecks;

        /// <summary>Magnitude shown by the alert popup when entering (AfadAlertOnEnter).</summary>
        public virtual float AlertMagnitude => SixtyOne.Core.QuakeInfo.MagnitudeForAct(act);

        /// <summary>True for floor checkpoints (macro rewind target).</summary>
        public bool IsCheckpoint => isCheckpoint;

        /// <summary>Choice fired when the timer runs out: the designated one, else the first Trap.</summary>
        public Choice GetTimeoutChoice()
        {
            if (timeoutChoiceIndex >= 0 && timeoutChoiceIndex < choices.Count)
            {
                return choices[timeoutChoiceIndex];
            }

            foreach (Choice choice in choices)
            {
                if (choice != null && choice.Type == ChoiceType.Trap)
                {
                    return choice;
                }
            }

            return null;
        }

        /// <summary>Authoring helper: sets basics and clears lists.</summary>
        public void Configure(string nodeId, TextKey title, TextKey body, string room, int nodeAct, float timer = 0f)
        {
            id = nodeId;
            titleKey = title;
            bodyKey = body;
            roomId = room;
            act = Mathf.Clamp(nodeAct, 1, 3);
            timerSeconds = timer;
            timeoutChoiceIndex = -1;
            afadAlertOnEnter = false;
            isCheckpoint = false;
            choices.Clear();
            onEnter.Clear();
            bombChecks.Clear();
        }

        /// <summary>Adds a choice and returns its index.</summary>
        public int AddChoice(Choice choice)
        {
            choices.Add(choice);
            return choices.Count - 1;
        }

        public void AddOnEnter(Outcome outcome) => onEnter.Add(outcome);
        public void AddBombCheck(BombCheck bomb) => bombChecks.Add(bomb);
        public void SetTimeoutChoice(int index) => timeoutChoiceIndex = index;
        public void SetAfadAlert(bool enabled) => afadAlertOnEnter = enabled;
        public void SetCheckpoint(bool checkpoint) => isCheckpoint = checkpoint;
    }
}
