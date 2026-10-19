using SixtyOne.Core;
using SixtyOne.Iso;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Click-and-run (IsoPicker router). In exploration a floor click runs the player there along the room's grid
    /// path (around debris; a blocked cell runs to the nearest reachable free cell); a click on a door / pickup /
    /// NPC runs to its approach cell (or the nearest free neighbour) and then uses it. Outside exploration (placement, dialogue, QTE) clicks go straight to their targets.
    /// The panic input delay is applied by IsoPicker before Route is called.
    /// </summary>
    public sealed class PlayerMover : MonoBehaviour, IWorldClickRouter
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("The player's motor.")]
        [SerializeField] private CharacterMotor player;

        [Tooltip("Current room (floor clamp).")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Run speed and floor margin.")]
        [SerializeField] private MovementConfig config;

        // Config speeds are tuned at world scale 1; rooms scaled by RoomVisualConfig run proportionally faster.
        private float Speed => (config != null ? config.RunSpeed : 2.4f) * (rooms != null && rooms.Current != null ? rooms.Current.Scale : 1f);
        private float Margin => config != null ? config.FloorMargin : 0.25f;

        /// <summary>True while the player may run (exploration, alive, no transition).</summary>
        public bool CanMove
        {
            get
            {
                GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
                return gm != null && !gm.IsDead && ExplorationGate.Allowed && !InputGate.IsBlocked && !CrouchGate.Frozen && player != null && rooms != null && rooms.Current != null;
            }
        }

        public bool AcceptsFloorClicks => CanMove;

        private void Start()
        {
            if (player != null) player.ReferenceSpeed = config != null ? config.RunSpeed : 2.4f;
        }

        private void Update()
        {
            // A dialogue or death took over: stop our own run where we are (placement runs belong to placement).
            if (player != null && player.IsMoving && player.Owner == (object)this && !CanMove) player.Stop();
        }

        public bool Route(IClickable target, Vector3 worldPoint)
        {
            if (!CanMove) return false;
            if (target is IApproachable approachable)
            {
                Room room = rooms.Current;
                Vector3 point = room.ApproachTarget(approachable.ApproachPoint, Margin);
                player.RunPath(room.PlanPath(player.transform.position, point, Margin), Speed, () =>
                {
                    if (CanMove && target.IsClickable) target.OnClicked();
                }, this);
                return true;
            }

            // Members, anchors and other clickables keep their direct click.
            if (target != null) return false;
            player.RunPath(rooms.Current.PlanPath(player.transform.position, worldPoint, Margin), Speed, null, this);
            return true;
        }
    }
}
