# Coding Guide

## Conventions

- Runtime code uses the `Jiangshi` root namespace.
- Editor-only code lives under `Assets/Editor`.
- Data that designers tune should be ScriptableObject assets.
- Scene objects should depend on data assets where possible.
- Prefer small systems with clear ownership over large manager classes.

## Initial Architecture

- `GridManager` owns cell state.
- `PlacementSystem` validates placement and spends resources.
- `ResourceManager` owns resource inventory.
- `UnitManager` spawns units from `UnitData`.
- `WaveManager` reads `WaveData` and spawns enemies.
- `AttackController` uses `FactionMember` to avoid friendly fire.

