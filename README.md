# AvoidThem

A simple top-down survival game in Unity where you dodge bouncing hazards with your cursor.

## Status

Playable prototype.

## Requirements

- Unity Editor `6000.3.2f1`
- Unity Hub
- Git

## Quick Start

1. Clone the repository.
2. Open the project in Unity Hub using Unity `6000.3.2f1`.
3. Open the scene `Assets/Scenes/SampleScene.unity`.
4. Press Play in the Unity Editor.

## Gameplay

- View: top-down camera over a square arena.
- Player: your mouse cursor is represented by a cyan circle.
- Hazards: red spheres spawn from random arena edges and bounce off walls.
- Lose condition: touching any hazard ends the run.
- Scoring: score is survival time in seconds.
- UI: start overlay and game-over overlay with restart prompt.

## Controls

- Mouse move: move player circle
- `Space` or left click: start game
- `R`, `Space`, or left click (after death): restart

## Project Structure

- `Assets/`: Game content, scenes, scripts, settings assets, and `.meta` files.
- `Packages/`: Unity package manifest and lock file.
- `ProjectSettings/`: Project-wide Unity configuration.

## Implementation Notes

- Main gameplay bootstrap is in `Assets/Scripts/AvoidThemGame.cs`.
- The game is initialized at runtime, so it works in the existing sample scene without manual scene wiring.

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

- Add audio feedback and simple VFX.
- Add difficulty tuning presets.
- Add pause/settings menu.
- Add a persistent high-score save.

## License

No license file is defined yet.
