# Work Log


## 2026-05-21

### 战斗与视觉反馈
- 添加弹道系统：塔攻击时发射橙色小球追踪目标，命中后造成伤害并回收到对象池。
- 新增 `Projectile` 组件和 `AttackController` 的 projectilePrefab 支持，无 prefab 时保持即时伤害（向后兼容僵尸近战）。

### 经济循环
- 新增 `ResourceProducer` 组件，定时产出资源。
- 新增金矿（每5秒产8金）和伐木场（按周围树林数量产木材）建筑。
- 新增发电厂（产电力）和农场（产食物）。
- 新增铁矿场和铜矿场，产量按周围对应矿石数量计算。
- 新增 Iron/Copper 资源类型。
- 建筑增加电力消耗（powerCost），放置时扣电力，摧毁时退还。
- `ResourceManager` 新增 `Deduct` 方法。

### 兵工厂与训练系统
- 兵工厂从自动出兵改为手动训练队列：点击兵工厂弹出训练面板，选择兵种花钱训练。
- 新增弓箭手（Archer）：远程自动攻击，50金+20木，10秒训练。
- 士兵改为 30金/6秒训练，增加移动逻辑。
- `UnitSpawner` 重写为队列式训练系统（TryTrain → 排队 → 按时间完成）。
- `BuildingData` 改为 `trainableUnits[]` 数组。
- 新增 `TrainingPanel` UI（OnGUI），显示可训练兵种、费用、进度条。

### 单位操控
- 新增 `UnitCommandController`：左键选择士兵/弓箭手，右键指挥移动。
- 建造模式下左键不会误选单位。
- 选中单位底部显示青色指示条。

### 血量显示
- 新增 `UnitHealthBar` 组件：鼠标悬停显示血量条，左键点击持续显示3秒。
- 建筑和单位 prefab 均挂载此组件。

### UI 中文化
- 所有游戏内 UI 文本改为中文：资源、状态、波次、面板标题、按钮、建筑名、单位名。
- `WaveManager` 状态文本中文化。
- 建造按钮显示电力消耗（⚡图标）。

### 放置系统修复
- 修复点击 HUD 按钮时误触发建筑放置的 bug（添加 `IsPointerOverGameObject` 检查）。
- 修复 `UnitSpawner` 因建筑旋转90°导致 `transform.right` 方向错误的问题。

### 地图生成系统
- 地图从 64×64 扩大到 128×128。
- 新增 `MapGenerator`：随机生成树林（簇状洪水填充）、铁矿簇、铜矿簇。
- 树林阻挡通行，矿石不阻挡。
- 新增 `Cell.Content` 枚举（Forest/IronOre/CopperOre）用于区分格子内容。
- 地图边缘5格内不生成水域和树林，防止僵尸卡住。

### 地形系统
- 新增 `TerrainGenerator`：Perlin Noise 生成草地/雪地/泥土/水域分布。
- 水域不可通行不可建造。
- 树林改为噪声密度驱动（密度>0.6区域自然长树），形成大片连续森林。
- 支持 3×3 九宫格自动拼接（auto-tiling），根据邻居地形自动选择边缘 tile。
- 支持手绘 tileset 替换（`Terrain_Grass_Tileset.png` 等 48×48 九宫格）。
- 地面 Plane 改为不可见（仅保留碰撞体供放置射线检测）。

### 寻路系统
- 僵尸寻路从简单避障 → A* → **Flow Field** 演进。
- 新增 `FlowField` 组件：BFS 从目标扩散计算方向场，所有僵尸共享，每秒更新。
- 僵尸代码精简为查表移动，性能大幅提升，支持大规模尸潮。
- 删除旧的 `GridPathfinder`（A*）依赖。

### Sprite 系统
- 所有 prefab 从 3D Primitive 改为 SpriteRenderer + BoxCollider。
- 新增 `SpriteFactory` 编辑器工具：生成纯色占位 sprite、切片 sprite sheet（支持网格切片）。
- 新增 `SpriteAnimator` 组件：按帧播放 sprite 动画。
- 导入骷髅行走动画（4帧 16×16），僵尸 prefab 自动切片并播放。
- `PlacementSystem` 预览材质兼容 SpriteRenderer（直接改颜色）。
- Sprite pivot 设为 BottomCenter 防止下半身被地面遮挡。

### 建筑列表（当前共10种）
1. 指挥基地 2. 城墙 3. 箭塔 4. 金矿 5. 伐木场 6. 兵工厂 7. 发电厂 8. 农场 9. 铁矿场 10. 铜矿场


## 2026-05-17

- Started the build-experience pass. Goal: add visible grid feedback, building preview, valid/invalid placement colors, and quick building selection for the Unity 2022 prototype.
- Added grid debug drawing to `GridManager`, including visible grid lines and occupied-cell overlays. Also added a size-aware `GridToWorld` overload so multi-cell buildings can be centered on their footprint.
- Expanded `PlacementSystem` with a live building preview, green/red placement material feedback, number-key building selection, right-click/Esc cancel, size-aware placement, and ground snapping so generated primitive buildings sit on the map.
- Wired prototype scene generation to assign building options for command base, wall, and tower. Updated the MVP task list to mark the completed build-experience items.
- Documented prototype controls in `README.md` so the build controls are visible outside the Unity scene.
- Added a placement null guard so mouse placement fails cleanly if a scene is missing its `GridManager` reference.
- Added Play Mode guards to the prototype setup menu so scene/asset generation shows a clear dialog instead of throwing an editor exception.
- Added building lifecycle handling: buildings now publish destruction, the manager registers scene buildings, clears grid occupancy when they die, and critical building data can trigger defeat.
- Added unit death cleanup and zombie contact attacks. Zombies now stop to attack nearby player-faction targets using their `UnitData` damage, range, speed, and interval.
- Updated prototype generation so the command base is marked as defeat-critical, the starting base carries its `BuildingData`, and `BuildingManager` receives the grid reference. Marked the completed combat loop items in the MVP task list.
- Added UI-facing status hooks: `GameManager` can restart the active scene, and `WaveManager` now exposes a readable countdown/active-wave status string.
- Added `PrototypeHud` to display game state, resources, base health, wave status, and a defeat panel with a restart button.
- Updated prototype scene generation to build a HUD Canvas, status panel, defeat panel, restart button, EventSystem, and Build Settings entry for `Prototype.unity`.
- Tightened restart loading to prefer the active scene build index, falling back to the scene name only when needed.
- Added pause support through `GameManager`, the HUD pause button, and the `P` key. Documented pause/restart controls in `README.md`.
- Fixed the Unity compiler error in `PrototypeHud` by fully qualifying the `Jiangshi.Building.Building` type.
- Added a HUD refresh menu for the currently open scene and an `F9` debug defeat key so the defeat panel can be tested immediately.
- Fixed HUD generation on Unity 2022 by switching generated UI text from the removed `Arial.ttf` built-in font to `LegacyRuntime.ttf`.
- Added `SurvivalTimer`, a simple game-state-aware countdown that triggers victory when the configured survival duration elapses.
- Extended `PrototypeHud` with survival time display, a Victory panel, a victory restart button, and an `F10` debug victory key.
- Wired `SurvivalTimer` and the Victory panel into prototype scene generation and the HUD refresh menu. Documented the `F10` victory test key and marked the survival victory task complete.
- Updated terminal game states so victory and defeat pause gameplay until the player restarts.
- Exposed placement selection state from `PlacementSystem` so the HUD build menu can stay synchronized with hotkeys and cancel actions.
- Added HUD build menu behavior: build buttons select buildings, display hotkey numbers and costs, disable when resources are insufficient, and highlight the selected building.
- Wired build menu button creation into prototype scene generation and the HUD refresh menu. Marked the MVP build-menu task complete and documented the HUD buttons.
- Tuned build menu button layout and color precedence so unaffordable buildings stay disabled/grey even if they were previously selected.
- Added a reusable rounded UI background component and restyled the generated HUD with rounded panels, inset surfaces, accent bars, shadows, softer colors, and rounded build buttons.
