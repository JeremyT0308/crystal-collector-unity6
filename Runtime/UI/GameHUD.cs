using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalCollector
{
    /// <summary>
    /// Presents score, timer, temporary messages, pause, and victory panels.
    /// </summary>
    public sealed class GameHUD : MonoBehaviour
    {
        [Header("Live HUD")]
        public Text crystalCountText;
        public Text timerText;
        public Text centerMessageText;
        public Text pickupMessageText;
        public Image[] crystalIndicators;
        public CanvasGroup centerMessageGroup;
        public CanvasGroup pickupMessageGroup;
        public RectTransform crystalCounterTransform;
        public RectTransform pickupMessageTransform;

        [Header("Panels")]
        public CanvasGroup pauseGroup;
        public RectTransform pausePanel;
        public CanvasGroup victoryGroup;
        public RectTransform victoryPanel;
        public Text victoryCountText;
        public Text victoryTimeText;
        public Button resumeButton;
        public Button restartButton;
        public Button pauseMenuButton;
        public Button playAgainButton;
        public Button victoryMenuButton;

        private Coroutine centerRoutine;
        private Coroutine pickupRoutine;
        private Coroutine counterRoutine;

        public void Bind(GameFlowController gameFlow)
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(gameFlow.Resume);
            if (restartButton != null) restartButton.onClick.AddListener(gameFlow.Restart);
            if (pauseMenuButton != null) pauseMenuButton.onClick.AddListener(gameFlow.ReturnToMenu);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(gameFlow.Restart);
            if (victoryMenuButton != null) victoryMenuButton.onClick.AddListener(gameFlow.ReturnToMenu);

            HidePanel(pauseGroup);
            HidePanel(victoryGroup);
            HideMessage(centerMessageGroup);
            HideMessage(pickupMessageGroup);
        }

        public void SetCrystalCount(int count, int total)
        {
            if (crystalCountText != null) crystalCountText.text = $"CRYSTALS  {count} / {total}";
            if (crystalIndicators != null)
            {
                for (var i = 0; i < crystalIndicators.Length; i++)
                {
                    if (crystalIndicators[i] == null) continue;
                    crystalIndicators[i].color = i < count
                        ? new Color(0.18f, 0.92f, 1f, 1f)
                        : new Color(0.18f, 0.3f, 0.46f, 0.45f);
                }
            }
            if (counterRoutine != null) StopCoroutine(counterRoutine);
            counterRoutine = StartCoroutine(PunchCounter());
        }

        public void SetTimer(float seconds)
        {
            if (timerText == null) return;
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var remainder = seconds - minutes * 60f;
            timerText.text = $"TIME  {minutes:00}:{remainder:00.0}";
        }

        public void ShowCenterMessage(string message, float duration)
        {
            if (centerRoutine != null) StopCoroutine(centerRoutine);
            centerRoutine = StartCoroutine(CenterMessageRoutine(message, duration));
        }

        public void ShowPickupMessage()
        {
            if (pickupRoutine != null) StopCoroutine(pickupRoutine);
            pickupRoutine = StartCoroutine(PickupMessageRoutine());
        }

        public void ShowPause()
        {
            ShowPanel(pauseGroup, pausePanel);
        }

        public void HidePause()
        {
            HidePanel(pauseGroup);
        }

        public void ShowVictory(int count, float time)
        {
            if (victoryCountText != null) victoryCountText.text = $"{count} CRYSTALS SECURED";
            if (victoryTimeText != null) victoryTimeText.text = $"TIME: {time:0.0} s";
            HidePanel(pauseGroup);
            ShowPanel(victoryGroup, victoryPanel);
        }

        private IEnumerator CenterMessageRoutine(string message, float duration)
        {
            if (centerMessageText == null || centerMessageGroup == null) yield break;
            centerMessageText.text = message;
            centerMessageGroup.gameObject.SetActive(true);
            centerMessageGroup.alpha = 0f;
            yield return UIAnimator.Fade(centerMessageGroup, 1f, 0.18f);
            if (duration > 0f) yield return new WaitForSecondsRealtime(duration);
            yield return UIAnimator.Fade(centerMessageGroup, 0f, 0.2f);
            centerMessageGroup.gameObject.SetActive(false);
        }

        private IEnumerator PickupMessageRoutine()
        {
            if (pickupMessageText == null || pickupMessageGroup == null) yield break;
            pickupMessageText.text = "+1 CRYSTAL";
            pickupMessageGroup.gameObject.SetActive(true);
            pickupMessageGroup.alpha = 0f;
            if (pickupMessageTransform != null) pickupMessageTransform.localScale = Vector3.one * 0.8f;
            yield return UIAnimator.Fade(pickupMessageGroup, 1f, 0.12f);
            if (pickupMessageTransform != null) yield return UIAnimator.Scale(pickupMessageTransform, Vector3.one * 0.8f, Vector3.one, 0.18f);
            yield return new WaitForSecondsRealtime(0.35f);
            yield return UIAnimator.Fade(pickupMessageGroup, 0f, 0.28f);
            pickupMessageGroup.gameObject.SetActive(false);
        }

        private IEnumerator PunchCounter()
        {
            if (crystalCounterTransform == null) yield break;
            yield return UIAnimator.Scale(crystalCounterTransform, Vector3.one * 1.15f, Vector3.one, 0.22f);
        }

        private static void HideMessage(CanvasGroup group)
        {
            if (group == null) return;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.gameObject.SetActive(false);
        }

        private static void HidePanel(CanvasGroup group)
        {
            if (group == null) return;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.gameObject.SetActive(false);
        }

        private void ShowPanel(CanvasGroup group, RectTransform panel)
        {
            if (group == null) return;
            StopCoroutine(nameof(AnimatePanel));
            StartCoroutine(AnimatePanel(group, panel, true));
        }

        private IEnumerator AnimatePanel(CanvasGroup group, RectTransform panel, bool visible)
        {
            yield return UIAnimator.FadeAndScale(group, panel, visible, 0.28f);
        }
    }
}
