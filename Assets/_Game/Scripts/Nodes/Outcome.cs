using System;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>What an <see cref="Outcome"/> does.</summary>
    public enum OutcomeType
    {
        GoToNode,
        Death,
        SetFlag,
        AddItem,
        RemoveItem,
        SetMemberState,
        /// <summary>The trader gives the first item the player lacks (TraderProfile).</summary>
        TraderGive,
        /// <summary>Shows a toast message (messageKey).</summary>
        ShowMessage,
        /// <summary>
        /// Someone takes items from the bag: every item in takeItems, else fallbackCount of the fallback item; key
        /// items optionally never (the robber on the F2 stairs). The taken ids come back in OutcomeResult.Taken.
        /// </summary>
        TakeItems,

        /// <summary>
        /// A neighbour gives an item (messageKey = the giver's name key): into the bag or key pocket, else (bag full)
        /// dropped in the current room at the giver's feet. One queued toast per item either way.
        /// </summary>
        GiveItem
    }

    /// <summary>One effect of a choice, bomb check or node entry.</summary>
    [Serializable]
    public sealed class Outcome
    {
        [Tooltip("What happens.")]
        [SerializeField] private OutcomeType type;

        [Tooltip("Target for GoToNode.")]
        [SerializeField] private SceneNode targetNode;

        [Tooltip("Flag name for SetFlag.")]
        [SerializeField] private string flag;

        [Tooltip("Value for SetFlag.")]
        [SerializeField] private bool flagValue = true;

        [Tooltip("Item for AddItem / RemoveItem.")]
        [SerializeField] private Item item;

        [Tooltip("Trader for TraderGive.")]
        [SerializeField] private SixtyOne.Items.TraderProfile trader;

        [Tooltip("Message for ShowMessage.")]
        [SerializeField] private SixtyOne.Text.TextKey messageKey;

        [Tooltip("Member for SetMemberState.")]
        [SerializeField] private MemberType member;

        [Tooltip("Health for SetMemberState.")]
        [SerializeField] private MemberHealth health;

        [Tooltip("TakeItems: every one of these in the bag is taken (e.g. all the food).")]
        [SerializeField] private System.Collections.Generic.List<Item> takeItems = new System.Collections.Generic.List<Item>();

        [Tooltip("TakeItems: taken instead when none of the above was in the bag (e.g. water).")]
        [SerializeField] private Item fallbackItem;

        [Tooltip("TakeItems: how many of the fallback item.")]
        [Min(0)]
        [SerializeField] private int fallbackCount = 1;

        [Tooltip("TakeItems: key items (flashlight, whistle) are never taken.")]
        [SerializeField] private bool excludeKeyItems = true;

        public OutcomeType Type => type;
        public SceneNode TargetNode => targetNode;
        public string Flag => flag;
        public bool FlagValue => flagValue;
        public Item Item => item;
        public MemberType Member => member;
        public SixtyOne.Items.TraderProfile Trader => trader;
        public SixtyOne.Text.TextKey MessageKey => messageKey;
        public MemberHealth Health => health;
        public System.Collections.Generic.IReadOnlyList<Item> TakeItemsList => takeItems;
        public Item FallbackItem => fallbackItem;
        public int FallbackCount => fallbackCount;
        public bool ExcludeKeyItems => excludeKeyItems;

        public Outcome()
        {
        }

        public static Outcome GoTo(SceneNode node) => new Outcome { type = OutcomeType.GoToNode, targetNode = node };
        public static Outcome Death() => new Outcome { type = OutcomeType.Death };
        public static Outcome SetFlag(string flag, bool value = true) => new Outcome { type = OutcomeType.SetFlag, flag = flag, flagValue = value };
        public static Outcome AddItem(Item item) => new Outcome { type = OutcomeType.AddItem, item = item };
        public static Outcome GiveItem(Item item, SixtyOne.Text.TextKey giverName) => new Outcome { type = OutcomeType.GiveItem, item = item, messageKey = giverName };
        public static Outcome RemoveItem(Item item) => new Outcome { type = OutcomeType.RemoveItem, item = item };
        public static Outcome GiveFromTrader(SixtyOne.Items.TraderProfile profile) => new Outcome { type = OutcomeType.TraderGive, trader = profile };
        public static Outcome Message(SixtyOne.Text.TextKey key) => new Outcome { type = OutcomeType.ShowMessage, messageKey = key };
        public static Outcome SetMember(MemberType member, MemberHealth health) => new Outcome { type = OutcomeType.SetMemberState, member = member, health = health };

        /// <summary>Takes every listed item from the bag, else fallbackCount of the fallback; key items optionally never.</summary>
        public static Outcome TakeItems(System.Collections.Generic.IEnumerable<Item> items, Item fallback, int fallbackCount, bool excludeKeyItems = true)
        {
            return new Outcome
            {
                type = OutcomeType.TakeItems,
                takeItems = new System.Collections.Generic.List<Item>(items),
                fallbackItem = fallback,
                fallbackCount = fallbackCount,
                excludeKeyItems = excludeKeyItems
            };
        }
    }
}
