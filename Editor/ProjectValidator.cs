using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CrystalCollector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

namespace CrystalCollector.EditorTools
{
    /// <summary>
    /// Checks the generated assignment against the important structural requirements.
    /// </summary>
    public static class ProjectValidator
    {
        [MenuItem("Tools/Crystal Collector/Validate Project", priority = 10)]
        public static void ValidateProjectMenu()
        {
            ValidateProject(true);
        }

        public static bool ValidateProject(bool logResults)
        {
            var results = new System.Collections.Generic.List<string>();
            var passed = true;

            void Check(bool condition, string message)
            {
                if (condition) results.Add("[PASS] " + message);
                else
                {
                    results.Add("[FAIL] " + message);
                    passed = false;
                }
            }

            var version = File.Exists("ProjectSettings/ProjectVersion.txt") ? File.ReadAllText("ProjectSettings/ProjectVersion.txt") : string.Empty;
            Check(version.Contains("6000.3.22f1"), "Unity version 6000.3.22f1 detected");
            Check(File.Exists("Packages/manifest.json") && File.ReadAllText("Packages/manifest.json").Contains("com.unity.render-pipelines.universal"), "URP package detected");
            var visualScriptingPackage = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.unity.visualscripting");
            Check(visualScriptingPackage != null && visualScriptingPackage.version == "1.9.12", "Visual Scripting package 1.9.12 detected");
            Check(Shader.Find("Universal Render Pipeline/Lit") != null || Shader.Find("Standard") != null, "URP Lit-compatible shader detected");

            var mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(CrystalCollectorGenerator.MainMenuScenePath);
            var mainGame = AssetDatabase.LoadAssetAtPath<SceneAsset>(CrystalCollectorGenerator.MainGameScenePath);
            Check(mainMenu != null, "MainMenu scene exists");
            Check(mainGame != null, "MainGame scene exists");

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalCollectorGenerator.PlayerPrefabPath);
            var crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalCollectorGenerator.CrystalPrefabPath);
            Check(playerPrefab != null, "Player prefab exists");
            Check(crystalPrefab != null, "Crystal prefab exists");
            if (playerPrefab != null)
            {
                Check(playerPrefab.GetComponent<Rigidbody>() != null, "Player has Rigidbody");
                Check(playerPrefab.GetComponent<Collider>() != null, "Player has Collider");
                Check(playerPrefab.GetComponent<PlayerMovement>() != null, "PlayerMovement exists");
                Check(playerPrefab.CompareTag("Player"), "Player tag is correct");
            }

            var graph = AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(VisualScriptingGraphGenerator.GraphPath);
            Check(graph != null, "Collection graph exists");
            if (graph != null)
            {
                var unitTypes = graph.graph.units.Select(unit => unit.GetType().Name).ToArray();
                Check(unitTypes.Contains(nameof(OnTriggerEnter)), "Collection graph contains trigger event");
                Check(unitTypes.Contains(nameof(GetVariable)) && unitTypes.Contains(nameof(SetVariable)), "Collection graph contains Visual Scripting variables");
                Check(unitTypes.Contains(nameof(If)) && unitTypes.Contains(nameof(Equal)), "Collection graph contains validation branches");
                Check(unitTypes.Contains(nameof(Comparison)) && unitTypes.Contains(nameof(ScalarSum)), "Collection graph contains score increment and threshold comparison");
                var scalarSum = graph.graph.units.OfType<ScalarSum>().FirstOrDefault();
                Check(scalarSum != null && scalarSum.multiInputs.Count > 1 && scalarSum.multiInputs[1].hasDefaultValue, "Collection graph contains a serialized +1 increment operand");
                Check(unitTypes.Contains(nameof(InvokeMember)) && unitTypes.Contains(nameof(SetMember)), "Collection graph contains notification and trigger disabling");
                Check(graph.graph.valueConnections.Count() + graph.graph.controlConnections.Count() >= 16, "Collection graph contains connected collection flow");
            }

            Check(RuntimeSourceHasNoCollectionCallbacks(), "No runtime C# trigger-based collection implementation detected");

            var previousScenePath = SceneManager.GetActiveScene().path;
            if (mainGame != null)
            {
                var gameScene = EditorSceneManager.OpenScene(CrystalCollectorGenerator.MainGameScenePath, OpenSceneMode.Single);
                var crystals = UnityEngine.Object.FindObjectsByType<CollectiblePresentation>(FindObjectsSortMode.None);
                Check(crystals.Length == 8, "Exactly 8 gameplay crystals exist");
                var triggerCount = crystals.Count(crystal => crystal.GetComponent<Collider>() != null && crystal.GetComponent<Collider>().isTrigger);
                Check(triggerCount == 8, "All collectibles have trigger colliders");
                var machines = crystals.Select(crystal => crystal.GetComponent<ScriptMachine>()).ToArray();
                Check(machines.All(machine => machine != null), "All collectibles have Script Machines");
                Check(machines.All(machine => machine != null && machine.nest.macro == graph), "All collectibles reference CollectCrystal graph");
                var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
                Check(hud != null && hud.pauseGroup != null && hud.victoryGroup != null, "Gameplay HUD, pause panel, and victory panel exist");
                Check(UnityEngine.Object.FindFirstObjectByType<Camera>() != null, "Gameplay camera exists");
                Check(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length >= 1, "Gameplay lighting exists");
                RestoreScene(previousScenePath, gameScene);
            }

            if (mainMenu != null)
            {
                var menuScene = EditorSceneManager.OpenScene(CrystalCollectorGenerator.MainMenuScenePath, OpenSceneMode.Single);
                var menu = UnityEngine.Object.FindFirstObjectByType<MenuController>();
                Check(menu != null && menu.playButton != null && menu.howToPlayButton != null && menu.quitButton != null, "Main Menu and How To Play controls exist");
                Check(UnityEngine.Object.FindFirstObjectByType<Camera>() != null, "Menu camera exists");
                RestoreScene(previousScenePath, menuScene);
            }

            var buildScenes = EditorBuildSettings.scenes;
            Check(buildScenes.Length >= 2 && buildScenes[0].path == CrystalCollectorGenerator.MainMenuScenePath && buildScenes[0].enabled, "MainMenu is the first enabled build scene");
            Check(buildScenes.Any(scene => scene.path == CrystalCollectorGenerator.MainGameScenePath && scene.enabled), "MainGame is an enabled build scene");

            if (logResults)
            {
                foreach (var result in results) Debug.Log(result);
                Debug.Log(passed ? "VALIDATION PASSED" : "VALIDATION FAILED");
            }

            return passed;
        }

        private static bool RuntimeSourceHasNoCollectionCallbacks()
        {
            if (!Directory.Exists("Assets/Scripts/Runtime")) return true;
            var callbackPattern = new Regex(@"\b(OnTriggerEnter|OnTriggerStay|OnCollisionEnter|OnCollisionStay)\s*\(", RegexOptions.Compiled);
            return Directory.GetFiles("Assets/Scripts/Runtime", "*.cs", SearchOption.AllDirectories)
                .All(path => !callbackPattern.IsMatch(File.ReadAllText(path)));
        }

        private static void RestoreScene(string previousScenePath, Scene openedScene)
        {
            if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
            else if (openedScene.IsValid())
            {
                EditorSceneManager.SetActiveScene(openedScene);
            }
        }
    }
}
