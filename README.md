# Mischief Managed

Mischief Managed is a local multiplayer student game built in **Unity 6** with **C#**. The project was developed during Creative Code Lab 4 as a multidisciplinary team project focused on fast-paced cooperative gameplay, reusable gameplay systems, and integrating code, 3D art, and audio into a complete prototype.

The game uses a wizarding-school theme and revolves around completing timed tasks, processing items at different stations, fulfilling orders, and carrying progress between levels.

## Project context

This project was created during a two-week Creative Code Lab at the University of Applied Sciences St. Pölten.

I served as the **main developer**, working alongside a second developer, a 3D artist, and an audio teammate. My role focused on implementing and integrating the core gameplay systems and helping bring the different parts of the project together.

Because this is a shared team repository, not every file in the project was authored by me.

## Gameplay systems

The project includes systems for:

- Local multiplayer gameplay
- Player interaction with world objects and stations
- Item pickup, placement, processing, and combination
- Configurable station behavior
- Timed order and hand-in workflows
- Room-specific recipes and item rules
- Persistent stars and progression between scenes
- UI and pause handling for multiplayer play
- Integrated audio through Wwise

## Tech stack

- **Unity 6000.3.6f1**
- **C#**
- **Universal Render Pipeline**
- **Unity Input System**
- **Wwise Unity Integration**
- **Git**

## System architecture

The gameplay code is organized around reusable systems rather than hard-coding every interaction into individual scenes.

```text
RoomConfig
    |
    v
RoomManager
    |
    +------> configures StationBase instances
    |
    +------> registers room recipes
    |
    v
OrderManager

Player interaction
    |
    v
StationBase
    |
    +------> ProcessingStation
    +------> ItemContainerStation
    +------> MultiIngredientStation
    +------> InstantStation
    +------> Counter

Level result
    |
    v
PersistentGameState
    |
    +------> stars
    +------> progression
    +------> hub / scene flow
```

### Room configuration

`RoomManager` reads room configuration data when a level starts. It applies item rules to station instances, registers recipes with the order system, and prepares dispensers and other scene-specific gameplay elements.

### Reusable stations

`StationBase` provides a common interaction layer for stations. Specialized station classes then handle processing, dispensing, combining, hand-ins, and other gameplay behavior.

This makes it possible to reuse the same interaction model across different rooms while changing the data and rules for each level.

### Persistent progression

`PersistentGameState` carries level results between scenes, including stars and progression data. Other systems use this shared state when returning to the hub or determining level completion.

## Local multiplayer

The project is designed for local multiplayer rather than online networking. The repository includes multiplayer test scenes and input handling intended for multiple active controllers and split-screen or shared-screen play.

## Project structure

The most relevant project code is located under:

```text
Assets/_Project/
├── Scripts/
├── Scenes/
├── Prefabs/
└── Models/
```

Examples of core scripts include:

```text
RoomManager.cs
RoomConfig.cs
OrderManager.cs
PersistentGameState.cs
StationBase.cs
ProcessingStation.cs
ItemContainerStation.cs
MultiIngredientStation.cs
InstantStation.cs
Counter.cs
```

The repository also contains the Wwise integration, project audio files, Unity packages, art assets, and other team-created or third-party project content.

## Running the project

1. Install **Unity 6000.3.6f1** through Unity Hub.
2. Clone the repository.
3. Add the repository folder as an existing Unity project.
4. Allow Unity to restore the packages listed in `Packages/manifest.json`.
5. Open a scene under `Assets/_Project/Scenes/`.
6. If working with the complete audio setup, open the included Wwise project in `CCL4_WwiseProject/`.

The repository is large because it contains the full Unity project, audio integration, and project assets.

## Repository note

This is the complete student team repository, so it contains both original project work and third-party packages or assets used during development. Any third-party content remains subject to its original license and should not be redistributed independently without checking those terms.

The `public/` directory contains additional project documentation created during development. Some parts of that documentation were originally used as coursework material and may include unfinished placeholders, so this README should be treated as the primary project overview.
