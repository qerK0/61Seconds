using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Dialogue
{
    /// <summary>
    /// Wires the F2 stairs robbery (generator and tests share it): the man's dialogue offers three choices
    /// (Yalvar / Kısa kes / Ona bağır), each going to its own reply RobberNode; every reply takes the food (else
    /// one water; key items never), sets RobbedAtExit and continues to the street. The choices only change the
    /// flavour: every branch ends the same way.
    /// </summary>
    public static class RobberSetup
    {
        public const string NodeId = "f2.robber";
        public const string SpeakerKey = "npc.robber.name";
        public const string TakenItemKey = SixtyOne.Text.UiKeys.RobbedItem;
        public const string ActorName = "Robber";

        /// <summary>Choice suffixes, in order: Yalvar, Kısa kes, Ona bağır.</summary>
        public static readonly string[] Replies = { "beg", "short", "shout" };

        /// <summary>Reply node id for a choice suffix (f2.robber.beg ...).</summary>
        public static string ReplyId(string reply) => NodeId + "." + reply;

        /// <summary>The take outcomes every reply runs.</summary>
        public static List<Outcome> TakeOutcomes(Item food, Item water)
        {
            return new List<Outcome>
            {
                Outcome.TakeItems(new[] { food }, water, 1, excludeKeyItems: true),
                Outcome.SetFlag(FlagNames.RobbedAtExit)
            };
        }

        /// <summary>Adds the three choices to the robber dialogue and configures each reply node (call after Configure/SetLines).</summary>
        public static void Wire(DialogueNode robber, IReadOnlyList<RobberNode> replies, SceneNode street, Item food, Item water, Vector2 runOffGrid)
        {
            for (int i = 0; i < Replies.Length && i < replies.Count; i++)
            {
                robber.AddChoice(new Choice($"{NodeId}.c_{Replies[i]}").Then(Outcome.GoTo(replies[i])));
                replies[i].ConfigureRobbery(TakeOutcomes(food, water), SpeakerKey, NodeId + ".taken", NodeId + ".empty", TakenItemKey, ActorName,
                    runOffGrid, street);
            }
        }
    }
}
