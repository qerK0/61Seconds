using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Nodes;

namespace SixtyOne.Tests
{
    /// <summary>Two-layer loop: micro rewind (last node) vs macro rewind (floor checkpoint), plus what persists.</summary>
    public class RewindTargetTests
    {
        private TestData data;
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            session = new GameSession();

            // Checkpoint: empty bag, no flags, family unplaced.
            session.TakeCheckpoint("f4.prep.salon");

            // Later on the floor: collect gear, close gas.
            session.State.Inventory.TryAdd("wrench");
            session.State.Flags.Set(FlagNames.GasOff, true);
            session.State.GetMember(MemberType.Wife).AnchorId = "lr_table";

            // Later decision node.
            session.TakeSnapshot("act1.n04_door");
            session.State.Inventory.TryAdd("tape");
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        [Test]
        public void MicroRewindGoesToLastNodeAndKeepsPlacements()
        {
            session.Die(new DeathInfo { TextKey = "death.x", RewindTarget = RewindTarget.LastNode });
            Assert.AreEqual("act1.n04_door", session.Rewind());
            Assert.IsFalse(session.LastRewindWasMacro);
            CollectionAssert.AreEqual(new[] { "wrench" }, session.State.Inventory.Items);
            Assert.IsTrue(session.State.Flags.Get(FlagNames.GasOff));
            Assert.AreEqual("lr_table", session.State.GetMember(MemberType.Wife).AnchorId);
        }

        [Test]
        public void MacroRewindRestoresCheckpoint()
        {
            session.State.CollectedTips.Add("tip.wardrobe");
            session.State.RevealedAnchors.Add("lr_wardrobe");
            session.Die(new DeathInfo { TextKey = "death.missing.crowbar", RewindTarget = RewindTarget.Checkpoint, MissingItemId = "crowbar", TipId = "tip.emergency_bag" });

            Assert.AreEqual("f4.prep.salon", session.Rewind());
            Assert.IsTrue(session.LastRewindWasMacro);
            Assert.AreEqual(0, session.State.Inventory.Count, "empty bag");
            Assert.IsFalse(session.State.Flags.Get(FlagNames.GasOff), "flags reset");
            Assert.IsNull(session.State.GetMember(MemberType.Wife).AnchorId, "family back at start positions");
            Assert.AreEqual(MemberHealth.Healthy, session.State.GetMember(MemberType.Player).Health);

            Assert.AreEqual(1, session.State.LoopCount);
            Assert.IsTrue(session.State.CollectedTips.Contains("tip.wardrobe"));
            Assert.IsTrue(session.State.CollectedTips.Contains("tip.emergency_bag"));
            Assert.IsTrue(session.State.RevealedAnchors.Contains("lr_wardrobe"));
        }

        [Test]
        public void MacroWithoutCheckpointFallsBackToLatest()
        {
            var plain = new GameSession();
            plain.TakeSnapshot("n1");
            plain.Die(new DeathInfo { RewindTarget = RewindTarget.Checkpoint });
            Assert.AreEqual("n1", plain.Rewind());
            Assert.IsFalse(plain.LastRewindWasMacro);
        }

        [Test]
        public void KnowledgePersistsAcrossMacroAndMicroRewinds()
        {
            session.Die(new DeathInfo { RewindTarget = RewindTarget.Checkpoint, MissingItemId = "crowbar" });
            Assert.IsTrue(session.State.Knowledge.Contains("crowbar"));
            session.Rewind();
            Assert.IsTrue(session.State.Knowledge.Contains("crowbar"), "survives macro rewind");

            session.TakeSnapshot("act1.n02_place");
            session.Die(new DeathInfo { RewindTarget = RewindTarget.LastNode });
            session.Rewind();
            Assert.IsTrue(session.State.Knowledge.Contains("crowbar"), "survives micro rewind");
            Assert.AreEqual(2, session.State.LoopCount);
            Assert.IsTrue(session.State.Clone().Knowledge.Contains("crowbar"), "clone copies knowledge");
        }

        [Test]
        public void LockedFatalChoiceIsClickableAndBuildsMacroDeath()
        {
            Item crowbar = data.Item("crowbar");
            Tip tip = data.Tip("tip.emergency_bag");
            SceneNode door = data.Node("act1.n04_door");
            door.AddChoice(new Choice("act1.n04.c_crowbar")
                .FatalWithout(crowbar, "act1.n04.c_crowbar.locked", "death.missing.crowbar")
                .WithDeath(new DeathData("death.missing.crowbar", tip)));

            var state = new GameState();
            var views = ChoiceView.Build(door, state);
            Assert.AreEqual(1, views.Count);
            Assert.IsTrue(views[0].Locked);
            Assert.IsTrue(views[0].Fatal);
            Assert.IsTrue(views[0].Clickable);

            DeathInfo info = door.Choices[0].BuildMissingItemDeath();
            Assert.AreEqual(RewindTarget.Checkpoint, info.RewindTarget);
            Assert.AreEqual("crowbar", info.MissingItemId);
            Assert.AreEqual("death.missing.crowbar", info.TextKey);
            Assert.AreEqual("tip.emergency_bag", info.TipId);

            state.Inventory.TryAdd("crowbar");
            views = ChoiceView.Build(door, state);
            Assert.IsFalse(views[0].Locked);
            Assert.IsFalse(views[0].Fatal);
        }

        [Test]
        public void DeathDataCarriesRewindTarget()
        {
            var death = new DeathData("death.x", data.Tip("tip.x"), target: RewindTarget.Checkpoint);
            Assert.AreEqual(RewindTarget.Checkpoint, death.ToInfo().RewindTarget);
            Assert.AreEqual(RewindTarget.LastNode, new DeathData("death.y", null).ToInfo().RewindTarget);
        }
    }
}
