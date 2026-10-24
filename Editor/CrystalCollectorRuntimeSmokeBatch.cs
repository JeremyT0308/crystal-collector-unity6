using System;
using System.Collections.Generic;
using System.Linq;
using CrystalCollector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

namespace CrystalCollector.EditorTools
{
    /// <summary>
    /// Deterministic Play Mode smoke test for the real Rigidbody -> trigger -> ScriptMachine path.
    /// </summary>
    public static class CrystalCollectorRuntimeSmokeBatch
    {
        private const string PendingKey = "CrystalCollector.RuntimeSmoke.Pending";
        private enum Step
        {
            WaitingForPlaying,
            WaitingForCollection,
            WaitingForDuplicate,
            WaitingForVictory
        }

        private static readonly List<CollectiblePresentation> Crystals = new List<CollectiblePresentation>();
        private static Step step;
        private static int crystalIndex;
        private static double nextAction;
        private static PlayerMovement player;
        private static Rigidbody playerBody;
        private static Collider playerCollider;
        private static SceneVariables sceneVariables;
        private static GameFlowController flow;
        private static GameHUD hud;
        private static SceneTransition transition;

        [InitializeOnLoadMethod]
        private static void ReattachAfterDomainReload()
        {
            if (SessionState.GetBool(PendingKey, false) && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
        }

        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Cannot start the Crystal Collector smoke test while Play Mode is already active.");
                EditorApplication.Exit(2);
                return;
            }

            var scene = EditorSceneManager.OpenScene(CrystalCollectorGenerator.MainGameScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.update -= Tick;
            }
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;

            if (flow == null)
            {
                flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
                hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
                transition = UnityEngine.Object.FindFirstObjectByType<SceneTransition>();
                player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
                sceneVariables = UnityEngine.Object.FindFirstObjectByType<SceneVariables>();
                if (player != null)
                {
                    playerBody = player.GetComponent<Rigidbody>();
                    playerCollider = player.GetComponent<Collider>();
                }
                Crystals.Clear();
                Crystals.AddRange(UnityEngine.Object.FindObjectsByType<CollectiblePresentation>(FindObjectsSortMode.None).OrderBy(item => item.name));
                step = Step.WaitingForPlaying;
                crystalIndex = 0;
            }

            if (flow == null || player == null || sceneVariables == null || Crystals.Count != 8) return;

            if (step == Step.WaitingForPlaying)
            {
                if (flow.State != GameFlowController.GameState.Playing) return;
                var overlay = transition == null ? null : transition.GetComponentInChildren<CanvasGroup>(true);
                if (overlay == null || overlay.alpha > 0.001f || overlay.blocksRaycasts || overlay.interactable)
                    Fail("Scene transition overlay did not release input after the fade-in.");
                flow.Pause();
                if (flow.State != GameFlowController.GameState.Paused || !Mathf.Approximately(Time.timeScale, 0f))
                    Fail("Pause did not enter Paused state with timeScale 0.");
                if (hud == null || hud.pauseGroup == null || !hud.pauseGroup.interactable || !hud.pauseGroup.blocksRaycasts)
                    Fail("Pause panel did not become interactive.");
                hud.resumeButton.onClick.Invoke();
                if (flow.State != GameFlowController.GameState.Playing || !Mathf.Approximately(Time.timeScale, 1f))
                    Fail("Resume button did not restore Playing state.");
                BeginCollectionAttempt();
                return;
            }

            if (EditorApplication.timeSinceStartup < nextAction) return;

            if (step == Step.WaitingForCollection)
            {
                AssertState(crystalIndex + 1, true, Crystals[crystalIndex]);
                playerBody.position = Crystals[crystalIndex].transform.position;
                Physics.SyncTransforms();
                step = Step.WaitingForDuplicate;
                nextAction = EditorApplication.timeSinceStartup + 0.18d;
                return;
            }

            if (step == Step.WaitingForDuplicate)
            {
                AssertState(crystalIndex + 1, true, Crystals[crystalIndex]);
                crystalIndex++;
                if (crystalIndex >= Crystals.Count)
                {
                    step = Step.WaitingForVictory;
                    nextAction = EditorApplication.timeSinceStartup + 0.25d;
                }
                else
                {
                    BeginCollectionAttempt();
                }
                return;
            }

            if (step == Step.WaitingForVictory)
            {
                if (flow.State != GameFlowController.GameState.Victory)
                {
                    Fail("All eight graph collections completed, but GameFlowController did not enter Victory.");
                    return;
                }

                Debug.Log("CRYSTAL COLLECTOR RUNTIME SMOKE TEST PASSED: physics triggers collected 8 crystals, duplicates were ignored, and the Player collider remained enabled.");
                SessionState.SetBool(PendingKey, false);
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
            }
        }

        private static void BeginCollectionAttempt()
        {
            var crystal = Crystals[crystalIndex];
            if (!crystal.gameObject.activeInHierarchy) Fail($"Crystal {crystal.name} was inactive before collection.");
            playerBody.position = crystal.transform.position;
            playerBody.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            step = Step.WaitingForCollection;
            nextAction = EditorApplication.timeSinceStartup + 0.22d;
        }

        private static void AssertState(int expectedCount, bool requirePlayerCollider, CollectiblePresentation crystal)
        {
            var actual = Convert.ToSingle(sceneVariables.variables.declarations.Get("CollectedCount"));
            if (Mathf.RoundToInt(actual) != expectedCount) Fail($"Expected CollectedCount {expectedCount}, observed {actual} at {crystal.name}.");
            if (requirePlayerCollider && (playerCollider == null || !playerCollider.enabled)) Fail($"Player collider was disabled after {crystal.name}.");
            var crystalCollider = crystal.GetComponent<Collider>();
            if (crystalCollider == null || crystalCollider.enabled) Fail($"Crystal collider was not disabled by Visual Scripting for {crystal.name}.");
        }

        private static void Fail(string message)
        {
            Debug.LogError("CRYSTAL COLLECTOR RUNTIME SMOKE TEST FAILED: " + message);
            SessionState.SetBool(PendingKey, false);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(1);
        }
    }
}
