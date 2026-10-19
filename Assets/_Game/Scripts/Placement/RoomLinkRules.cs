using System;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;

namespace SixtyOne.Placement
{
    /// <summary>What using a door / choice hotspot does right now.</summary>
    public enum LinkAction
    {
        /// <summary>Nothing in the current node leads there (e.g. Flat 12 before you know about it).</summary>
        Unavailable,
        /// <summary>Pick the node choice at ChoiceIndex (its costs, conditions and outcomes apply as usual).</summary>
        Choose,
        /// <summary>The matching choice is shown locked: show ReasonKey.</summary>
        Locked,
        /// <summary>Another room of the same flat (same node): switch rooms without a node change.</summary>
        LocalSwitch
    }

    /// <summary>Result of <see cref="RoomLinkRules"/>.</summary>
    public readonly struct LinkDecision
    {
        public readonly LinkAction Action;
        public readonly int ChoiceIndex;
        public readonly string ReasonKey;

        public LinkDecision(LinkAction action, int choiceIndex = -1, string reasonKey = null)
        {
            Action = action;
            ChoiceIndex = choiceIndex;
            ReasonKey = reasonKey;
        }
    }

    /// <summary>
    /// Pure rules that map world doors and hotspots onto the current node's choices, so moving between rooms
    /// stays data-driven: a door into another flat/corridor plays the node's move choice that leads to that
    /// room's zone; a hotspot plays the choice with its label key. Those choices are hidden from the panel.
    /// </summary>
    public static class RoomLinkRules
    {
        /// <summary>True if the choice goes to a node whose room is in targetZone.</summary>
        public static bool LeadsToZone(Choice choice, string targetZone, Func<string, string> zoneOf)
        {
            if (choice == null || string.IsNullOrEmpty(targetZone)) return false;
            foreach (Outcome outcome in choice.Outcomes)
            {
                if (outcome == null || outcome.Type != OutcomeType.GoToNode || outcome.TargetNode == null) continue;
                if (zoneOf(outcome.TargetNode.RoomId) == targetZone) return true;
            }

            return false;
        }

        /// <summary>What a door into targetRoom does from currentRoom in this node.</summary>
        public static LinkDecision DecideDoor(SceneNode node, GameState state, string currentRoom, string targetRoom, Func<string, string> zoneOf)
        {
            string targetZone = zoneOf(targetRoom);
            if (targetZone == zoneOf(currentRoom)) return new LinkDecision(LinkAction.LocalSwitch);
            return Decide(node, state, c => LeadsToZone(c, targetZone, zoneOf));
        }

        /// <summary>What a hotspot bound to the choice label key does in this node.</summary>
        public static LinkDecision DecideHotspot(SceneNode node, GameState state, string choiceKey)
        {
            if (string.IsNullOrEmpty(choiceKey)) return new LinkDecision(LinkAction.Unavailable);
            return Decide(node, state, c => c.LabelKey.Key == choiceKey);
        }

        /// <summary>True if a door (to doorZone, seen from currentZone) or a hotspot (choiceKey) takes over this choice.</summary>
        public static bool Handles(Choice choice, string currentZone, string doorZone, string choiceKey, Func<string, string> zoneOf)
        {
            if (choice == null) return false;
            if (!string.IsNullOrEmpty(choiceKey)) return choice.LabelKey.Key == choiceKey;
            return doorZone != currentZone && LeadsToZone(choice, doorZone, zoneOf);
        }

        private static LinkDecision Decide(SceneNode node, GameState state, Func<Choice, bool> matches)
        {
            if (node == null || state == null) return new LinkDecision(LinkAction.Unavailable);
            foreach (ChoiceView view in ChoiceView.Build(node, state))
            {
                if (!matches(view.Choice)) continue;
                if (view.Clickable) return new LinkDecision(LinkAction.Choose, view.Index);
                string reason = view.ReasonOverride ?? (view.Choice.LockedReasonKey.IsEmpty ? UiKeys.Locked : view.Choice.LockedReasonKey.Key);
                return new LinkDecision(LinkAction.Locked, view.Index, reason);
            }

            return new LinkDecision(LinkAction.Unavailable);
        }
    }
}
