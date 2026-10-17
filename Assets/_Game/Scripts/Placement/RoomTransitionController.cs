using System;
using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>Kind of room change.</summary>
    public enum TransitionKind
    {
        /// <summary>Same floor: cross-fade with a sideways pan.</summary>
        Lateral,

        /// <summary>Down a floor (rope, cables, stairs, hole): longer fade with a downward pan.</summary>
        Descend
    }

    /// <summary>
    /// Room-to-room transition (coroutine tweens, unscaled time, no DOTween): locks input
    /// (NavigationEvents.OnTransitionStart), fades to dark while the camera rig pans, swaps the room at the
    /// midpoint, fades back, then unlocks input (OnTransitionComplete). Rewinds skip it (instant swap).
    /// Reduce Effects keeps the fade but drops the camera pan.
    /// </summary>
    public sealed class RoomTransitionController : MonoBehaviour
    {
        [Tooltip("Durations, pan distances and cues.")]
        [SerializeField] private TransitionConfig config;

        [Tooltip("Full-screen dark overlay (never blocks clicks; input is locked through InputGate).")]
        [SerializeField] private CanvasGroup overlay;

        [Tooltip("Camera rig to pan (the camera itself is shaken by CameraShake, so pan its parent).")]
        [SerializeField] private Transform cameraRig;

        private Coroutine running;
        private Action pendingMidpoint;
        private string pendingRoom;
        private bool locked;
        private Vector3 rigOrigin;

        public bool IsPlaying => running != null;

        private void Awake()
        {
            if (cameraRig != null) rigOrigin = cameraRig.localPosition;
            SetOverlay(0f);
        }

        private void OnDisable()
        {
            Finish();
        }

        /// <summary>Plays a transition; midpoint swaps the room. A running transition completes first.</summary>
        public void Play(TransitionKind kind, string fromRoom, string toRoom, Action midpoint)
        {
            Finish();
            if (config == null || !isActiveAndEnabled)
            {
                midpoint?.Invoke();
                NavigationEvents.RaiseTransitionComplete(toRoom);
                return;
            }

            pendingMidpoint = midpoint;
            pendingRoom = toRoom;
            InputGate.Block();
            locked = true;
            NavigationEvents.RaiseTransitionStart(fromRoom, toRoom);
            if (kind == TransitionKind.Descend) AudioService.PlayCue(config.DescendCue);
            else
            {
                AudioService.PlayCue(config.DoorCue);
                AudioService.PlayCue(config.FootstepsCue);
            }

            running = StartCoroutine(Run(kind));
        }

        /// <summary>Lateral pan direction: +1 when moving to a room later in the scene list, else -1.</summary>
        public static float LateralDirection(int fromIndex, int toIndex) => toIndex >= fromIndex ? 1f : -1f;

        private IEnumerator Run(TransitionKind kind)
        {
            bool descend = kind == TransitionKind.Descend;
            float half = (descend ? config.DescendDuration : config.TransitionDuration) * 0.5f;
            Vector3 pan = descend ? new Vector3(0f, -config.DescendPan, 0f) : new Vector3(config.LateralPan * lastDirection, 0f, 0f);
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Settings != null && gm.Settings.ReduceEffects) pan = Vector3.zero;

            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float k = Smooth(t / half);
                SetOverlay(k * config.PeakAlpha);
                SetRig(pan * k);
                yield return null;
            }

            Action midpoint = pendingMidpoint;
            pendingMidpoint = null;
            midpoint?.Invoke();

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float k = Smooth(t / half);
                SetOverlay((1f - k) * config.PeakAlpha);
                SetRig(-pan * (1f - k));
                yield return null;
            }

            running = null;
            Complete();
        }

        private float lastDirection = 1f;

        /// <summary>Sets the sideways direction of the next lateral transition.</summary>
        public void SetDirection(float direction) => lastDirection = direction >= 0f ? 1f : -1f;

        /// <summary>Jumps a running transition to its end (swap + unlock).</summary>
        public void Finish()
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }

            Action midpoint = pendingMidpoint;
            pendingMidpoint = null;
            midpoint?.Invoke();
            if (locked) Complete();
        }

        private void Complete()
        {
            SetOverlay(0f);
            SetRig(Vector3.zero);
            if (locked)
            {
                locked = false;
                InputGate.Unblock();
            }

            NavigationEvents.RaiseTransitionComplete(pendingRoom);
        }

        private void SetOverlay(float alpha)
        {
            if (overlay == null) return;
            overlay.alpha = alpha;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;
        }

        private void SetRig(Vector3 offset)
        {
            if (cameraRig != null) cameraRig.localPosition = rigOrigin + offset;
        }

        private static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
