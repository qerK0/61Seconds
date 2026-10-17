using SixtyOne.Core;
using SixtyOne.Iso;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Click-and-run (IsoPicker router). In exploration a floor click runs the player there (straight line,
    /// clamped onto the room's floor diamond); a click on a door / pickup / NPC runs to its approach point and
    /// then uses it. Outside exploration (placement, dialogue, QTE) clicks go straight to their targets.
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

        private float Speed => config != null ? config.RunSpeed : 2.4f;
        private float Margin => config != null ? config.FloorMargin : 0.25f;

        /// <summary>True while the player may run (exploration, alive, no transition).</summary>
        public bool CanMove
        {
            get
            {
                GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
                return gm != null && !gm.IsDead && ExplorationGate.Allowed && !InputGate.IsBlocked && player != null && rooms != null && rooms.Current != null;
            }
        }

        public bool AcceptsFloorClicks => CanMove;

        private void Start()
        {
            if (player != null) player.ReferenceSpeed = Speed;
        }

        private void Update()
        {
            // A dialogue, placement or death took over: stop where we are.
            if (player != null && player.IsMoving && !CanMove) player.Stop();
        }

        public bool Route(IClickable target, Vector3 worldPoint)
        {
            if (!CanMove) return false;
            if (target is IApproachable approachable)
            {
                Vector3 point = rooms.Current.ClampToFloor(approachable.ApproachPoint, Margin);
                player.RunTo(point, Speed, () =>
                {
                    if (CanMove && target.IsClickable) target.OnClicked();
                });
                return true;
            }

            // Members, anchors and other clickables keep their direct click.
            if (target != null) return false;
            player.RunTo(rooms.Current.ClampToFloor(worldPoint, Margin), Speed);
            return true;
        }
    }
}
