using System.Collections;
using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Shared lightweight UI interpolation helpers using unscaled time.
    /// </summary>
    public static class UIAnimator
    {
        public static IEnumerator Fade(CanvasGroup group, float targetAlpha, float duration)
        {
            if (group == null) yield break;

            var startAlpha = group.alpha;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            group.alpha = targetAlpha;
        }

        public static IEnumerator Scale(RectTransform target, Vector3 from, Vector3 to, float duration)
        {
            if (target == null) yield break;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                target.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            target.localScale = to;
        }

        public static IEnumerator FadeAndScale(CanvasGroup group, RectTransform target, bool visible, float duration)
        {
            if (group == null || target == null) yield break;

            if (visible)
            {
                group.gameObject.SetActive(true);
                group.interactable = true;
                group.blocksRaycasts = true;
                group.alpha = 0f;
                target.localScale = Vector3.one * 0.86f;
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                    group.alpha = t;
                    target.localScale = Vector3.LerpUnclamped(Vector3.one * 0.86f, Vector3.one, t);
                    yield return null;
                }
                group.alpha = 1f;
                target.localScale = Vector3.one;
            }
            else
            {
                group.interactable = false;
                group.blocksRaycasts = false;
                yield return Fade(group, 0f, duration);
                group.gameObject.SetActive(false);
            }
        }
    }
}
