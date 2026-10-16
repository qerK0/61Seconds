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

        /// <summary>Shakes for a duration at a magnitude.</summary>
        public void Shake(float duration, float magnitude, bool grow = false)
        {
            StopShake();
            running = StartCoroutine(ShakeRoutine(duration, magnitude, grow));
        }

        private void OnQuakeStarted(QuakeInfo info)
        {
            int index = Mathf.Clamp(info.Act - 1, 0, intensityPerAct.Length - 1);
            float magnitude = intensityPerAct.Length > 0 ? intensityPerAct[index] : 0.15f;
            Shake(info.Duration, magnitude);
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
