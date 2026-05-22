# Jiangshi

Unity prototype for a 2D/2.5D zombie horde survival RTS / tower defense game.

## Current Scope

- Grid-based map foundation
- Building placement foundation
- Resource inventory foundation
- Unit, combat, wave, and object-pool script skeletons
- Directory structure prepared for art, audio, prefabs, scenes, and data assets

## Open In Unity

1. Install Unity Hub.
2. Install Unity 2022 LTS. This project is set to `2022.3.62f3c1`.
3. Add this folder as an existing Unity project: `H:\jiangshi`.
4. Let Unity generate the `Library` folder and IDE solution files.

## First Prototype Target

A 64x64 grid map where the player can place a command base, wall, and tower. Zombies spawn at the map edge, move toward the base, and towers automatically attack.

## Prototype Controls

- `1`: select command base.
- `2`: select wall.
- `3`: select tower.
- Left mouse: place the selected building.
- Right mouse or `Esc`: cancel the current building preview.
- Build buttons are available in the HUD.
- `WASD`: move camera.
- Mouse wheel: zoom camera.
- `P`: pause or resume.
- `F9`: force defeat for UI testing.
- `F10`: force victory for UI testing.
- Restart is available from the defeat panel.
