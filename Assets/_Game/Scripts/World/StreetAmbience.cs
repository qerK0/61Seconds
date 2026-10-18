using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Placement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SixtyOne.World
{
    /// <summary>
    /// The street at night, alive only while it is the shown room: a flickering street lamp, a swinging one, a
    /// burning spot behind a façade, a blinking car-alarm light, red / blue emergency lights pulsing from the far
    /// left (help is that way), plus a wind loop, distant sirens, the ambulance growing louder toward the sign and a
    /// distant dog bark or metal groan every 10–20 s. Reduce Effects calms the flicker and the swing.
    /// </summary>
    public sealed class StreetAmbience : MonoBehaviour
    {
        [SerializeField] private RoomSwitcher rooms;
        [SerializeField] private string streetRoomId = "street";
        [SerializeField] private GameManager gameManager;

        [Tooltip("The player (ambulance volume follows how far left they are).")]
        [SerializeField] private Transform player;

        [Header("Lights")]
        [SerializeField] private Light2D[] flicker = new Light2D[0];
        [SerializeField] private Light2D[] fire = new Light2D[0];
        [SerializeField] private Light2D[] blink = new Light2D[0];
        [SerializeField] private Light2D[] emergencyRed = new Light2D[0];
        [SerializeField] private Light2D[] emergencyBlue = new Light2D[0];
        [SerializeField] private Transform[] swing = new Transform[0];

        [Header("Sound")]
        [SerializeField] private AudioCue wind;
        [SerializeField] private AudioCue sirens;
        [SerializeField] private AudioCue ambulance;
        [SerializeField] private AudioCue[] oneShots = new AudioCue[0];
        [SerializeField] private Vector2 oneShotGap = new Vector2(10f, 20f);

        [Tooltip("Street length in grid cells (the sign is at x = 0).")]
        [SerializeField] private float streetLength = 20f;

        private float[] baseIntensity;
        private AudioSource ambulanceSource;
        private bool active;
        private float nextOneShot;

        private void Awake()
        {
            var all = new System.Collections.Generic.List<Light2D>();
            all.AddRange(flicker);
            all.AddRange(fire);
            all.AddRange(blink);
            all.AddRange(emergencyRed);
            all.AddRange(emergencyBlue);
            baseIntensity = new float[all.Count];
            for (int i = 0; i < all.Count; i++) baseIntensity[i] = all[i] != null ? all[i].intensity : 0f;
        }

        private void OnDisable() => SetActive(false);

        private void Update()
        {
            bool shown = rooms != null && rooms.Current != null && rooms.Current.RoomId == streetRoomId && !PauseState.IsPaused;
            if (shown != active) SetActive(shown);
            if (!active) return;

            bool calm = gameManager != null && gameManager.Settings != null && gameManager.Settings.ReduceEffects;
            float t = Time.time;
            int k = 0;
            foreach (Light2D l in flicker) Set(l, k++, calm ? 0.8f : (Mathf.PerlinNoise(t * 9f, 3.1f) > 0.35f ? 0.6f + 0.6f * Mathf.PerlinNoise(t * 23f, 1f) : 0.05f));
            foreach (Light2D l in fire) Set(l, k++, 0.75f + 0.45f * Mathf.PerlinNoise(t * (calm ? 2f : 7f), 5.3f));
            foreach (Light2D l in blink) Set(l, k++, Mathf.Repeat(t, 0.9f) < 0.45f ? 1f : 0.05f);
            float wave = Mathf.Sin(t * 5.5f);
            foreach (Light2D l in emergencyRed) Set(l, k++, Mathf.Clamp01(wave) * 1.2f + 0.1f);
            foreach (Light2D l in emergencyBlue) Set(l, k++, Mathf.Clamp01(-wave) * 1.2f + 0.1f);
            for (int i = 0; i < swing.Length; i++)
            {
                if (swing[i] != null) swing[i].localRotation = Quaternion.Euler(0f, 0f, (calm ? 2f : 9f) * Mathf.Sin(t * 1.3f + i));
            }

            if (ambulanceSource != null && ambulance != null && player != null && rooms.Current != null)
            {
                float x = rooms.Current.WorldToGrid(player.position).x;
                float near = 1f - Mathf.Clamp01(x / Mathf.Max(1f, streetLength));
                ambulanceSource.volume = ambulance.Volume * Mathf.Lerp(0.12f, 1f, near * near);
            }

            if (Time.time >= nextOneShot && oneShots.Length > 0)
            {
                AudioCue cue = oneShots[Random.Range(0, oneShots.Length)];
                if (cue != null && AudioService.Instance != null) AudioService.Instance.Play(cue, 0.45f, 1f);
                nextOneShot = Time.time + Random.Range(oneShotGap.x, oneShotGap.y);
            }
        }

        private void Set(Light2D light, int index, float factor)
        {
            if (light == null || baseIntensity == null || index >= baseIntensity.Length) return;
            light.intensity = baseIntensity[index] * factor;
        }

        private void SetActive(bool on)
        {
            active = on;
            AudioService audio = AudioService.Instance;
            if (audio == null) return;
            if (on)
            {
                if (wind != null) audio.Play(wind);
                if (sirens != null) audio.Play(sirens);
                ambulanceSource = ambulance != null ? audio.Play(ambulance) : null;
                nextOneShot = Time.time + Random.Range(oneShotGap.x * 0.5f, oneShotGap.y * 0.5f);
            }
            else
            {
                foreach (AudioCue cue in new[] { wind, sirens, ambulance }) if (cue != null) audio.FadeOut(cue, 0.6f);
                ambulanceSource = null;
            }
        }
    }
}
