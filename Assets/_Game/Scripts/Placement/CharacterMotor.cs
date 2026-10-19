using System;
using System.Collections.Generic;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Moves a character along world waypoints at a given speed (the callers plan the path with the room's GridNav;
    /// a single point is a straight run). Scaled time, so pausing pauses it; never touches timeScale. The owner
    /// (the mover, a follower, placement) is remembered so one system never stops a run another system started.
    /// </summary>
    public sealed class CharacterMotor : MonoBehaviour
    {
        private const float ArriveDistance = 0.02f;

        private readonly List<Vector3> waypoints = new List<Vector3>();
        private int next;
        private float speed;
        private Action arrived;

        public bool IsMoving { get; private set; }

        /// <summary>Who started the current run (null when standing or for anonymous runs).</summary>
        public object Owner { get; private set; }

        /// <summary>World units per second this frame (zero while standing).</summary>
        public Vector2 Velocity { get; private set; }

        /// <summary>Current speed / reference speed (animation rate).</summary>
        public float SpeedRatio { get; private set; }

        /// <summary>Reference speed for SpeedRatio (the player's run speed).</summary>
        public float ReferenceSpeed { get; set; } = 2.4f;

        /// <summary>Final destination of the current run (the position itself when standing).</summary>
        public Vector3 Destination => IsMoving && waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : transform.position;

        private void OnEnable()
        {
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.Died -= OnDied;
            Stop();
        }

        /// <summary>Runs straight to point; arrivedCallback fires once on arrival (not when stopped).</summary>
        public void RunTo(Vector3 point, float unitsPerSecond, Action arrivedCallback = null, object owner = null)
        {
            RunPath(new[] { point }, unitsPerSecond, arrivedCallback, owner);
        }

        /// <summary>Runs through the waypoints in order; arrivedCallback fires once at the last one (not when stopped).</summary>
        public void RunPath(IReadOnlyList<Vector3> points, float unitsPerSecond, Action arrivedCallback = null, object owner = null)
        {
            waypoints.Clear();
            if (points != null)
            {
                foreach (Vector3 p in points) waypoints.Add(new Vector3(p.x, p.y, transform.position.z));
            }

            if (waypoints.Count == 0) waypoints.Add(transform.position);
            next = 0;
            speed = Mathf.Max(0.01f, unitsPerSecond);
            arrived = arrivedCallback;
            Owner = owner;
            IsMoving = true;
        }

        /// <summary>Changes the destination (straight) without resetting the callback or owner (followers).</summary>
        public void Retarget(Vector3 point, float unitsPerSecond)
        {
            RetargetPath(new[] { point }, unitsPerSecond);
        }

        /// <summary>Changes the waypoints without resetting the callback or owner (followers re-planning).</summary>
        public void RetargetPath(IReadOnlyList<Vector3> points, float unitsPerSecond)
        {
            if (points == null || points.Count == 0) return;
            waypoints.Clear();
            foreach (Vector3 p in points) waypoints.Add(new Vector3(p.x, p.y, transform.position.z));
            next = 0;
            speed = Mathf.Max(0.01f, unitsPerSecond);
        }

        public void Stop()
        {
            IsMoving = false;
            arrived = null;
            Owner = null;
            waypoints.Clear();
            next = 0;
            Velocity = Vector2.zero;
            SpeedRatio = 0f;
        }

        private void Update()
        {
            if (!IsMoving) return;
            Vector3 position = transform.position;
            float step = speed * Time.deltaTime;
            Vector3 moved = position;

            // Spend this frame's step across as many waypoints as it reaches.
            while (next < waypoints.Count)
            {
                Vector3 target = waypoints[next];
                Vector3 delta = target - moved;
                delta.z = 0f;
                float distance = delta.magnitude;
                bool last = next == waypoints.Count - 1;
                if (distance <= Mathf.Max(last ? ArriveDistance : 0f, step))
                {
                    moved = new Vector3(target.x, target.y, moved.z);
                    step -= distance;
                    next++;
                    if (last)
                    {
                        transform.position = moved;
                        Action callback = arrived;
                        Stop();
                        callback?.Invoke();
                        return;
                    }

                    continue;
                }

                moved += delta / distance * step;
                break;
            }

            Vector3 move = moved - position;
            transform.position = moved;
            Velocity = Time.deltaTime > 0f ? (Vector2)(move / Time.deltaTime) : Vector2.zero;
            // Reference speed is tuned at world scale 1; compare against it at this character's world scale.
            float scale = transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.x) : 1f;
            SpeedRatio = speed / Mathf.Max(0.01f, ReferenceSpeed * scale);
        }

        private void OnDied(DeathInfo info) => Stop();
    }
}
