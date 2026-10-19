using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Runs doors and choice hotspots (RoomLinkRules) and hides the choices they take over from the panel.
    /// A door inside a flat switches rooms for MovementConfig.DoorSeconds; any other link plays a node choice
    /// through NodePlayer.Choose, so costs, locks, fatal locks and outcomes behave exactly like the old buttons.
    /// </summary>
    public sealed class RoomNavigator : MonoBehaviour, IChoiceFilter
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Plays the chosen node choice.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Shows rooms and knows their zones.")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Door cost inside a flat.")]
        [SerializeField] private MovementConfig config;

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;

        /// <summary>True while doors and hotspots react (exploration, alive, no transition).</summary>
        public bool CanUseLinks
        {
            get
            {
                GameManager gm = Gm;
                return gm != null && !gm.IsDead && ExplorationGate.Allowed && !InputGate.IsBlocked && rooms != null && rooms.Current != null;
            }
        }

        /// <summary>What using the link would do right now.</summary>
        public LinkDecision Decide(RoomLink link)
        {
            GameManager gm = Gm;
            if (link == null || gm == null || nodePlayer == null || rooms == null || rooms.Current == null) return new LinkDecision(LinkAction.Unavailable);
            switch (link)
            {
                case Door door:
                    return RoomLinkRules.DecideDoor(nodePlayer.Current, gm.State, rooms.Current.RoomId, door.TargetRoomId, rooms.ZoneOf);
                case ChoiceHotspot hotspot:
                    // The first of its keys the current node offers (a valve plays the prep or the post-quake choice).
                    foreach (string key in hotspot.ChoiceKeys)
                    {
                        LinkDecision decision = RoomLinkRules.DecideHotspot(nodePlayer.Current, gm.State, key);
                        if (decision.Action != LinkAction.Unavailable) return decision;
                    }

                    return new LinkDecision(LinkAction.Unavailable);
                default:
                    return new LinkDecision(LinkAction.Unavailable);
            }
        }

        public void Use(RoomLink link)
        {
            if (!CanUseLinks) return;
            LinkDecision decision = Decide(link);
            var door = link as Door;
            switch (decision.Action)
            {
                case LinkAction.LocalSwitch:
                    ActionCostRouter.SpendTime(config != null ? config.DoorSeconds : 1f, true);
                    if (Gm.IsDead) return;
                    rooms.ShowLocal(door.TargetRoomId, ArrivalOf(door));
                    nodePlayer.RefreshCurrentChoices();
                    break;
                case LinkAction.Choose:
                    if (door != null) rooms.SetPendingArrival(door.TargetRoomId, ArrivalOf(door));
                    nodePlayer.Choose(decision.ChoiceIndex);
                    rooms.ClearPendingArrival();
                    break;
                case LinkAction.Locked:
                    GameEvents.RaiseMessage(decision.ReasonKey);
                    break;
                default:
                    GameEvents.RaiseMessage(door != null ? UiKeys.DoorUnavailable : UiKeys.NotNow);
                    break;
            }
        }

        /// <summary>Choices a door or hotspot of the current flat/corridor already offers.</summary>
        public bool Hides(SceneNode node, Choice choice)
        {
            // The room being entered (GameState updates at once; rooms.Current only at the transition midpoint).
            Room shown = ShownRoom();
            if (shown == null || choice == null) return false;
            string zone = shown.ZoneId;
            foreach (Room room in rooms.RoomsInZone(zone))
            {
                foreach (RoomLink link in room.Links)
                {
                    if (link is Door door && RoomLinkRules.Handles(choice, zone, rooms.ZoneOf(door.TargetRoomId), null, rooms.ZoneOf)) return true;
                    if (link is ChoiceHotspot hotspot)
                    {
                        foreach (string key in hotspot.ChoiceKeys)
                        {
                            if (RoomLinkRules.Handles(choice, zone, null, key, rooms.ZoneOf)) return true;
                        }
                    }
                }
            }

            return false;
        }

        private Room ShownRoom()
        {
            GameManager gm = Gm;
            Room room = gm != null && rooms != null ? rooms.FindRoom(gm.State.CurrentRoomId) : null;
            return room != null ? room : rooms != null ? rooms.Current : null;
        }

        private static Vector3? ArrivalOf(Door door)
        {
            return door != null && door.ArrivalPoint != null ? door.ArrivalPoint.position : (Vector3?)null;
        }
    }
}
