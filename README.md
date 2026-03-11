# AvoidThem

A Unity 6 project scaffold for the game concept "AvoidThem".

## Status

Early setup / template stage. Core gameplay systems are not implemented yet.

## Requirements

- Unity Editor `6000.3.2f1`
- Unity Hub
- Git

## Quick Start

1. Clone the repository.
2. Open the project in Unity Hub using Unity `6000.3.2f1`.
3. Open the scene `Assets/Scenes/SampleScene.unity`.
4. Press Play in the Unity Editor.

## Project Structure

- `Assets/`: Game content, scenes, scripts, settings assets, and `.meta` files.
- `Packages/`: Unity package manifest and lock file.
- `ProjectSettings/`: Project-wide Unity configuration.

## Current Input Setup

Input actions are defined in `Assets/InputSystem_Actions.inputactions`. The action map exists, but gameplay bindings are not fully wired to custom game logic yet.

## Build

1. Open Unity and go to build settings/profiles.
2. Ensure `Assets/Scenes/SampleScene.unity` is included.
3. Select target platform.
4. Build to `Builds/<Platform>/`.

## Git Workflow (Unity Best Practices)

- Commit:
  - `Assets/**` (including all `.meta` files)
  - `Packages/manifest.json`
  - `Packages/packages-lock.json`
  - `ProjectSettings/**`
- Ignore:
  - `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Obj/`, `Build/`, `Builds/`
  - IDE/generated files (`*.csproj`, `*.sln`, `*.slnx`, `.vs/`, `.vscode/`, `.idea/`)

## Roadmap

- Implement player controller and camera behavior.
- Add enemy/spawn/avoidance gameplay loop.
- Add game states (start, lose, restart) and UI.
- Add score/progression and balancing.

## License

No license file is defined yet.
