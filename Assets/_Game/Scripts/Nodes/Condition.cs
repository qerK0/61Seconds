using System;
using System.Collections.Generic;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>What a <see cref="Condition"/> checks.</summary>
    public enum ConditionType
    {
        HasItem,
        LacksItem,
        FlagTrue,
        FlagFalse,
        MemberState,
        /// <summary>At least one of the listed items is in the bag.</summary>
        HasAnyItem,
        /// <summary>The player is in this room (sub-rooms of a flat share one node; GameState.CurrentRoomId).</summary>
        InRoom
    }

    /// <summary>A single requirement on the game state (item, flag or member health).</summary>
    [Serializable]
    public sealed class Condition
    {
        [Tooltip("What to check.")]
        [SerializeField] private ConditionType type;

        [Tooltip("Item for HasItem / LacksItem.")]
        [SerializeField] private Item item;

        [Tooltip("Items for HasAnyItem (any one is enough).")]
        [SerializeField] private List<Item> anyItems = new List<Item>();

        [Tooltip("Flag name for FlagTrue / FlagFalse (e.g. GasOff).")]
        [SerializeField] private string flag;

        [Tooltip("Member for MemberState.")]
        [SerializeField] private MemberType member;

        [Tooltip("Required health for MemberState.")]
        [SerializeField] private MemberHealth health;

        [Tooltip("Room id for InRoom.")]
        [SerializeField] private string room;

        public ConditionType Type => type;
        public Item Item => item;
        public string ItemId => item != null ? item.Id : null;
        public string Flag => flag;
        public IReadOnlyList<Item> AnyItems => anyItems;
        public MemberType Member => member;
        public MemberHealth Health => health;
        public string Room => room;

        public Condition()
        {
        }

        public static Condition HasItem(Item item) => new Condition { type = ConditionType.HasItem, item = item };
        public static Condition LacksItem(Item item) => new Condition { type = ConditionType.LacksItem, item = item };
        public static Condition FlagTrue(string flag) => new Condition { type = ConditionType.FlagTrue, flag = flag };
        public static Condition FlagFalse(string flag) => new Condition { type = ConditionType.FlagFalse, flag = flag };
        public static Condition HasAnyItem(params Item[] items) => new Condition { type = ConditionType.HasAnyItem, anyItems = new List<Item>(items) };
        public static Condition MemberIs(MemberType member, MemberHealth health) => new Condition { type = ConditionType.MemberState, member = member, health = health };
        public static Condition InRoom(string roomId) => new Condition { type = ConditionType.InRoom, room = roomId };
    }
}
