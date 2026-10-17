using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Wife / child: in exploration, run after the player to a fixed offset beside them, starting after a short
    /// delay (same straight-line motor, clamped onto the floor). Injured members are slower. Placement moves them
    /// instead (anchors), and room changes teleport them to the arrival point (RoomSwitcher.SnapMembers).
    /// </summary>
    public sealed class FamilyFollower : MonoBehaviour
    {
        private const float SettleDistance = 0.06f;

        [Tooltip("This member.")]
        [SerializeField] private FamilyMember member;

        [Tooltip("This member's motor.")]
        [SerializeField] private CharacterMotor motor;

        [Tooltip("The player to follow.")]
        [SerializeField] private Transform leader;

        [Tooltip("Current room (floor clamp).")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Speeds, delays and offsets.")]
        [SerializeField] private MovementConfig config;

        private float farTime;
        private CharacterMotor leaderMotor;
        private bool following;

        private void Awake()
        {
            if (leader != null) leaderMotor = leader.GetComponent<CharacterMotor>();
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            bool active = gm != null && !gm.IsDead && ExplorationGate.Allowed && leader != null && motor != null && rooms != null && rooms.Current != null && config != null;
            if (!active)
            {
                if (motor != null && motor.IsMoving) motor.Stop();
                farTime = 0f;
                following = false;
                return;
            }

            bool child = member != null && member.Type == MemberType.Child;
            Vector2 offset = child ? config.ChildOffset : config.WifeOffset;
            float delay = child ? config.ChildDelay : config.WifeDelay;
            // Offsets, distances and speeds are tuned at world scale 1.
            float scale = rooms.Current.Scale;
            Vector3 spot = rooms.Current.ClampToFloor(leader.position + rooms.Current.WorldOffset(offset), config.FloorMargin);
            float distance = Vector2.Distance(transform.position, spot);
            float speed = config.RunSpeed * config.FollowerSpeedFactor * scale;
            if (member != null && gm.State.GetMember(member.Type).Health == MemberHealth.Injured) speed *= config.InjuredSpeedFactor;

            if (motor.IsMoving)
            {
                if (distance <= SettleDistance * scale) motor.Stop();
                else motor.Retarget(spot, speed);
                return;
            }

            if (distance <= config.FollowStartDistance * scale)
            {
                farTime = 0f;
                following = false;
                return;
            }

            // Caught up with a still-running leader: keep going without re-waiting the start delay.
            bool leaderRunning = leaderMotor != null && leaderMotor.IsMoving;
            farTime += Time.deltaTime;
            if (farTime < delay && !(following && leaderRunning)) return;
            farTime = 0f;
            following = true;
            motor.RunTo(spot, speed);
        }
    }
}
