using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CrystalCollector
{
    /// <summary>
    /// Owns gameplay state, timer, pause/restart flow, and presentation bridges called by Visual Scripting.
    /// </summary>
    public sealed class GameFlowController : MonoBehaviour
    {
        public enum GameState
        {
            Countdown,
            Playing,
            Paused,
            Victory
        }

        public static GameFlowController Instance { get; private set; }

        public PlayerMovement player;
        public GameHUD hud;
        public SceneTransition transition;
        public new ProceduralAudio audio;
        public int totalCrystals = 8;

        public GameState State { get; private set; }
        public float ElapsedTime { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
            State = GameState.Countdown;
            hud?.Bind(this);
            hud?.SetCrystalCount(0, totalCrystals);
            hud?.SetTimer(0f);
        }

        private void Start()
        {
            StartCoroutine(BeginCountdown());
        }

        private void Update()
        {
            if (State == GameState.Playing)
            {
                ElapsedTime += Time.deltaTime;
                hud?.SetTimer(ElapsedTime);
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        public static void NotifyVisualScriptCrystalCollected(GameObject crystal, object newCount)
        {
            var count = newCount is float floatCount ? Mathf.RoundToInt(floatCount) : Mathf.RoundToInt(System.Convert.ToSingle(newCount));
            Instance?.ReceiveVisualScriptCollection(crystal, count);
        }

        public static void NotifyVisualScriptVictory()
        {
            Instance?.BeginVictory();
        }

        public void ReceiveVisualScriptCollection(GameObject crystal, int newCount)
        {
            if (State != GameState.Playing) return;
            crystal?.GetComponent<CollectiblePresentation>()?.PlayPickupFeedback();
            hud?.SetCrystalCount(newCount, totalCrystals);
            hud?.ShowPickupMessage();
            audio?.PlayPickup();
        }

        public void BeginVictory()
        {
            if (State == GameState.Victory) return;
            State = GameState.Victory;
            player?.SetInputEnabled(false);
            hud?.ShowVictory(totalCrystals, ElapsedTime);
            audio?.PlayVictory();
        }

        public void TogglePause()
        {
            if (State == GameState.Victory || State == GameState.Countdown) return;
            if (State == GameState.Paused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            player?.SetInputEnabled(false);
            hud?.ShowPause();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            State = GameState.Playing;
            player?.SetInputEnabled(true);
            hud?.HidePause();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            player?.SetInputEnabled(false);
            if (transition != null) transition.FadeToScene("MainMenu");
            else SceneManager.LoadScene("MainMenu");
        }

        private IEnumerator BeginCountdown()
        {
            Time.timeScale = 1f;
            State = GameState.Countdown;
            player?.SetInputEnabled(false);
            hud?.ShowCenterMessage("MISSION\nSECURE ALL ENERGY CRYSTALS", 0.55f);
            yield return new WaitForSecondsRealtime(0.8f);
            hud?.ShowCenterMessage("READY", 0.65f);
            yield return new WaitForSecondsRealtime(0.95f);
            hud?.ShowCenterMessage("GO!", 0.4f);
            yield return new WaitForSecondsRealtime(0.5f);
            State = GameState.Playing;
            player?.SetInputEnabled(true);
        }
    }
}
