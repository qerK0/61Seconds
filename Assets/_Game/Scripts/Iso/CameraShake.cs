using System.Collections;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Iso
{
    /// <summary>Shakes the camera while a quake runs; stronger for each act (6.0 / 7.0 / 8.0).</summary>
    public sealed class CameraShake : MonoBehaviour
    {
        [Tooltip("Shake magnitude in world units for act 1, 2 and 3.")]
        [SerializeField] private float[] intensityPerAct = { 0.12f, 0.22f, 0.38f };

        [Tooltip("Transform to shake. Defaults to this one.")]
        [SerializeField] private Transform target;

        [Tooltip("Camera whose orthographic size scales the shake. Defaults to the camera on this object.")]
        [SerializeField] private Camera sizeSource;

        [Tooltip("Ortho size the intensities are tuned at: framing that zooms out shakes proportionally more, so quakes feel equally strong in every room.")]
        [Min(0.1f)]
        [SerializeField] private float referenceOrthoSize = 2f;

        private Coroutine running;
        private Vector3 origin;

        private void Awake()
        {
            if (target == null) target = transform;
            origin = target.localPosition;
        }

        private void OnEnable()
        {
            GameEvents.QuakeStarted += OnQuakeStarted;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.QuakeStarted -= OnQuakeStarted;
            GameEvents.Died -= OnDied;
            StopShake();
        }

        /// <summary>Shakes for a duration at a magnitude (world units at the reference ortho size; scaled by SizeFactor).</summary>
        public void Shake(float duration, float magnitude, bool grow = false)
        {
            StopShake();
            running = StartCoroutine(ShakeRoutine(duration, magnitude * SizeFactor(), grow));
        }

        private void OnQuakeStarted(QuakeInfo info)
        {
            int index = Mathf.Clamp(info.Act - 1, 0, intensityPerAct.Length - 1);
            float magnitude = (intensityPerAct.Length > 0 ? intensityPerAct[index] : 0.15f) * info.ShakeScale;
            GameManager gm = GameManager.Instance;
            GameSettings settings = gm != null ? gm.Settings : null;
            if (settings != null && settings.ReduceEffects) magnitude *= settings.ReducedShakeScale;
            Shake(info.Duration, magnitude);
        }

        /// <summary>Current ortho size / reference size (screen-relative shake strength is then the same everywhere).</summary>
        public float SizeFactor()
        {
            if (sizeSource == null) sizeSource = GetComponent<Camera>();
            return SizeFactor(sizeSource != null && sizeSource.orthographic ? sizeSource.orthographicSize : referenceOrthoSize, referenceOrthoSize);
        }

        /// <summary>Pure scale rule (clamped so extreme framings stay sane).</summary>
        public static float SizeFactor(float orthoSize, float referenceSize)
        {
            if (referenceSize <= 0f) return 1f;
            return Mathf.Clamp(orthoSize / referenceSize, 0.4f, 3f);
        }

        private void OnDied(DeathInfo info)
        {
            StopShake();
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude, bool grow)
        {
            // Hold full strength most of the quake, then decay in the last third.
            float hold = grow ? 0f : duration * 0.66f;
            float time = 0f;
            while (time < hold)
            {
                time += Time.deltaTime;
                Vector2 offset = Random.insideUnitCircle * magnitude;
                target.localPosition = origin + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            yield return Tween.Shake(target, duration - hold, magnitude, grow);
            target.localPosition = origin;
            running = null;
        }

        private void StopShake()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            if (target != null) target.localPosition = origin;
        }
    }
}
