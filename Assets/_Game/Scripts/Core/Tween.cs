using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Core
{
    /// <summary>
    /// Coroutine tweens (no DOTween). Run with StartCoroutine(Tween.Move(...)).
    /// Every tween stops silently if its target is destroyed.
    /// </summary>
    public static class Tween
    {
        /// <summary>Calls step(easedT) every frame for the duration, ending with step(1).</summary>
        public static IEnumerator Run(float duration, Action<float> step, Ease ease = Ease.Linear, bool unscaled = false)
        {
            if (duration <= 0f)
            {
                step(Easing.Evaluate(ease, 1f));
                yield break;
            }

            float time = 0f;
            while (time < duration)
            {
                time += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                step(Easing.Evaluate(ease, time / duration));
                yield return null;
            }
        }

        /// <summary>Moves to a world position.</summary>
        public static IEnumerator Move(Transform target, Vector3 to, float duration, Ease ease = Ease.OutQuad, bool unscaled = false)
        {
            Vector3 from = target.position;
            yield return Run(duration, k => { if (target) target.position = Vector3.LerpUnclamped(from, to, k); }, ease, unscaled);
        }

        /// <summary>Moves to a local position.</summary>
        public static IEnumerator MoveLocal(Transform target, Vector3 to, float duration, Ease ease = Ease.OutQuad, bool unscaled = false)
        {
            Vector3 from = target.localPosition;
            yield return Run(duration, k => { if (target) target.localPosition = Vector3.LerpUnclamped(from, to, k); }, ease, unscaled);
        }

        /// <summary>Rotates around Z to an absolute local angle (degrees).</summary>
        public static IEnumerator RotateZ(Transform target, float toAngle, float duration, Ease ease = Ease.OutQuad, bool unscaled = false)
        {
            float from = target.localEulerAngles.z;
            float delta = Mathf.DeltaAngle(from, toAngle);
            yield return Run(duration, k => { if (target) target.localRotation = Quaternion.Euler(0f, 0f, from + delta * k); }, ease, unscaled);
        }

        /// <summary>Scales to a local scale.</summary>
        public static IEnumerator Scale(Transform target, Vector3 to, float duration, Ease ease = Ease.OutQuad, bool unscaled = false)
        {
            Vector3 from = target.localScale;
            yield return Run(duration, k => { if (target) target.localScale = Vector3.LerpUnclamped(from, to, k); }, ease, unscaled);
        }

        /// <summary>Fades a CanvasGroup's alpha.</summary>
        public static IEnumerator Fade(CanvasGroup group, float to, float duration, Ease ease = Ease.Linear, bool unscaled = true)
        {
            float from = group.alpha;
            yield return Run(duration, k => { if (group) group.alpha = Mathf.LerpUnclamped(from, to, k); }, ease, unscaled);
        }

        /// <summary>Fades a SpriteRenderer's color alpha.</summary>
        public static IEnumerator Fade(SpriteRenderer renderer, float to, float duration, Ease ease = Ease.Linear, bool unscaled = false)
        {
            float from = renderer.color.a;
            yield return Run(duration, k =>
            {
                if (!renderer) return;
                Color c = renderer.color;
                c.a = Mathf.LerpUnclamped(from, to, k);
                renderer.color = c;
            }, ease, unscaled);
        }

        /// <summary>Fades a uGUI Graphic's color alpha.</summary>
        public static IEnumerator Fade(Graphic graphic, float to, float duration, Ease ease = Ease.Linear, bool unscaled = true)
        {
            float from = graphic.color.a;
            yield return Run(duration, k =>
            {
                if (!graphic) return;
                Color c = graphic.color;
                c.a = Mathf.LerpUnclamped(from, to, k);
                graphic.color = c;
            }, ease, unscaled);
        }

        /// <summary>
        /// Shakes the local position randomly, decaying to zero, then restores it.
        /// Set grow to true for a reverse shake (starts calm, ends strong).
        /// </summary>
        public static IEnumerator Shake(Transform target, float duration, float magnitude, bool grow = false, bool unscaled = false)
        {
            Vector3 origin = target.localPosition;
            yield return Run(duration, k =>
            {
                if (!target) return;
                float strength = magnitude * (grow ? k : 1f - k);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * strength;
                target.localPosition = origin + new Vector3(offset.x, offset.y, 0f);
            }, Ease.Linear, unscaled);

            if (target)
            {
                target.localPosition = origin;
            }
        }
    }
}
