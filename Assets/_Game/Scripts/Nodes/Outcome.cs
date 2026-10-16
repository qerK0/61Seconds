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
        SetMemberState
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

        [Tooltip("Member for SetMemberState.")]
        [SerializeField] private MemberType member;

        [Tooltip("Health for SetMemberState.")]
        [SerializeField] private MemberHealth health;

        public OutcomeType Type => type;
        public SceneNode TargetNode => targetNode;
        public string Flag => flag;
        public bool FlagValue => flagValue;
        public Item Item => item;
        public MemberType Member => member;
        public MemberHealth Health => health;

        public Outcome()
        {
        }

        public static Outcome GoTo(SceneNode node) => new Outcome { type = OutcomeType.GoToNode, targetNode = node };
        public static Outcome Death() => new Outcome { type = OutcomeType.Death };
        public static Outcome SetFlag(string flag, bool value = true) => new Outcome { type = OutcomeType.SetFlag, flag = flag, flagValue = value };
        public static Outcome AddItem(Item item) => new Outcome { type = OutcomeType.AddItem, item = item };
        public static Outcome RemoveItem(Item item) => new Outcome { type = OutcomeType.RemoveItem, item = item };
        public static Outcome SetMember(MemberType member, MemberHealth health) => new Outcome { type = OutcomeType.SetMemberState, member = member, health = health };
    }
}
