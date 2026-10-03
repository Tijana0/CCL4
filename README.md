# CCL4

CCL4 is a collaborative Unity project with two local player controllers, themed rooms, timed orders, and score based progression. The repository contains the Unity project, C# gameplay scripts, scenes, artwork, and a Wwise project.

## What is in the project

- A start screen, hub, and two playable room scenes in the enabled build sequence.
- Local player movement, interaction, item handoff, and controller input in `SimplePlayerController.cs`.
- Room configuration that supplies available items, recipes, and station rules.
- An order system that generates requests and awards points for completed items.
- Timed rounds, report cards, stars, and level unlocking.
- Wwise integration for music and sound effects.

These are features visible in the source. This README does not claim that a packaged build or every scene has been tested.

## Structure

```text
Assets/_Project/Scripts/       Gameplay, UI, room, and audio scripts
Assets/_Project/Scenes/        Game scenes and development scenes
Assets/Wwise/                 Unity Wwise integration
CCL4_WwiseProject/            Wwise authoring project
Packages/                    Unity package dependencies
ProjectSettings/             Unity editor and build settings
```

## Open the project

1. Install Unity **6000.3.6f1**, the version recorded in `ProjectSettings/ProjectVersion.txt`.
2. Open the repository folder as a Unity project.
3. Open `Assets/_Project/Scenes/DevScenes/Bootstrap.unity`. This is the first enabled scene in the build settings.
4. Check the Wwise integration and sound banks if audio is missing.

The full repository is large and includes third party packages and art assets. Allow extra time for the first Unity import.

## Team and attribution

This is a team repository. Git history records work from multiple contributors. For a specific example of my work, [this commit](https://github.com/Tijana0/CCL4/commit/03c037669c45a0f0a9ef8cd508304d5f0fa05f5a) by Tijana0 updates controller and UI handling in `GameManager`, `HubManager`, `InstantStation`, and `StartScreen`. Git history alone does not establish complete ownership of each system.

The repository also contains third party Unity and Wwise content. Refer to the licenses and attribution supplied with those assets before redistributing them. No blanket license for the complete repository is stated here.
