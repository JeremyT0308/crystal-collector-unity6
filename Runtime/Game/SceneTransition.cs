using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrystalCollector
{
    /// <summary>
    /// Provides a small unscaled-time fade overlay for scene transitions.
    /// </summary>
    public sealed class SceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private float fadeDuration = 0.35f;

        public void Configure(CanvasGroup fadeOverlay)
        {
            overlay = fadeOverlay;
        }

        private void Awake()
        {
            if (overlay != null) overlay.alpha = 1f;
        }

        private void Start()
        {
            StartCoroutine(FadeTo(0f, fadeDuration));
        }

        public void FadeToScene(string sceneName)
        {
            StartCoroutine(FadeAndLoad(sceneName));
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            yield return FadeTo(1f, fadeDuration);
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (overlay == null) yield break;

            var startAlpha = overlay.alpha;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                overlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            overlay.alpha = targetAlpha;
        }
    }
}
