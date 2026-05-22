# Project Structure

## Runtime Scripts

- `Assets/Scripts/Core`: game state and time control.
- `Assets/Scripts/Grid`: grid cells, grid positions, occupancy checks.
- `Assets/Scripts/Building`: building data, building instances, placement.
- `Assets/Scripts/Economy`: resources, costs, spending, production foundation.
- `Assets/Scripts/Units`: soldier, zombie, unit data, spawning.
- `Assets/Scripts/Combat`: health and basic attack behavior.
- `Assets/Scripts/Waves`: timed enemy wave data and spawning.
- `Assets/Scripts/Pathfinding`: placeholder for A* or flow field logic.
- `Assets/Scripts/Pools`: generic component object pool.
- `Assets/Scripts/UI`: resource display foundation.
- `Assets/Editor`: Unity editor menu tools for generating prototype content.

## Content Folders

- `Assets/Prefabs`: generated or handmade prefabs.
- `Assets/Scenes`: Unity scenes.
- `Assets/ScriptableObjects`: editable gameplay data.
- `Assets/Art`: sprites, models, materials, visual effects.
- `Assets/Audio`: music and sound effects.

## Editor Tools

Use Unity menu:

- `Jiangshi/Setup/Create Prototype Assets`
- `Jiangshi/Setup/Create Prototype Scene`

The generated scene is intentionally simple. It is a starting point for wiring references and implementing the first playable loop.

## First Unity Steps

1. Open `H:\jiangshi` with Unity Hub.
2. Wait for Unity to import scripts.
3. Run `Jiangshi/Setup/Create Prototype Scene`.
4. Open `Assets/Scenes/Prototype.unity`.
5. Press Play and use WASD plus mouse wheel to move the camera.
