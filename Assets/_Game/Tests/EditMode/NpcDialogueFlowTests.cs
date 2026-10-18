using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Editor;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using UnityEditor;

namespace SixtyOne.Tests
{
    /// <summary>
    /// The generated NPC flows end to end on the real node assets: Sevim on the way out of Flat 11 (door → dialogue →
    /// whistle), her corridor hotspot, Hasan's gift, Cemil's trade and the robber on the F2 stairs.
    /// </summary>
    public class NpcDialogueFlowTests
    {
        private static SceneNode Node(string floor, string id)
        {
            var node = AssetDatabase.LoadAssetAtPath<SceneNode>($"{BuildingLevelsGenerator.NodesDir}/{floor}/{id}.asset");
            if (node == null) Assert.Ignore($"Node {id} not generated (run SixtyOne > Generate Building Levels).");
            return node;
        }

        /// <summary>Every room is its own zone here (Flat 11 hall and the corridor are separate zones).</summary>
        private static string Zone(string roomId) => roomId;

        private static Choice ChoiceByKey(SceneNode node, GameState state, string key)
        {
            foreach (ChoiceView view in ChoiceView.Build(node, state))
            {
                if (view.Choice.LabelKey.Key == key) return view.Choice;
            }

            Assert.Fail($"{node.Id}: no visible choice {key}");
            return null;
        }

        private static SceneNode Target(Choice choice)
        {
            foreach (Outcome o in choice.Outcomes)
            {
                if (o != null && o.Type == OutcomeType.GoToNode) return o.TargetNode;
            }

            return null;
        }

        [Test]
        public void HallDoorWithSevimNotDoneEntersHerDialogueAndThanksGivesTheWhistle()
        {
            SceneNode hall = Node("Floor4", BuildingLevelsGenerator.F4Hall);
            var state = new GameState();
            LinkDecision door = RoomLinkRules.DecideDoor(hall, state, BuildingLevelsGenerator.Flat11Hall, BuildingLevelsGenerator.F4Corridor, Zone);
            Assert.AreEqual(LinkAction.Choose, door.Action);
            Choice exit = hall.Choices[door.ChoiceIndex];
            Assert.AreEqual(BuildingLevelsGenerator.F4Hall + ".c_exit", exit.LabelKey.Key);
            SceneNode sevim = Target(exit);
            Assert.AreEqual(BuildingLevelsGenerator.F4Sevim, sevim.Id);
            Assert.IsInstanceOf<DialogueNode>(sevim);
            Assert.AreEqual(4, ((DialogueNode)sevim).Lines.Count, "her 4 lines");

            OutcomeExecutor.Execute(ChoiceByKey(sevim, state, BuildingLevelsGenerator.F4Sevim + ".c_thanks").Outcomes, state);
            Assert.IsTrue(state.KeyItems.Has("whistle"), "the whistle goes to the key-item pocket");
            Assert.IsFalse(state.Inventory.Has("whistle"), "no bag slot");
            Assert.IsTrue(state.Flags.Get(BuildingLevelsGenerator.KnowsFlat12));
            Assert.IsTrue(state.Flags.Get(BuildingLevelsGenerator.SevimDone));

            door = RoomLinkRules.DecideDoor(hall, state, BuildingLevelsGenerator.Flat11Hall, BuildingLevelsGenerator.F4Corridor, Zone);
            Assert.AreEqual(BuildingLevelsGenerator.F4Hall + ".c_exit_again", hall.Choices[door.ChoiceIndex].LabelKey.Key, "second time out: no Sevim");
        }

        [Test]
        public void HurryingSevimSkipsTheWhistleAndTheInfo()
        {
            var sevim = (DialogueNode)Node("Floor4", BuildingLevelsGenerator.F4Sevim);
            var state = new GameState();
            OutcomeExecutor.Execute(ChoiceByKey(sevim, state, BuildingLevelsGenerator.F4Sevim + ".c_hurry").Outcomes, state);
            Assert.IsFalse(StateOps.HasItem(state, "whistle"));
            Assert.IsFalse(state.Flags.Get(BuildingLevelsGenerator.KnowsFlat12));
            Assert.IsTrue(state.Flags.Get(BuildingLevelsGenerator.SevimDone));
        }

        [Test]
        public void SevimHotspotTalksFirstThenSaysOneLine()
        {
            SceneNode corridor = Node("Floor4", BuildingLevelsGenerator.F4CorridorNode);
            var state = new GameState();
            LinkDecision talk = RoomLinkRules.DecideHotspot(corridor, state, BuildingLevelsGenerator.F4CorridorNode + ".c_sevim_talk");
            Assert.AreEqual(LinkAction.Choose, talk.Action);
            Assert.AreEqual(BuildingLevelsGenerator.F4Sevim, Target(corridor.Choices[talk.ChoiceIndex]).Id);
            Assert.AreEqual(LinkAction.Unavailable, RoomLinkRules.DecideHotspot(corridor, state, BuildingLevelsGenerator.F4CorridorNode + ".c_sevim").Action);

            state.Flags.Set(BuildingLevelsGenerator.SevimDone, true);
            Assert.AreEqual(LinkAction.Unavailable, RoomLinkRules.DecideHotspot(corridor, state, BuildingLevelsGenerator.F4CorridorNode + ".c_sevim_talk").Action);
            LinkDecision line = RoomLinkRules.DecideHotspot(corridor, state, BuildingLevelsGenerator.F4CorridorNode + ".c_sevim");
            Assert.AreEqual(LinkAction.Choose, line.Action);
            Choice c = corridor.Choices[line.ChoiceIndex];
            Assert.IsNull(Target(c), "no node change, just her line");
            Assert.AreEqual(BuildingLevelsGenerator.F4CorridorNode + ".msg_sevim", c.Outcomes[0].MessageKey.Key);
        }

        [Test]
        public void HasanGivesFoodAndWater()
        {
            SceneNode corridor = Node("Floor4", BuildingLevelsGenerator.F4CorridorNode);
            var state = new GameState();
            LinkDecision talk = RoomLinkRules.DecideHotspot(corridor, state, BuildingLevelsGenerator.F4CorridorNode + ".c_talk");
            Assert.AreEqual(LinkAction.Choose, talk.Action);
            SceneNode hasan = Target(corridor.Choices[talk.ChoiceIndex]);
            Assert.AreEqual(BuildingLevelsGenerator.F4Neighbor, hasan.Id);
            OutcomeExecutor.Execute(ChoiceByKey(hasan, state, BuildingLevelsGenerator.F4Neighbor + ".c_thanks").Outcomes, state);
            Assert.IsTrue(state.Inventory.Has("food"));
            Assert.IsTrue(state.Inventory.Has("water"));
            Assert.IsTrue(state.Flags.Get(BuildingLevelsGenerator.GiftDone));
        }

        [Test]
        public void CemilTradesTheFirstMissingNeed()
        {
            SceneNode corridor = Node("Floor3", BuildingLevelsGenerator.F3CorridorNode);
            var state = new GameState();
            LinkDecision talk = RoomLinkRules.DecideHotspot(corridor, state, BuildingLevelsGenerator.F3CorridorNode + ".c_trader");
            Assert.AreEqual(LinkAction.Choose, talk.Action);
            var cemil = (DialogueNode)Target(corridor.Choices[talk.ChoiceIndex]);
            Assert.AreEqual(BuildingLevelsGenerator.F3Trader, cemil.Id);
            Assert.AreEqual(4, cemil.Lines.Count);
            OutcomeExecutor.Execute(ChoiceByKey(cemil, state, BuildingLevelsGenerator.F3Trader + ".c_take").Outcomes, state);
            Assert.IsTrue(StateOps.HasItem(state, "flashlight"), "empty bag: his first need");
            Assert.IsTrue(state.Flags.Get(BuildingLevelsGenerator.TraderDone));
        }

        [Test]
        public void StairsLeadToTheRobber()
        {
            SceneNode exit = Node("Floor2", BuildingLevelsGenerator.F2Exit);
            var state = new GameState();
            SceneNode robber = Target(ChoiceByKey(exit, state, BuildingLevelsGenerator.F2Exit + ".c_out"));
            Assert.AreEqual(BuildingLevelsGenerator.F2Robber, robber.Id);
            Assert.IsInstanceOf<DialogueNode>(robber);
            Assert.Greater(((DialogueNode)robber).Lines.Count, 0);
        }
    }
}
