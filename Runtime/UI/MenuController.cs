using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalCollector
{
    /// <summary>
    /// Owns main-menu navigation and the animated how-to-play panel.
    /// </summary>
    public sealed class MenuController : MonoBehaviour
    {
        public Button playButton;
        public Button howToPlayButton;
        public Button quitButton;
        public Button howToPlayBackButton;
        public CanvasGroup menuGroup;
        public CanvasGroup howToPlayGroup;
        public RectTransform howToPlayPanel;
        public RectTransform titleTransform;
        public SceneTransition transition;
        public new ProceduralAudio audio;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(StartGame);
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(ShowHowToPlay);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            if (howToPlayBackButton != null) howToPlayBackButton.onClick.AddListener(HideHowToPlay);
            if (howToPlayGroup != null)
            {
                howToPlayGroup.alpha = 0f;
                howToPlayGroup.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (menuGroup != null)
            {
                menuGroup.alpha = 0f;
                StartCoroutine(UIAnimator.Fade(menuGroup, 1f, 0.7f));
            }
            if (titleTransform != null)
            {
                titleTransform.localScale = Vector3.one * 0.86f;
                StartCoroutine(UIAnimator.Scale(titleTransform, Vector3.one * 0.86f, Vector3.one, 0.75f));
            }
        }

        public void StartGame()
        {
            audio?.PlayClick();
            if (transition != null) transition.FadeToScene("MainGame");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
        }

        public void ShowHowToPlay()
        {
            audio?.PlayClick();
            if (howToPlayGroup == null) return;
            StopCoroutine(nameof(AnimateHowToPlay));
            StartCoroutine(AnimateHowToPlay(true));
        }

        public void HideHowToPlay()
        {
            audio?.PlayClick();
            if (howToPlayGroup == null) return;
            StopCoroutine(nameof(AnimateHowToPlay));
            StartCoroutine(AnimateHowToPlay(false));
        }

        public void QuitGame()
        {
            audio?.PlayClick();
            Debug.Log("Crystal Collector quit requested.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator AnimateHowToPlay(bool visible)
        {
            yield return UIAnimator.FadeAndScale(howToPlayGroup, howToPlayPanel, visible, 0.3f);
        }
    }
}
