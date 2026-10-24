# Crystal Collector - Unity 6 URP

## Assignment Objective

Crystal Collector demonstrates a clear separation between traditional C# scripting and Unity Visual Scripting in a small playable 3D game. C# owns movement, camera, game flow, UI, and presentation. Visual Scripting owns the actual crystal collection decision.

## Game Description

Explore a compact neon arena and collect all eight crystals. Obstacles create readable routes through the space, while the HUD tracks progress and elapsed time. When the last crystal is accepted by the Visual Scripting graph, the game presents a celebratory completion panel.

## Technologies

- Unity 6 `6000.3.22f1`
- Universal Render Pipeline `17.3.0`
- Unity Input System `1.20.0`
- Unity Visual Scripting `1.9.12`
- UGUI
- Traditional C# MonoBehaviours

## Controls

- Move: `WASD` or Arrow Keys
- Pause / Resume: `ESC`
- Menu buttons: mouse or keyboard UI navigation

## Gameplay

1. Select `PLAY` from the generated main menu.
2. Wait for the short `READY` / `GO!` introduction.
3. Move through the arena and touch all eight crystals.
4. Use `ESC` to pause, restart, or return to the menu.
5. The victory panel shows the final count and time.

## Traditional C# Implementation

### PlayerMovement.cs

`PlayerMovement` reads the installed Input System keyboard device, normalizes the input vector to prevent faster diagonal movement, and applies smoothed movement through a Rigidbody in `FixedUpdate`. Input is disabled by the game-flow state during countdown, pause, and victory.

### Other C# responsibilities

- `CameraFollow.cs`: smooth elevated camera follow with movement look-ahead.
- `GameFlowController.cs`: countdown, game states, timer, pause, restart, victory presentation, and scene transitions.
- `GameHUD.cs` / `MenuController.cs` / `UIAnimator.cs`: UI presentation and animation.
- `CollectiblePresentation.cs` / `WorldAnimator.cs` / `PlayerPresentation.cs`: visual-only motion and pickup feedback.
- `ProceduralAudio.cs`: optional generated tones created at runtime with `AudioClip.Create`.

## Visual Scripting Implementation

### Crystal Collection

The generated `Assets/Generated/VisualScripting/CollectCrystal.asset` graph begins with the installed `OnTriggerEnter` unit. It reads the other Collider, obtains its GameObject, calls `CompareTag("Player")`, branches, checks the crystal Object variable `Collected`, marks it immediately, disables the trigger Collider, reads Scene variable `CollectedCount`, adds one, writes the new count back through Visual Scripting, and calls presentation bridges. It then compares the new value against Scene variable `TotalCrystals` and invokes the victory bridge when the threshold is reached.

The runtime presentation bridge can animate and update UI, but it never detects a player, increments the score, or decides whether a collection is valid.

## Why the collection logic is Visual Scripting

The assignment requires collision/trigger detection and collection decisions to be visible as a node graph. The graph makes the event flow easy to demonstrate in a screenshot and keeps the boundary explicit: C# is used for systems that benefit from code structure, while the interaction rule is authored visually.

## Generator

Open the project and run:

`Tools > Crystal Collector > Generate Complete Game`

Generation is deterministic and idempotent. It removes and recreates only `Assets/Generated`, so running it again does not create duplicate players, canvases, crystals, scenes, prefabs, or materials.

Additional commands:

- `Tools > Crystal Collector > Validate Project`
- `Tools > Crystal Collector > Reset Generated Content`
- `Tools > Crystal Collector > Select Collection Graph`

## C# vs Visual Scripting

### C# Advantages

- Scales well as systems become more complex.
- Strong IDE tooling, navigation, refactoring, and compile-time feedback.
- Easier to organize into focused files and reusable APIs.
- A natural fit for movement, state, UI, and presentation systems.

### C# Disadvantages

- Requires programming knowledge and familiarity with Unity lifecycle rules.
- The event flow is less immediately visual for beginners or non-programmers.
- Small interactions can require more setup code than a graph.

### Visual Scripting Advantages

- The event sequence can be read visually from trigger to outcome.
- Useful for rapid prototyping and straightforward interactions.
- Accessible to designers and students learning gameplay logic.
- Makes the assignment's collection responsibility easy to inspect.

### Visual Scripting Disadvantages

- Large graphs become difficult to manage and navigate.
- Dense connections can reduce readability and make refactoring slower.
- Complex systems are usually easier to maintain in C# with source control and IDE tooling.

## Challenges and Solutions

- **No imported assets:** the generator uses primitive meshes, generated URP materials, lights, animation, and procedural audio.
- **Reliable graph generation:** the editor script targets the actual Visual Scripting 1.9.12 APIs installed in the project rather than relying on handwritten graph YAML.
- **Repeatable setup:** all scene, prefab, UI, material, graph, tag, and build-setting wiring is performed by the generator.
- **Clear separation:** the validator scans runtime source for trigger/collision callbacks and inspects the generated graph for the required unit families.

## Project Structure

```text
Assets/Scripts/
├── Runtime/
│   ├── Player/PlayerMovement.cs
│   ├── Camera/CameraFollow.cs
│   ├── Game/GameFlowController.cs
│   ├── Game/SceneTransition.cs
│   ├── UI/GameHUD.cs
│   ├── UI/MenuController.cs
│   ├── UI/UIAnimator.cs
│   ├── Presentation/CollectiblePresentation.cs
│   ├── Presentation/PlayerPresentation.cs
│   ├── Presentation/WorldAnimator.cs
│   └── Audio/ProceduralAudio.cs
├── Editor/
│   ├── CrystalCollectorGenerator.cs
│   ├── VisualScriptingGraphGenerator.cs
│   ├── GeneratorUtilities.cs
│   ├── ProjectValidator.cs
│   └── CrystalCollectorBatch.cs
├── Docs/README.md
├── README.md
└── .gitignore
```

Generated assets are kept separately under `Assets/Generated`:

```text
Scenes/MainMenu.unity
Scenes/MainGame.unity
Prefabs/Player.prefab
Prefabs/Crystal.prefab
Materials/*.mat
VisualScripting/CollectCrystal.asset
```

## How to Run

1. Open the Unity project with Unity `6000.3.22f1`.
2. Run `Tools > Crystal Collector > Generate Complete Game`.
3. Press Play; `MainMenu` is the first enabled build scene.

## How to Validate

Run `Tools > Crystal Collector > Validate Project`. The validator checks the Unity/package setup, generated scenes and prefabs, eight trigger crystals, Script Machines and shared graph, graph unit evidence, UI panels, camera/lighting, build order, and the C# / Visual Scripting separation.

## Visual Scripting Screenshot

Run `Tools > Crystal Collector > Select Collection Graph`, then open `CollectCrystal.asset` in the Visual Scripting editor and capture the readable graph for the university submission.
