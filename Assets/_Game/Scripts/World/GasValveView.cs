using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// The visible state of one gas zone's valve. Reads the valve flag every frame (so rewinds restore it): while
    /// gas leaks (after the quake, flag false) the room shows a green haze and, when it is the shown room, the hiss
    /// loop plays. Closing the valve turns the handle 90° (tween around its centre), plays a short metal creak, stops
    /// the hiss and fades the haze. A rewind back to an open valve snaps the handle and the haze back.
    /// </summary>
    public sealed class GasValveView : MonoBehaviour
    {
        [Tooltip("Flag the valve choice sets (LevelConfig gas zone).")]
        [SerializeField] private string valveFlag;

        [Tooltip("The valve art that turns (bottom-centre pivot; it rotates around its middle).")]
        [SerializeField] private Transform handle;

        [Tooltip("Height of the handle art (rotation centre = half of it).")]
        [SerializeField] private float handleHeight = 0.25f;

        [Tooltip("Green haze sprites of the zone's rooms (faded when the valve closes).")]
        [SerializeField] private SpriteRenderer[] hazes = new SpriteRenderer[0];

        [Tooltip("Rooms of this zone (the hiss plays while one of them is shown).")]
        [SerializeField] private Room[] rooms = new Room[0];

        [Tooltip("Gas hiss loop.")]
        [SerializeField] private AudioCue hiss;

        [Tooltip("Short metal creak when the valve closes.")]
        [SerializeField] private AudioCue creak;

        [Tooltip("Haze alpha while gas leaks.")]
        [SerializeField] private float hazeAlpha = 0.22f;

        private GameManager gm;
        private RoomSwitcher switcher;
        private Vector3 handleRest;
        private bool known;
        private bool closed;
        private bool hissing;
        private Coroutine turning;

        /// <summary>Authoring (scene builder).</summary>
        public void Configure(string flag, Transform handleArt, float height, SpriteRenderer[] zoneHazes, Room[] zoneRooms, AudioCue hissCue, AudioCue creakCue)
        {
            valveFlag = flag;
            handle = handleArt;
            handleHeight = height;
            hazes = zoneHazes;
            rooms = zoneRooms;
            hiss = hissCue;
            creak = creakCue;
        }

        private void Awake()
        {
            if (handle != null) handleRest = handle.localPosition;
        }

        private void OnDisable() => StopHiss();

        private void Update()
        {
            if (gm == null) gm = FindFirstObjectByType<GameManager>();
            if (switcher == null) switcher = FindFirstObjectByType<RoomSwitcher>();
            if (gm == null || gm.State == null) return;

            bool isClosed = gm.State.Flags.Get(valveFlag);
            bool leaking = !isClosed && gm.State.QuakeCount > 0;
            if (!known || isClosed != closed)
            {
                bool animate = known && isClosed;
                known = true;
                closed = isClosed;
                if (animate) Close();
                else Snap(isClosed);
            }

            if (turning == null) SetHaze(leaking ? hazeAlpha * (0.85f + 0.15f * Mathf.Sin(Time.time * 1.7f)) : 0f);
            bool shown = leaking && switcher != null && System.Array.IndexOf(rooms, switcher.Current) >= 0 && !PauseState.IsPaused;
            if (shown && !hissing) StartHiss();
            else if (!shown && hissing) StopHiss();
        }

        private void Close()
        {
            if (turning != null) StopCoroutine(turning);
            StopHiss();
            AudioService.PlayCue(creak);
            turning = StartCoroutine(Turn());
        }

        private IEnumerator Turn()
        {
            float from = hazes.Length > 0 && hazes[0] != null ? hazes[0].color.a : 0f;
            // The handle turns in the first 0.45 s; the haze thins out over 1.5 s.
            yield return Tween.Run(1.5f, t =>
            {
                Rotate(-90f * Easing.Evaluate(Ease.OutQuad, Mathf.Clamp01(t / 0.3f)));
                SetHaze(from * (1f - t));
            });
            turning = null;
        }

        private void Snap(bool isClosed)
        {
            if (turning != null)
            {
                StopCoroutine(turning);
                turning = null;
            }

            Rotate(isClosed ? -90f : 0f);
        }

        /// <summary>Rotates the handle around its middle (its pivot is at the bottom).</summary>
        private void Rotate(float angle)
        {
            if (handle == null) return;
            Quaternion q = Quaternion.Euler(0f, 0f, angle);
            Vector3 centre = new Vector3(0f, handleHeight * 0.5f * handle.localScale.y, 0f);
            handle.localRotation = q;
            handle.localPosition = handleRest + centre - q * centre;
        }

        private void SetHaze(float alpha)
        {
            foreach (SpriteRenderer haze in hazes)
            {
                if (haze == null) continue;
                Color c = haze.color;
                c.a = alpha;
                haze.color = c;
                haze.enabled = alpha > 0.001f;
            }
        }

        private void StartHiss()
        {
            if (hiss == null) return;
            AudioService.PlayCue(hiss);
            hissing = true;
        }

        private void StopHiss()
        {
            if (!hissing) return;
            hissing = false;
            AudioService.FadeOutCue(hiss, 0.25f);
        }
    }
}
