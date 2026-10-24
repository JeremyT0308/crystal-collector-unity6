using System.Collections.Generic;
using System.IO;
using CrystalCollector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace CrystalCollector.EditorTools
{
    /// <summary>
    /// Deterministically generates the complete assignment game under Assets/Generated.
    /// </summary>
    public static class CrystalCollectorGenerator
    {
        public const string GeneratedRoot = "Assets/Generated";
        public const string ScenesRoot = GeneratedRoot + "/Scenes";
        public const string PrefabsRoot = GeneratedRoot + "/Prefabs";
        public const string MaterialsRoot = GeneratedRoot + "/Materials";
        public const string VolumeProfilesRoot = GeneratedRoot + "/VolumeProfiles";
        public const string MainMenuScenePath = ScenesRoot + "/MainMenu.unity";
        public const string MainGameScenePath = ScenesRoot + "/MainGame.unity";
        public const string PlayerPrefabPath = PrefabsRoot + "/Player.prefab";
        public const string CrystalPrefabPath = PrefabsRoot + "/Crystal.prefab";

        private static readonly Vector3[] CrystalPositions =
        {
            new Vector3(-6.5f, 0f, -6.2f), new Vector3(0f, 0f, -6.7f), new Vector3(6.5f, 0f, -5.4f),
            new Vector3(-6.8f, 0f, 0.5f), new Vector3(6.4f, 0f, 1.2f),
            new Vector3(-5.4f, 0f, 6.1f), new Vector3(0.4f, 0f, 6.7f), new Vector3(6.2f, 0f, 6.0f)
        };

        private static Material floorMaterial;
        private static Material wallMaterial;
        private static Material obstacleMaterial;
        private static Material crystalMaterial;
        private static Material crystalAccentMaterial;
        private static Material playerMaterial;
        private static Material playerAccentMaterial;
        private static Material ringMaterial;
        private static Material architectureMaterial;
        private static Material energyMaterial;
        private static Material particleMaterial;
        private static VolumeProfile volumeProfile;

        [MenuItem("Tools/Crystal Collector/Generate Complete Game", priority = 1)]
        public static void GenerateCompleteGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before generating Crystal Collector.");
                return;
            }

            ResetGeneratedContentInternal();
            EnsurePlayerTag();
            CreateFolders();
            CreateMaterials();

            var graph = VisualScriptingGraphGenerator.GenerateCollectionGraph();
            var playerPrefab = CreatePlayerPrefab();
            var crystalPrefab = CreateCrystalPrefab(graph);
            CreateMainMenuScene();
            CreateMainGameScene(playerPrefab, crystalPrefab);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            var mainMenuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            EditorSceneManager.playModeStartScene = mainMenuAsset;
            if (!Application.isBatchMode)
            {
                var mainMenuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                EditorSceneManager.SetActiveScene(mainMenuScene);
                EditorSceneManager.SaveScene(mainMenuScene);
            }
            Selection.activeObject = mainMenuAsset;
            Debug.Log("Crystal Collector generation complete. Run Tools > Crystal Collector > Validate Project.");
        }

        [MenuItem("Tools/Crystal Collector/Reset Generated Content", priority = 20)]
        public static void ResetGeneratedContent()
        {
            ResetGeneratedContentInternal();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Crystal Collector generated content reset.");
        }

        [MenuItem("Tools/Crystal Collector/Select Collection Graph", priority = 21)]
        public static void SelectCollectionGraph()
        {
            var graph = AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(VisualScriptingGraphGenerator.GraphPath);
            if (graph == null)
            {
                Debug.LogWarning("CollectCrystal graph does not exist yet. Generate the complete game first.");
                return;
            }

            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
            Debug.Log("CollectCrystal graph selected. Double-click the asset to open it in Visual Scripting.");
        }

        private static void ResetGeneratedContentInternal()
        {
            var activePath = SceneManager.GetActiveScene().path.Replace('\\', '/');
            if (activePath.StartsWith(GeneratedRoot + "/"))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            if (AssetDatabase.IsValidFolder(GeneratedRoot))
            {
                AssetDatabase.DeleteAsset(GeneratedRoot);
            }
        }

        private static void CreateFolders()
        {
            GeneratorUtilities.EnsureFolder(GeneratedRoot);
            GeneratorUtilities.EnsureFolder(ScenesRoot);
            GeneratorUtilities.EnsureFolder(PrefabsRoot);
            GeneratorUtilities.EnsureFolder(MaterialsRoot);
            GeneratorUtilities.EnsureFolder(VolumeProfilesRoot);
            GeneratorUtilities.EnsureFolder(GeneratedRoot + "/VisualScripting");
        }

        private static void CreateMaterials()
        {
            floorMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/Floor.mat", new Color(0.035f, 0.055f, 0.1f), 0.25f, 0.82f, new Color(0.02f, 0.07f, 0.16f), 0.5f);
            wallMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/Walls.mat", new Color(0.07f, 0.1f, 0.17f), 0.45f, 0.68f, new Color(0.05f, 0.08f, 0.16f), 0.6f);
            obstacleMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/Obstacles.mat", new Color(0.12f, 0.16f, 0.25f), 0.72f, 0.78f, new Color(0.05f, 0.16f, 0.28f), 0.7f);
            crystalMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/Crystal.mat", new Color(0.2f, 0.82f, 1f), 0.08f, 0.92f, new Color(0.05f, 0.78f, 1f), 3.2f);
            crystalAccentMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/CrystalAccent.mat", new Color(0.55f, 0.2f, 1f), 0.1f, 0.9f, new Color(0.46f, 0.1f, 1f), 3.8f);
            playerMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/Player.mat", new Color(0.98f, 0.58f, 0.22f), 0.18f, 0.86f, new Color(1f, 0.2f, 0.04f), 1.8f);
            playerAccentMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/PlayerAccent.mat", new Color(0.3f, 0.92f, 1f), 0.15f, 0.9f, new Color(0.1f, 0.7f, 1f), 2.2f);
            ringMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/GroundRing.mat", new Color(0.18f, 0.52f, 0.75f), 0.25f, 0.8f, new Color(0.05f, 0.45f, 0.9f), 2.6f);
            architectureMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/ArchitectureMetal.mat", new Color(0.035f, 0.075f, 0.14f), 0.82f, 0.9f, new Color(0.015f, 0.08f, 0.16f), 0.75f);
            energyMaterial = GeneratorUtilities.CreateMaterial(MaterialsRoot + "/EnergyAccent.mat", new Color(0.05f, 0.42f, 0.65f), 0.15f, 0.82f, new Color(0.05f, 0.8f, 1f), 3.8f);
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            particleMaterial = new Material(particleShader) { name = "GeneratedParticles" };
            if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", new Color(0.15f, 0.78f, 1f, 0.9f));
            if (particleMaterial.HasProperty("_Color")) particleMaterial.SetColor("_Color", new Color(0.15f, 0.78f, 1f, 0.9f));
            AssetDatabase.CreateAsset(particleMaterial, MaterialsRoot + "/Particles.mat");
            CreatePostProcessProfile();
        }

        private static GameObject CreatePlayerPrefab()
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            var body = GeneratorUtilities.CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 0.95f, 0.9f), playerMaterial);
            var core = GeneratorUtilities.CreatePrimitive(PrimitiveType.Sphere, "HeadOrCore", root.transform, new Vector3(0f, 1.95f, 0.05f), new Vector3(0.5f, 0.5f, 0.5f), playerAccentMaterial);
            var indicator = GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "DirectionIndicator", root.transform, new Vector3(0f, 1.05f, 0.5f), new Vector3(0.16f, 0.12f, 0.5f), playerAccentMaterial);
            var marker = GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "GroundMarker", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.82f, 0.015f, 0.82f), ringMaterial);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "HoverRing", root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.62f, 0.025f, 0.62f), energyMaterial);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "SideModuleLeft", root.transform, new Vector3(-0.58f, 1.1f, 0f), new Vector3(0.16f, 0.34f, 0.48f), playerAccentMaterial);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "SideModuleRight", root.transform, new Vector3(0.58f, 1.1f, 0f), new Vector3(0.16f, 0.34f, 0.48f), playerAccentMaterial);
            var trailObject = new GameObject("Trail");
            trailObject.transform.SetParent(root.transform, false);
            trailObject.transform.localPosition = new Vector3(0f, 0.35f, -0.35f);
            var trail = trailObject.AddComponent<TrailRenderer>();
            trail.time = 0.32f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.01f;
            trail.minVertexDistance = 0.08f;
            trail.sharedMaterial = playerAccentMaterial;
            trail.startColor = new Color(0.25f, 0.9f, 1f, 0.75f);
            trail.endColor = new Color(0.2f, 0.5f, 1f, 0f);

            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.height = 2f;
            collider.radius = 0.45f;
            var bodyPhysics = root.AddComponent<Rigidbody>();
            bodyPhysics.useGravity = false;
            bodyPhysics.constraints = RigidbodyConstraints.FreezeRotation;
            bodyPhysics.interpolation = RigidbodyInterpolation.Interpolate;
            var movement = root.AddComponent<PlayerMovement>();
            var presentation = root.AddComponent<PlayerPresentation>();
            presentation.Configure(movement, body.transform, core.transform);
            AddParticleSystem(root.transform, "ThrusterParticles", new Color(0.18f, 0.8f, 1f), true, 5f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateCrystalPrefab(ScriptGraphAsset graph)
        {
            var root = new GameObject("Crystal");
            var core = GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "Core", root.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.42f, 1.25f, 0.42f), crystalMaterial);
            core.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            var shardA = GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "ShardA", root.transform, new Vector3(-0.38f, 0.95f, 0.03f), new Vector3(0.18f, 0.75f, 0.18f), crystalAccentMaterial);
            shardA.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            var shardB = GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "ShardB", root.transform, new Vector3(0.36f, 1.22f, 0.05f), new Vector3(0.16f, 0.88f, 0.16f), crystalMaterial);
            shardB.transform.localRotation = Quaternion.Euler(0f, 0f, 42f);
            var shardC = GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "ShardC", root.transform, new Vector3(0.05f, 1.95f, -0.02f), new Vector3(0.13f, 0.58f, 0.13f), crystalAccentMaterial);
            shardC.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Sphere, "GlowPart", root.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.72f, 0.72f, 0.72f), crystalMaterial);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "GroundRing", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.75f, 0.018f, 0.75f), ringMaterial);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "OrbitRingA", root.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.92f, 0.012f, 0.92f), crystalAccentMaterial).transform.localRotation = Quaternion.Euler(70f, 0f, 25f);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "OrbitRingB", root.transform, new Vector3(0f, 1.1f, 0f), new Vector3(1.08f, 0.009f, 1.08f), crystalMaterial).transform.localRotation = Quaternion.Euler(70f, 0f, -25f);
            AddParticleSystem(root.transform, "PickupParticles", new Color(0.2f, 0.82f, 1f), true, 7f);

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.15f, 0f);
            trigger.size = new Vector3(1.15f, 2.65f, 1.15f);
            var machine = root.AddComponent<ScriptMachine>();
            machine.nest.SwitchToMacro(graph);
            var variables = root.GetComponent<Variables>();
            variables.declarations.Set("Collected", false);
            root.AddComponent<CollectiblePresentation>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CrystalPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("MenuWorld");
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "Platform", world.transform, new Vector3(0f, -0.55f, 0f), new Vector3(22f, 0.5f, 22f), floorMaterial, true);
            var showcase = new GameObject("ShowcaseCrystal");
            showcase.transform.SetParent(world.transform, false);
            showcase.transform.localPosition = new Vector3(0f, 1.1f, 0.2f);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "Core", showcase.transform, new Vector3(0f, 1.3f, 0f), new Vector3(1.25f, 3.7f, 1.25f), crystalMaterial).transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "ShardA", showcase.transform, new Vector3(-0.95f, 1.1f, 0.1f), new Vector3(0.34f, 2f, 0.34f), crystalAccentMaterial).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "ShardB", showcase.transform, new Vector3(0.9f, 1.5f, 0.08f), new Vector3(0.26f, 2.4f, 0.26f), crystalMaterial).transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "Ring", showcase.transform, new Vector3(0f, -0.8f, 0f), new Vector3(2.15f, 0.03f, 2.15f), ringMaterial);
            showcase.AddComponent<WorldAnimator>();
            AddParticleSystem(showcase.transform, "HeroEnergy", new Color(0.22f, 0.78f, 1f), true, 8f);

            for (var i = 0; i < 9; i++)
            {
                var fragment = GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "FloatingFragment_" + (i + 1), world.transform, new Vector3(-7.4f + i * 1.85f, 0.7f + (i % 3) * 0.65f, 3.8f + (i % 2) * 0.9f), new Vector3(0.12f + (i % 2) * 0.08f, 0.55f + (i % 3) * 0.18f, 0.12f + (i % 2) * 0.08f), i % 2 == 0 ? crystalMaterial : crystalAccentMaterial);
                fragment.transform.localRotation = Quaternion.Euler(0f, i * 27f, (i % 3) * 18f);
                fragment.AddComponent<WorldAnimator>();
            }

            AddDirectionalLight(world.transform, new Vector3(38f, -28f, 18f), new Color(0.56f, 0.74f, 1f), 1.2f);
            AddPointLight(world.transform, new Vector3(0f, 4.5f, 0f), new Color(0.16f, 0.64f, 1f), 9f, 13f);
            AddPointLight(world.transform, new Vector3(-6f, 2.5f, 3f), new Color(0.5f, 0.12f, 1f), 5f, 8f);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.position = new Vector3(0f, 6.4f, -10.8f);
            GeneratorUtilities.SetLookAt(cameraObject.transform, new Vector3(0f, 1.2f, 0.5f));
            cameraObject.GetComponent<Camera>().fieldOfView = 48f;
            cameraObject.AddComponent<MenuCameraMotion>();
            ConfigureCameraQuality(cameraObject.GetComponent<Camera>());
            CreateGlobalVolume(world.transform);

            var canvas = GeneratorUtilities.CreateCanvas("MainMenuCanvas", out _);
            var content = CreateStretchRoot("MenuContent", canvas.transform);
            var menuGroup = GeneratorUtilities.AddCanvasGroup(content, true);
            var title = GeneratorUtilities.CreateText("Title", content.transform, "CRYSTAL\nCOLLECTOR", 72, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(900f, 240f));
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.85f, 0.96f, 1f);
            var subtitle = GeneratorUtilities.CreateText("Subtitle", content.transform, "A luminous hunt through the midnight arena", 24, new Color(0.66f, 0.78f, 0.9f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(820f, 60f));
            subtitle.fontStyle = FontStyle.Italic;
            var playButton = GeneratorUtilities.CreateButton("PlayButton", content.transform, "PLAY", new Vector2(0f, -35f), new Vector2(320f, 68f), new Color(0.1f, 0.82f, 1f));
            var howButton = GeneratorUtilities.CreateButton("HowToPlayButton", content.transform, "HOW TO PLAY", new Vector2(0f, -120f), new Vector2(320f, 68f), new Color(0.55f, 0.3f, 1f));
            var quitButton = GeneratorUtilities.CreateButton("QuitButton", content.transform, "QUIT", new Vector2(0f, -205f), new Vector2(320f, 68f), new Color(1f, 0.36f, 0.35f));

            var howPanelImage = GeneratorUtilities.CreateImage("HowToPlayPanel", content.transform, new Color(0.035f, 0.06f, 0.12f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 520f));
            var howPanel = howPanelImage.rectTransform;
            var howGroup = GeneratorUtilities.AddCanvasGroup(howPanelImage.gameObject, false);
            GeneratorUtilities.CreateText("HowTitle", howPanel, "HOW TO PLAY", 38, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(640f, 60f));
            GeneratorUtilities.CreateText("HowBody", howPanel, "MOVE\nWASD / ARROW KEYS\n\nOBJECTIVE\nCollect all 8 crystals\n\nPAUSE\nESC", 26, new Color(0.76f, 0.86f, 0.96f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(620f, 290f));
            var howBack = GeneratorUtilities.CreateButton("HowBackButton", howPanel, "BACK", new Vector2(0f, -190f), new Vector2(260f, 60f), new Color(0.1f, 0.82f, 1f));

            var systems = new GameObject("MenuSystems");
            var audio = systems.AddComponent<ProceduralAudio>();
            var controller = systems.AddComponent<MenuController>();
            controller.playButton = playButton;
            controller.howToPlayButton = howButton;
            controller.quitButton = quitButton;
            controller.howToPlayBackButton = howBack;
            controller.menuGroup = menuGroup;
            controller.howToPlayGroup = howGroup;
            controller.howToPlayPanel = howPanel;
            controller.titleTransform = title.rectTransform;
            controller.audio = audio;

            var transition = AddTransition(canvas.transform);
            controller.transition = transition;
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateMainGameScene(GameObject playerPrefab, GameObject crystalPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("Arena");
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "Floor", world.transform, new Vector3(0f, -0.35f, 0f), new Vector3(19f, 0.7f, 19f), floorMaterial, true);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "NorthBoundary", world.transform, new Vector3(0f, 0.7f, 9.2f), new Vector3(19f, 1.8f, 0.55f), wallMaterial, true);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "SouthBoundary", world.transform, new Vector3(0f, 0.7f, -9.2f), new Vector3(19f, 1.8f, 0.55f), wallMaterial, true);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "EastBoundary", world.transform, new Vector3(9.2f, 0.7f, 0f), new Vector3(0.55f, 1.8f, 19f), wallMaterial, true);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cube, "WestBoundary", world.transform, new Vector3(-9.2f, 0.7f, 0f), new Vector3(0.55f, 1.8f, 19f), wallMaterial, true);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "CentralNexus", world.transform, new Vector3(0f, 0.08f, 0f), new Vector3(3.1f, 0.08f, 3.1f), architectureMaterial);
            GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "CentralEnergyRing", world.transform, new Vector3(0f, 0.2f, 0f), new Vector3(2.55f, 0.018f, 2.55f), energyMaterial).AddComponent<WorldAnimator>();
            var sectorPositions = new[] { new Vector3(-6.5f, 0.05f, -6.2f), new Vector3(6.5f, 0.05f, -5.4f), new Vector3(-5.4f, 0.05f, 6.1f), new Vector3(6.2f, 0.05f, 6f) };
            for (var i = 0; i < sectorPositions.Length; i++)
            {
                GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "SectorPlatform_" + (i + 1), world.transform, sectorPositions[i], new Vector3(2.1f, 0.07f, 2.1f), architectureMaterial);
                GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "SectorAccent_" + (i + 1), world.transform, sectorPositions[i] + Vector3.up * 0.11f, new Vector3(1.7f, 0.015f, 1.7f), energyMaterial);
            }

            var obstaclePositions = new[]
            {
                new Vector3(-3.2f, 0.6f, -2.4f), new Vector3(3.5f, 0.6f, -2.3f), new Vector3(-2.5f, 0.6f, 3.0f), new Vector3(3.0f, 0.6f, 3.3f),
                new Vector3(0f, 0.9f, -4.2f), new Vector3(0f, 0.9f, 4.4f)
            };
            for (var i = 0; i < obstaclePositions.Length; i++)
            {
                var obstacle = GeneratorUtilities.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder, "Obstacle_" + (i + 1), world.transform, obstaclePositions[i], i % 2 == 0 ? new Vector3(1.25f, 1.2f, 1.25f) : new Vector3(1f, 1.8f, 1f), obstacleMaterial, true);
                obstacle.transform.localRotation = Quaternion.Euler(0f, i * 17f, 0f);
            }

            for (var i = 0; i < 6; i++)
            {
                var column = GeneratorUtilities.CreatePrimitive(PrimitiveType.Cylinder, "AccentColumn_" + (i + 1), world.transform, new Vector3(-7.6f + (i % 3) * 7.6f, 0.8f, i < 3 ? 7.5f : -7.5f), new Vector3(0.18f, 0.8f, 0.18f), crystalAccentMaterial);
                column.AddComponent<WorldAnimator>();
            }

            AddDirectionalLight(world.transform, new Vector3(48f, -34f, 24f), new Color(0.6f, 0.75f, 1f), 1.3f);
            AddPointLight(world.transform, new Vector3(-5f, 3.5f, -5f), new Color(0.1f, 0.55f, 1f), 7f, 9f);
            AddPointLight(world.transform, new Vector3(5f, 3.5f, 5f), new Color(0.5f, 0.12f, 1f), 7f, 9f);
            CreateGlobalVolume(world.transform);

            var variablesObject = new GameObject("Scene Variables");
            var sceneVariables = variablesObject.AddComponent<SceneVariables>();
            sceneVariables.variables.declarations.Set("CollectedCount", 0f);
            sceneVariables.variables.declarations.Set("TotalCrystals", 8f);

            var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0f, 0f);
            for (var i = 0; i < CrystalPositions.Length; i++)
            {
                var crystal = PrefabUtility.InstantiatePrefab(crystalPrefab) as GameObject;
                crystal.name = $"Crystal_{i + 1:00}";
                crystal.transform.position = CrystalPositions[i];
                crystal.transform.SetParent(world.transform, true);
            }

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
            cameraObject.transform.position = new Vector3(0f, 12f, -12f);
            cameraObject.GetComponent<Camera>().fieldOfView = 52f;
            ConfigureCameraQuality(cameraObject.GetComponent<Camera>());
            var cameraFollow = cameraObject.GetComponent<CameraFollow>();
            cameraFollow.SetTarget(player.transform);
            GeneratorUtilities.SetLookAt(cameraObject.transform, player.transform.position + Vector3.up * 0.6f);

            var canvas = GeneratorUtilities.CreateCanvas("GameplayCanvas", out _);
            var hudRoot = CreateStretchRoot("HUD", canvas.transform);
            var gameHud = hudRoot.AddComponent<GameHUD>();
            var crystalCount = GeneratorUtilities.CreateText("CrystalCount", hudRoot.transform, "CRYSTALS  0 / 8", 26, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f, -54f), new Vector2(420f, 62f));
            crystalCount.fontStyle = FontStyle.Bold;
            var indicators = new Image[8];
            for (var i = 0; i < indicators.Length; i++)
            {
                indicators[i] = GeneratorUtilities.CreateImage("CrystalIndicator_" + (i + 1), hudRoot.transform, new Color(0.18f, 0.3f, 0.46f, 0.45f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(62f + i * 34f, -98f), new Vector2(22f, 22f));
                indicators[i].raycastTarget = false;
            }
            var timer = GeneratorUtilities.CreateText("Timer", hudRoot.transform, "TIME  00:00.0", 26, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-54f, -54f), new Vector2(420f, 62f));
            var controls = GeneratorUtilities.CreateText("ControlsHint", hudRoot.transform, "WASD / ARROWS TO MOVE     •     ESC TO PAUSE", 20, new Color(0.56f, 0.68f, 0.8f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(800f, 42f));
            var centerObject = new GameObject("CenterMessage", typeof(RectTransform));
            centerObject.transform.SetParent(hudRoot.transform, false);
            var centerRect = centerObject.GetComponent<RectTransform>();
            GeneratorUtilities.SetRect(centerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 120f));
            var centerGroup = GeneratorUtilities.AddCanvasGroup(centerObject, false);
            var centerText = GeneratorUtilities.CreateText("Message", centerObject.transform, "READY", 64, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            centerText.rectTransform.offsetMin = Vector2.zero;
            centerText.rectTransform.offsetMax = Vector2.zero;
            centerText.fontStyle = FontStyle.Bold;
            var pickupObject = new GameObject("PickupMessage", typeof(RectTransform));
            pickupObject.transform.SetParent(hudRoot.transform, false);
            var pickupRect = pickupObject.GetComponent<RectTransform>();
            GeneratorUtilities.SetRect(pickupRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(600f, 80f));
            var pickupGroup = GeneratorUtilities.AddCanvasGroup(pickupObject, false);
            var pickupText = GeneratorUtilities.CreateText("PickupText", pickupObject.transform, "+1 CRYSTAL", 32, new Color(0.3f, 0.9f, 1f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            pickupText.rectTransform.offsetMin = Vector2.zero;
            pickupText.rectTransform.offsetMax = Vector2.zero;

            var pauseImage = GeneratorUtilities.CreateImage("PausePanel", hudRoot.transform, new Color(0.025f, 0.04f, 0.1f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 520f));
            var pauseGroup = GeneratorUtilities.AddCanvasGroup(pauseImage.gameObject, false);
            var pausePanel = pauseImage.rectTransform;
            GeneratorUtilities.CreateText("PauseTitle", pausePanel, "PAUSED", 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(500f, 70f));
            var resumeButton = GeneratorUtilities.CreateButton("ResumeButton", pausePanel, "RESUME", new Vector2(0f, 60f), new Vector2(300f, 62f), new Color(0.1f, 0.82f, 1f));
            var restartButton = GeneratorUtilities.CreateButton("RestartButton", pausePanel, "RESTART", new Vector2(0f, -20f), new Vector2(300f, 62f), new Color(0.55f, 0.3f, 1f));
            var pauseMenuButton = GeneratorUtilities.CreateButton("PauseMenuButton", pausePanel, "MAIN MENU", new Vector2(0f, -100f), new Vector2(300f, 62f), new Color(1f, 0.38f, 0.32f));

            var victoryImage = GeneratorUtilities.CreateImage("VictoryPanel", hudRoot.transform, new Color(0.025f, 0.05f, 0.13f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 570f));
            var victoryGroup = GeneratorUtilities.AddCanvasGroup(victoryImage.gameObject, false);
            var victoryPanel = victoryImage.rectTransform;
            GeneratorUtilities.CreateText("VictoryTitle", victoryPanel, "LEVEL COMPLETE", 44, new Color(0.78f, 0.96f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 175f), new Vector2(640f, 70f));
            var victoryCount = GeneratorUtilities.CreateText("VictoryCount", victoryPanel, "8 / 8 CRYSTALS", 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(600f, 52f));
            var victoryTime = GeneratorUtilities.CreateText("VictoryTime", victoryPanel, "TIME: 00.0 s", 24, new Color(0.64f, 0.8f, 0.94f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(600f, 52f));
            var playAgainButton = GeneratorUtilities.CreateButton("PlayAgainButton", victoryPanel, "PLAY AGAIN", new Vector2(0f, -60f), new Vector2(310f, 62f), new Color(0.1f, 0.82f, 1f));
            var victoryMenuButton = GeneratorUtilities.CreateButton("VictoryMenuButton", victoryPanel, "MAIN MENU", new Vector2(0f, -140f), new Vector2(310f, 62f), new Color(1f, 0.38f, 0.32f));

            var transition = AddTransition(canvas.transform);
            var systems = new GameObject("GameSystems");
            var audio = systems.AddComponent<ProceduralAudio>();
            var flow = systems.AddComponent<GameFlowController>();
            flow.player = player.GetComponent<PlayerMovement>();
            flow.hud = gameHud;
            flow.transition = transition;
            flow.audio = audio;
            gameHud.crystalCountText = crystalCount;
            gameHud.crystalIndicators = indicators;
            gameHud.timerText = timer;
            gameHud.centerMessageText = centerText;
            gameHud.pickupMessageText = pickupText;
            gameHud.centerMessageGroup = centerGroup;
            gameHud.pickupMessageGroup = pickupGroup;
            gameHud.crystalCounterTransform = crystalCount.rectTransform;
            gameHud.pickupMessageTransform = pickupRect;
            gameHud.pauseGroup = pauseGroup;
            gameHud.pausePanel = pausePanel;
            gameHud.victoryGroup = victoryGroup;
            gameHud.victoryPanel = victoryPanel;
            gameHud.victoryCountText = victoryCount;
            gameHud.victoryTimeText = victoryTime;
            gameHud.resumeButton = resumeButton;
            gameHud.restartButton = restartButton;
            gameHud.pauseMenuButton = pauseMenuButton;
            gameHud.playAgainButton = playAgainButton;
            gameHud.victoryMenuButton = victoryMenuButton;

            EditorSceneManager.SaveScene(scene, MainGameScenePath);
        }

        private static SceneTransition AddTransition(Transform canvas)
        {
            var transitionObject = new GameObject("SceneTransition");
            transitionObject.transform.SetParent(canvas, false);
            var overlay = GeneratorUtilities.CreateImage("FadeOverlay", transitionObject.transform, Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.rectTransform.offsetMin = Vector2.zero;
            overlay.rectTransform.offsetMax = Vector2.zero;
            var group = GeneratorUtilities.AddCanvasGroup(overlay.gameObject, true);
            var transition = transitionObject.AddComponent<SceneTransition>();
            transition.Configure(group);
            return transition;
        }

        private static GameObject CreateStretchRoot(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            GeneratorUtilities.SetRect(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return root;
        }

        private static void CreatePostProcessProfile()
        {
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volumeProfile.name = "CrystalCollectorGlobalProfile";
            var bloom = volumeProfile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.65f);
            bloom.scatter.Override(0.72f);
            var tonemapping = volumeProfile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            var color = volumeProfile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.12f);
            color.contrast.Override(10f);
            color.saturation.Override(8f);
            var vignette = volumeProfile.Add<Vignette>(true);
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.72f);
            AssetDatabase.CreateAsset(volumeProfile, VolumeProfilesRoot + "/CrystalCollectorGlobalProfile.asset");
        }

        private static void CreateGlobalVolume(Transform parent)
        {
            var volumeObject = new GameObject("Global Volume", typeof(Volume));
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = volumeProfile;
        }

        private static void ConfigureCameraQuality(Camera camera)
        {
            if (camera == null) return;
            var additional = camera.GetComponent<UniversalAdditionalCameraData>() ?? camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            additional.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        private static ParticleSystem AddParticleSystem(Transform parent, string name, Color color, bool loop, float emissionRate)
        {
            var particleObject = new GameObject(name, typeof(ParticleSystem), typeof(ParticleSystemRenderer));
            particleObject.transform.SetParent(parent, false);
            var system = particleObject.GetComponent<ParticleSystem>();
            var main = system.main;
            main.loop = loop;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.08f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.55f;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = particleMaterial;
            return system;
        }

        private static void AddDirectionalLight(Transform parent, Vector3 rotation, Color color, float intensity)
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localEulerAngles = rotation;
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }

        private static void AddPointLight(Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject("Accent Point Light", typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(MainGameScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsurePlayerTag()
        {
            var tags = new List<string>(InternalEditorUtility.tags);
            if (!tags.Contains("Player")) InternalEditorUtility.AddTag("Player");
        }
    }
}
