using System.Collections.Generic;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Dialogue
{
    /// <summary>
    /// The scripted robbery on the F2 stairs (one per reply): the man's reply line(s), then the take outcomes run
    /// (TakeItems: all the food, else one water; key items never; RobbedAtExit), one toast per item taken, a
    /// closing line (or a bitter line when he found nothing), his sprite runs off down the stairs, and the player
    /// continues to the next node (the street) automatically. No QTE, no death, no fight. Played by RobberController.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Robber Node", fileName = "robber_new")]
    public sealed class RobberNode : DialogueNode
    {
        [Tooltip("Run after the reply lines: TakeItems (+ RobbedAtExit).")]
        [SerializeField] private List<Outcome> takeOutcomes = new List<Outcome>();

        [Tooltip("Speaker of the closing / bitter line.")]
        [SerializeField] private TextKey speakerKey;

        [Tooltip("Closing line when he took something.")]
        [SerializeField] private TextKey takenLineKey;

        [Tooltip("Bitter line when the bag had nothing for him.")]
        [SerializeField] private TextKey nothingLineKey;

        [Tooltip("Toast per item taken ({0} = item name).")]
        [SerializeField] private TextKey takenItemKey;

        [Tooltip("Name of the man's hotspot object in the room (Hotspot_<name>): it runs off down the stairs.")]
        [SerializeField] private string actorName = "Robber";

        [Tooltip("Where he runs, in room grid cells (the stairs down).")]
        [SerializeField] private Vector2 runOffGrid = new Vector2(1.5f, 4.6f);

        [Tooltip("Node after he has gone (the street).")]
        [SerializeField] private SceneNode nextNode;

        public IReadOnlyList<Outcome> TakeOutcomes => takeOutcomes;
        public TextKey SpeakerKey => speakerKey;
        public TextKey TakenLineKey => takenLineKey;
        public TextKey NothingLineKey => nothingLineKey;
        public TextKey TakenItemKey => takenItemKey;
        public string ActorName => actorName;
        public Vector2 RunOffGrid => runOffGrid;
        public SceneNode NextNode => nextNode;

        /// <summary>The closing line for what was taken: the bitter line when nothing was.</summary>
        public TextKey ClosingLine(int takenCount) => takenCount > 0 ? takenLineKey : nothingLineKey;

        /// <summary>Authoring helper (call after Configure and SetLines).</summary>
        public void ConfigureRobbery(IEnumerable<Outcome> take, TextKey speaker, TextKey takenLine, TextKey nothingLine, TextKey takenItem, string actor,
            Vector2 runOff, SceneNode next)
        {
            takeOutcomes = new List<Outcome>(take);
            speakerKey = speaker;
            takenLineKey = takenLine;
            nothingLineKey = nothingLine;
            takenItemKey = takenItem;
            actorName = actor;
            runOffGrid = runOff;
            nextNode = next;
        }
    }
}
