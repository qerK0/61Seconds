using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Wife / child: in exploration, run after the player to a fixed offset beside them, starting after a short
    /// delay (grid path on the room floor). Injured members are slower. After a room change they appear at the
    /// arrival door and run to their spot (ArriveFrom), also while a dialogue or placement owns the screen.
    /// Placement runs (anchors) belong to PlacementController; this component never stops a run it did not start.
    /// </summary>
    public sealed class FamilyFollower : MonoBehaviour
    {
        private const float SettleDistance = 0.06f;
        private const float ReplanInterval = 0.2f;

        [Tooltip("This member.")]
        [SerializeField] private FamilyMember member;

        [Tooltip("This member's motor.")]
        [SerializeField] private CharacterMotor motor;

        [Tooltip("The player to follow.")]
        [SerializeField] private Transform leader;

        [Tooltip("Current room (floor and paths).")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Speeds, delays and offsets.")]
        [SerializeField] private MovementConfig config;

        private float farTime;
        private float nextReplan;
        private CharacterMotor leaderMotor;
        private bool following;
        private bool arriving;

        /// <summary>True while the arrival run after a room change is in progress.</summary>
        public bool IsArriving => arriving;

        private void Awake()
        {
            if (leader != null) leaderMotor = leader.GetComponent<CharacterMotor>();
        }

        /// <summary>Appears at the entry point (arrival door / descent point) and runs to the follow spot.</summary>
        public void ArriveFrom(Vector3 entry)
        {
            if (motor == null || leader == null || rooms == null || rooms.Current == null) return;
            transform.position = new Vector3(entry.x, entry.y, transform.position.z);
            arriving = true;
            following = true;
            nextReplan = 0f;
            Vector3 spot = Spot();
            motor.RunPath(rooms.Current.PlanPath(transform.position, spot, config != null ? config.FloorMargin : 0.25f), Speed(), OnArrived, this);
        }

        /// <summary>Cancels an arrival run (rewinds snap instead).</summary>
        public void CancelArrival()
        {
            arriving = false;
            if (motor != null && motor.IsMoving && motor.Owner == (object)this) motor.Stop();
        }

        private void OnArrived() => arriving = false;

        private bool Ready
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return gm != null && !gm.IsDead && leader != null && motor != null && rooms != null && rooms.Current != null && config != null;
            }
        }

        private void Update()
        {
            bool ready = Ready;
            if (arriving && (motor == null || motor.Owner != (object)this)) arriving = false;
            bool active = ready && (ExplorationGate.Allowed || arriving);
            if (!active)
            {
                if (motor != null && motor.IsMoving && motor.Owner == (object)this) motor.Stop();
                arriving = false;
                farTime = 0f;
                following = false;
                return;
            }

            // Someone else (placement) is driving this member.
            if (motor.IsMoving && motor.Owner != (object)this) return;

            float scale = rooms.Current.Scale;
            Vector3 spot = Spot();
            float distance = Vector2.Distance(transform.position, spot);
            float speed = Speed();

            if (motor.IsMoving)
            {
                if (!arriving && distance <= SettleDistance * scale)
                {
                    motor.Stop();
                }
                else if (Time.time >= nextReplan)
                {
                    nextReplan = Time.time + ReplanInterval;
                    motor.RetargetPath(rooms.Current.PlanPath(transform.position, spot, config.FloorMargin), speed);
                }

                return;
            }

            // Only exploration starts new follow runs.
            if (!ExplorationGate.Allowed) return;
            if (distance <= config.FollowStartDistance * scale)
            {
                farTime = 0f;
                following = false;
                return;
            }

            // Caught up with a still-running leader: keep going without re-waiting the start delay.
            bool child = member != null && member.Type == MemberType.Child;
            float delay = child ? config.ChildDelay : config.WifeDelay;
            bool leaderRunning = leaderMotor != null && leaderMotor.IsMoving;
            farTime += Time.deltaTime;
            if (farTime < delay && !(following && leaderRunning)) return;
            farTime = 0f;
            following = true;
            nextReplan = Time.time + ReplanInterval;
            motor.RunPath(rooms.Current.PlanPath(transform.position, spot, config.FloorMargin), speed, null, this);
        }

        /// <summary>The follow offset beside the player, on the floor.</summary>
        private Vector3 Spot()
        {
            bool child = member != null && member.Type == MemberType.Child;
            Vector2 offset = config != null ? (child ? config.ChildOffset : config.WifeOffset) : Vector2.zero;
            // Offsets, distances and speeds are tuned at world scale 1.
            float margin = config != null ? config.FloorMargin : 0.25f;
            // A spot under debris moves to the nearest free cell, so followers settle instead of re-planning forever.
            return rooms.Current.ApproachTarget(leader.position + rooms.Current.WorldOffset(offset), margin);
        }

        private float Speed()
        {
            float scale = rooms != null && rooms.Current != null ? rooms.Current.Scale : 1f;
            float speed = config != null ? config.RunSpeed * config.FollowerSpeedFactor * scale : 2.6f * scale;
            GameManager gm = GameManager.Instance;
            if (config != null && member != null && gm != null && gm.State.GetMember(member.Type).Health == MemberHealth.Injured)
            {
                speed *= config.InjuredSpeedFactor;
            }

            return speed;
        }
    }
}
