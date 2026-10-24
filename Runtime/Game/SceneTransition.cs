using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrystalCollector
{
    /// <summary>
    /// Provides a small unscaled-time fade overlay for scene transitions.
    /// </summary>
    public sealed class SceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private float fadeDuration = 0.35f;

        private bool transitionInProgress;
        private Coroutine fadeRoutine;

        public void Configure(CanvasGroup fadeOverlay)
        {
            overlay = fadeOverlay;
        }

        private void Awake()
        {
            SetOverlayState(true, 1f);
        }

        private void Start()
        {
            fadeRoutine = StartCoroutine(FadeTo(0f, fadeDuration));
        }

        public void FadeToScene(string sceneName)
        {
            if (transitionInProgress || string.IsNullOrWhiteSpace(sceneName)) return;
            transitionInProgress = true;
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            StartCoroutine(FadeAndLoad(sceneName));
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            SetOverlayInput(true);
            yield return FadeTo(1f, fadeDuration);
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (overlay == null) yield break;

            if (targetAlpha > 0.001f)
            {
                SetOverlayInput(true);
            }

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
            if (targetAlpha <= 0.001f)
            {
                SetOverlayInput(false);
            }
        }

        private void SetOverlayState(bool blocksInput, float alpha)
        {
            if (overlay == null) return;
            overlay.gameObject.SetActive(true);
            overlay.alpha = alpha;
            SetOverlayInput(blocksInput);
        }

        private void SetOverlayInput(bool blocksInput)
        {
            if (overlay == null) return;
            overlay.interactable = blocksInput;
            overlay.blocksRaycasts = blocksInput;
            var image = overlay.GetComponent<Graphic>();
            if (image != null) image.raycastTarget = blocksInput;
        }
    }
}
