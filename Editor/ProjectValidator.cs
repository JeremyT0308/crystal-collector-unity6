using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CrystalCollector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
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
                Check(unitTypes.Contains(nameof(If)) && unitTypes.Contains(nameof(Negate)), "Collection graph contains duplicate-protection validation branches");
                Check(unitTypes.Contains(nameof(Comparison)) && unitTypes.Contains(nameof(ScalarSum)), "Collection graph contains score increment and threshold comparison");
                var scalarSum = graph.graph.units.OfType<ScalarSum>().FirstOrDefault();
                Check(scalarSum != null && scalarSum.multiInputs.Count > 1 && scalarSum.multiInputs[1].hasDefaultValue, "Collection graph contains a serialized +1 increment operand");
                Check(unitTypes.Contains(nameof(InvokeMember)) && unitTypes.Contains(nameof(SetMember)), "Collection graph contains notification and trigger disabling");
                Check(unitTypes.Contains(nameof(This)), "Collection graph contains an explicit self reference");
                var incomingColliderDisable = graph.graph.valueConnections.Any(connection =>
                    connection.source.unit is OnTriggerEnter && connection.destination.unit is SetMember);
                Check(!incomingColliderDisable, "Collection graph does not disable the incoming Player collider");
                var setCollider = graph.graph.units.OfType<SetMember>().FirstOrDefault();
                var ownColliderConnection = setCollider != null && graph.graph.valueConnections.Any(connection =>
                    connection.destination == setCollider.target && connection.source.unit is InvokeMember);
                Check(ownColliderConnection, "Collection graph disables the crystal's own Collider from This.GetComponent");
                Check(unitTypes.Count(type => type == nameof(InvokeMember)) >= 3, "Collection graph contains self-collider and notification member calls");
                Check(graph.graph.valueConnections.Count() + graph.graph.controlConnections.Count() >= 16, "Collection graph contains connected collection flow");
            }

            Check(RuntimeSourceHasNoCollectionCallbacks(), "No runtime C# trigger-based collection implementation detected");
            Check(QuitSourceHasEditorSafePath(), "Quit uses EditorApplication in the Editor and Application.Quit in players");
            Check(new[] { "ArchitectureDark.mat", "ArchitectureMetal.mat", "ArchitectureAccent.mat", "FloorBase.mat", "FloorPanel.mat", "FloorEmission.mat", "CrystalCore.mat", "CrystalShard.mat", "Pedestal.mat", "EnergyRing.mat", "PlayerBody.mat", "PlayerDark.mat", "PlayerEnergy.mat" }
                .All(name => File.Exists(MaterialsRootPath(name))), "V3 material library exists");

            var previousScenePath = SceneManager.GetActiveScene().path;
            if (mainGame != null)
            {
                var gameScene = EditorSceneManager.OpenScene(CrystalCollectorGenerator.MainGameScenePath, OpenSceneMode.Single);
                var crystals = UnityEngine.Object.FindObjectsByType<CollectiblePresentation>(FindObjectsSortMode.None);
                Check(crystals.Length == 8, "Exactly 8 gameplay crystals exist");
                Check(new[] { "CentralNexus", "North_ReactorCore", "East_ResearchLab", "South_CrystalVault", "West_Forge" }
                    .All(name => GameObject.Find(name) != null), "Central Nexus and all four named sectors exist");
                Check(UnityEngine.Object.FindFirstObjectByType<NexusPresentation>() != null, "Central Nexus has landmark presentation controller");
                var triggerCount = crystals.Count(crystal => crystal.GetComponent<Collider>() != null && crystal.GetComponent<Collider>().isTrigger);
                Check(triggerCount == 8, "All collectibles have trigger colliders");
                var machines = crystals.Select(crystal => crystal.GetComponent<ScriptMachine>()).ToArray();
                Check(machines.All(machine => machine != null), "All collectibles have Script Machines");
                Check(machines.All(machine => machine != null && machine.nest.macro == graph), "All collectibles reference CollectCrystal graph");
                Check(crystals.All(crystal => crystal.GetComponentInParent<PedestalPresentation>() != null), "Every gameplay crystal is mounted on a presentation-only pedestal");
                Check(UnityEngine.Object.FindObjectsByType<ScriptMachine>(FindObjectsSortMode.None).Length == 8, "Decorative environment has no extra Script Machines");
                var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
                Check(hud != null && hud.pauseGroup != null && hud.victoryGroup != null, "Gameplay HUD, pause panel, and victory panel exist");
                Check(hud != null && hud.resumeButton != null && hud.restartButton != null && hud.pauseMenuButton != null, "Pause buttons are assigned");
                Check(hud != null && hud.playAgainButton != null && hud.victoryMenuButton != null, "Victory buttons are assigned");
                Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Gameplay has exactly one EventSystem");
                Check(UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None).Length == 1, "Gameplay EventSystem uses InputSystemUIInputModule");
                Check(UnityEngine.Object.FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None).Length >= 1, "Gameplay Canvas has GraphicRaycaster");
                var gameplayTransition = UnityEngine.Object.FindFirstObjectByType<SceneTransition>();
                var gameplayOverlay = gameplayTransition == null ? null : gameplayTransition.GetComponentInChildren<CanvasGroup>(true);
                Check(gameplayTransition != null && gameplayOverlay != null && gameplayOverlay.blocksRaycasts, "Gameplay transition overlay starts deterministic and input-blocking");
                Check(UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Length >= 1, "Gameplay has generated global post-processing volume");
                Check(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).All(renderer => renderer.sharedMaterial != null), "Gameplay renderers have generated materials");
                Check(UnityEngine.Object.FindFirstObjectByType<Camera>() != null, "Gameplay camera exists");
                Check(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length >= 1, "Gameplay lighting exists");
                RestoreScene(previousScenePath, gameScene);
            }

            if (mainMenu != null)
            {
                var menuScene = EditorSceneManager.OpenScene(CrystalCollectorGenerator.MainMenuScenePath, OpenSceneMode.Single);
                var menu = UnityEngine.Object.FindFirstObjectByType<MenuController>();
                Check(menu != null && menu.playButton != null && menu.howToPlayButton != null && menu.quitButton != null, "Main Menu and How To Play controls exist");
                Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Main Menu has exactly one EventSystem");
                Check(UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None).Length == 1, "Main Menu EventSystem uses InputSystemUIInputModule");
                Check(UnityEngine.Object.FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None).Length >= 1, "Main Menu Canvas has GraphicRaycaster");
                Check(menu != null && menu.howToPlayGroup != null && menu.howToPlayBackButton != null, "How To Play panel and BACK button are assigned");
                Check(GameObject.Find("ShowcaseCrystal") != null && GameObject.Find("MenuWorld") != null, "Main Menu has its hero crystal and world backdrop");
                var menuTransition = UnityEngine.Object.FindFirstObjectByType<SceneTransition>();
                Check(menuTransition != null, "Main Menu transition overlay exists");
                Check(UnityEngine.Object.FindFirstObjectByType<Camera>() != null && UnityEngine.Object.FindFirstObjectByType<MenuCameraMotion>() != null, "Menu camera and cinematic motion exist");
                RestoreScene(previousScenePath, menuScene);
            }

            var buildScenes = EditorBuildSettings.scenes;
            Check(buildScenes.Length >= 2 && buildScenes[0].path == CrystalCollectorGenerator.MainMenuScenePath && buildScenes[0].enabled, "MainMenu is the first enabled build scene");
            Check(buildScenes.Length >= 2 && buildScenes[1].path == CrystalCollectorGenerator.MainGameScenePath && buildScenes[1].enabled, "MainGame is the second enabled build scene");
            var startScene = EditorSceneManager.playModeStartScene;
            Check(startScene != null && AssetDatabase.GetAssetPath(startScene) == CrystalCollectorGenerator.MainMenuScenePath, "Editor Play Mode Start Scene is MainMenu");

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

        private static bool QuitSourceHasEditorSafePath()
        {
            var path = "Assets/Scripts/Runtime/UI/MenuController.cs";
            if (!File.Exists(path)) return false;
            var source = File.ReadAllText(path);
            return source.Contains("#if UNITY_EDITOR") && source.Contains("UnityEditor.EditorApplication.isPlaying = false") && source.Contains("Application.Quit();");
        }

        private static string MaterialsRootPath(string fileName)
        {
            return CrystalCollectorGenerator.MaterialsRoot + "/" + fileName;
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
