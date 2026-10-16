using System.Collections;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>A prop animated by code tweens when its hazard goes off (no Animator needed).</summary>
    public sealed class HazardProp : MonoBehaviour
    {
        [Tooltip("Matches Hazard.TargetPropId.")]
        [SerializeField] private string propId;

        [Tooltip("Seconds the motion takes.")]
        [SerializeField] private float duration = 0.7f;

        [Tooltip("Topple angle in degrees (negative = falls right).")]
        [SerializeField] private float toppleAngle = -80f;

        [Tooltip("World units a Drop motion falls.")]
        [SerializeField] private float dropDistance = 0.8f;

        private SpriteRenderer[] renderers;
        private Color[] startColors;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 startScale;
        private bool captured;

        public string PropId => string.IsNullOrEmpty(propId) ? name : propId;

        private void Awake()
        {
            Capture();
        }

        private void Capture()
        {
            if (captured) return;
            captured = true;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            startColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) startColors[i] = renderers[i].color;
            startPosition = transform.localPosition;
            startRotation = transform.localRotation;
            startScale = transform.localScale;
        }

        /// <summary>Puts the prop back to its authored pose.</summary>
        public void ResetPose()
        {
            Capture();
            StopAllCoroutines();
            transform.localPosition = startPosition;
            transform.localRotation = startRotation;
            transform.localScale = startScale;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].color = startColors[i];
            }
        }

        /// <summary>Starts the motion on this prop's own coroutine runner.</summary>
        public void Play(HazardMotion motion)
        {
            Capture();
            if (isActiveAndEnabled) StartCoroutine(PlayRoutine(motion));
        }

        private IEnumerator PlayRoutine(HazardMotion motion)
        {
            switch (motion)
            {
                case HazardMotion.Topple:
                    yield return Tween.Shake(transform, duration * 0.4f, 0.06f, grow: true);
                    yield return Tween.RotateZ(transform, toppleAngle, duration, Ease.OutBounce);
                    break;
                case HazardMotion.Drop:
                    yield return Tween.Shake(transform, duration * 0.4f, 0.04f, grow: true);
                    yield return Tween.MoveLocal(transform, transform.localPosition + Vector3.down * dropDistance, duration, Ease.OutBounce);
                    break;
                case HazardMotion.Shatter:
                    yield return Tween.Shake(transform, duration * 0.5f, 0.08f);
                    StartCoroutine(Tween.Scale(transform, startScale * 1.15f, duration * 0.5f, Ease.OutQuad));
                    foreach (SpriteRenderer r in renderers)
                    {
                        if (r != null) StartCoroutine(Tween.Fade(r, 0.15f, duration * 0.5f));
                    }
                    break;
            }
        }
    }
}
