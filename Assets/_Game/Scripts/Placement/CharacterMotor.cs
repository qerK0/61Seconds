using System;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Moves a character in a straight line to a world point at a given speed (no pathfinding; targets are
    /// clamped onto the room floor by the caller). Scaled time, so pausing pauses it; never touches timeScale.
    /// </summary>
    public sealed class CharacterMotor : MonoBehaviour
    {
        private const float ArriveDistance = 0.02f;

        private Vector3 target;
        private float speed;
        private Action arrived;

        public bool IsMoving { get; private set; }

        /// <summary>World units per second this frame (zero while standing).</summary>
        public Vector2 Velocity { get; private set; }

        /// <summary>Current speed / reference speed (animation rate).</summary>
        public float SpeedRatio { get; private set; }

        /// <summary>Reference speed for SpeedRatio (the player's run speed).</summary>
        public float ReferenceSpeed { get; set; } = 2.4f;

        private void OnEnable()
        {
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.Died -= OnDied;
            Stop();
        }

        /// <summary>Runs to point; arrivedCallback fires once on arrival (not when stopped).</summary>
        public void RunTo(Vector3 point, float unitsPerSecond, Action arrivedCallback = null)
        {
            target = new Vector3(point.x, point.y, transform.position.z);
            speed = Mathf.Max(0.01f, unitsPerSecond);
            arrived = arrivedCallback;
            IsMoving = true;
        }

        /// <summary>Changes the destination without resetting the callback (followers).</summary>
        public void Retarget(Vector3 point, float unitsPerSecond)
        {
            target = new Vector3(point.x, point.y, transform.position.z);
            speed = Mathf.Max(0.01f, unitsPerSecond);
        }

        public void Stop()
        {
            IsMoving = false;
            arrived = null;
            Velocity = Vector2.zero;
            SpeedRatio = 0f;
        }

        private void Update()
        {
            if (!IsMoving) return;
            Vector3 position = transform.position;
            Vector3 delta = target - position;
            delta.z = 0f;
            float distance = delta.magnitude;
            float step = speed * Time.deltaTime;
            if (distance <= Mathf.Max(ArriveDistance, step))
            {
                transform.position = target;
                Action callback = arrived;
                Stop();
                callback?.Invoke();
                return;
            }

            Vector3 move = delta / distance * step;
            transform.position = position + move;
            Velocity = Time.deltaTime > 0f ? (Vector2)(move / Time.deltaTime) : Vector2.zero;
            // Reference speed is tuned at world scale 1; compare against it at this character's world scale.
            float scale = transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.x) : 1f;
            SpeedRatio = speed / Mathf.Max(0.01f, ReferenceSpeed * scale);
        }

        private void OnDied(DeathInfo info) => Stop();
    }
}
