using System;
using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>All mutable game state. Plain C#, owned by GameManager and deep-copied by Snapshot.</summary>
    public sealed class GameState
    {
        private readonly Dictionary<MemberType, FamilyMemberState> members = new Dictionary<MemberType, FamilyMemberState>();

        /// <summary>The bag (8 slots by default; key items live in KeyItems instead).</summary>
        public Inventory Inventory { get; private set; } = new Inventory();

        /// <summary>Key items (flashlight, whistle): no bag slot, cannot be dropped. Restored by snapshots.</summary>
        public KeyItemState KeyItems { get; private set; } = new KeyItemState();

        /// <summary>Items dropped per room. Restored by snapshots.</summary>
        public RoomDropLedger Dropped { get; private set; } = new RoomDropLedger();

        /// <summary>Story flags.</summary>
        public Flags Flags { get; private set; } = new Flags();

        /// <summary>Loop counter. Persists across rewinds.</summary>
        public LoopCounter Loop { get; private set; } = new LoopCounter();

        public int LoopCount => Loop.Count;

        /// <summary>Id of the node currently being played.</summary>
        public string CurrentNodeId { get; set; }

        /// <summary>Room id of the current node (where dropped items land).</summary>
        public string CurrentRoomId { get; set; }

        /// <summary>Current act (1-3).</summary>
        public int Act { get; set; } = 1;

        /// <summary>Tip ids collected through deaths. Persists across rewinds.</summary>
        public HashSet<string> CollectedTips { get; private set; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Anchor ids whose label is revealed. Persists across rewinds.</summary>
        public HashSet<string> RevealedAnchors { get; private set; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Seconds left until the building collapses. Restored by snapshots.</summary>
        public float BuildingTime { get; set; }

        /// <summary>Seconds left on the pre-quake prep countdown (Floor 4 loot phase). Restored by snapshots.</summary>
        public float PrepTime { get; set; }

        /// <summary>False during the prep phase; the building timer only runs once the quake hits. Restored by snapshots.</summary>
        public bool BuildingTimerStarted { get; set; } = true;

        /// <summary>Stamina (0..LevelConfig.StaminaMax). Restored by snapshots.</summary>
        public float Stamina { get; set; } = 100f;

        /// <summary>Gas poisoning 0..1 (1 = death). Restored by snapshots.</summary>
        public float Poison { get; set; }

        /// <summary>Hunger/thirst exhaustion 0..1 (rises at 0 stamina; 1 = death). Restored by snapshots.</summary>
        public float Exhaustion { get; set; }

        /// <summary>Quakes so far: 0 before the quake, opening quake 1, F3 aftershock 2, F2 aftershock 3, street 4. Restored by snapshots.</summary>
        public int QuakeCount { get; set; }

        /// <summary>Checkpoint seed for debris and wall cracks: the same checkpoint always gives the same layout. Restored by snapshots.</summary>
        public int DebrisSeed { get; set; } = 61;

        /// <summary>Items left in searched furniture (loot containers). Restored by snapshots.</summary>
        public ContainerLedger Containers { get; private set; } = new ContainerLedger();

        /// <summary>Items borrowed from neighbours' flats. Restored by snapshots (reset by checkpoint rewinds).</summary>
        public BorrowLedger Borrowed { get; private set; } = new BorrowLedger();

        /// <summary>Items the player died for lacking. Persists across every rewind.</summary>
        public KnowledgeSet Knowledge { get; private set; } = new KnowledgeSet();

        /// <summary>All family members.</summary>
        public IEnumerable<FamilyMemberState> Members => members.Values;

        public GameState()
        {
            foreach (MemberType type in (MemberType[])Enum.GetValues(typeof(MemberType)))
            {
                members[type] = new FamilyMemberState(type);
            }
        }

        /// <summary>Returns the state of one family member.</summary>
        public FamilyMemberState GetMember(MemberType type)
        {
            return members[type];
        }

        /// <summary>Deep copy of everything.</summary>
        public GameState Clone()
        {
            var copy = new GameState
            {
                Inventory = Inventory.Clone(),
                KeyItems = KeyItems.Clone(),
                Dropped = Dropped.Clone(),
                CurrentRoomId = CurrentRoomId,
                Flags = Flags.Clone(),
                Loop = new LoopCounter(Loop.Count),
                CurrentNodeId = CurrentNodeId,
                Act = Act,
                BuildingTime = BuildingTime,
                PrepTime = PrepTime,
                BuildingTimerStarted = BuildingTimerStarted,
                Stamina = Stamina,
                Poison = Poison,
                Exhaustion = Exhaustion,
                QuakeCount = QuakeCount,
                DebrisSeed = DebrisSeed,
                Containers = Containers.Clone(),
                Borrowed = Borrowed.Clone(),
                CollectedTips = new HashSet<string>(CollectedTips, StringComparer.Ordinal),
                RevealedAnchors = new HashSet<string>(RevealedAnchors, StringComparer.Ordinal),
                Knowledge = Knowledge.Clone()
            };

            foreach (KeyValuePair<MemberType, FamilyMemberState> pair in members)
            {
                copy.members[pair.Key] = pair.Value.Clone();
            }

            return copy;
        }
    }
}
