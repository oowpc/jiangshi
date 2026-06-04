# Jiangshi 项目 AI 开发准则

## 项目概要

Unity 2022.3.62 2.5D 丧尸生存 RTS/塔防游戏原型。
打开方式：Unity Hub → Unity 2022 LTS → 添加此文件夹为已有项目。

## 当前状态

MVP 四阶段**已全部完成**，渲染管线已升级为 **URP 14.0.12**（2026-06-04）：
- 网格 + 建筑放置 + 预览幽灵 + 红绿反馈
- Zombie/Soldier/Swordsman/FastZombie/LargeZombie 单位 + 战斗系统
- 4 波次 + 胜利/失败判定 + 600 秒生存
- 经济系统 + HUD + 兵工厂训练 + 暂停/重开

### 渲染管线升级记录（2026-06-04）
- `Packages/manifest.json`：添加 `com.unity.render-pipelines.universal: 14.0.12` + `com.unity.textmeshpro: 3.0.7`
- `ProjectSettings/GraphicsSettings.asset`：`m_CustomRenderPipeline` 指向 `New Universal Render Pipeline Asset.asset`
- `PlacementSystem.cs` 和 `PrototypeSetupMenu.cs` 中的 `Shader.Find("Standard")` → `Shader.Find("Universal Render Pipeline/Lit")`
- URP Pipeline Asset / Renderer 从 SilentCorridor 源工程复制

上次代码审查已修复（2026-06-02 拉取的最新代码）：
- `OverlapSphere` 已全部替换为 `OverlapSphereNonAlloc`
- `Zombie.Start()` 已改用 `FlowField.Instance` 单例
- `SetupMenu` 已补 `worldCamera` 引用
- 死代码已删除：`TimeManager.cs`、`GridPathfinder.cs`、`PathfindingManager.cs`

## 待办事项

按优先级：

### 高优先级
- **调试 10 分钟 Demo 节奏**：`Docs/MVP_TASKS.md` 最后一项。参考 `Docs/superpowers/specs/2026-06-02-10min-pacing-design.md` 和 `Docs/superpowers/plans/2026-06-02-10min-pacing-plan.md`。调波次时间/数量/兵种组合、资源起始量、建筑成本。

### 融合进度（2026-06-04 进行中）
- [x] 渲染管线升级到 URP 14.0.12（与 SilentCorridor 统一）
- [x] SilentCorridor 资产导入（`Assets/Scripts/SilentCorridor/`、`Assets/Scenes/SilentCorridor/`、`Assets/Audio/`、`Assets/Fonts/`、`Assets/Materials/`、`Assets/Textures/`、`Assets/TextMesh Pro/`）
- [x] SampleScene 注册到 Build Settings
- [x] 创建 `MissionResultState.cs`（SerumAcquired / OperatorLost 枚举 + 静态状态）
- [x] `PasswordLock.cs` / `Chaser.cs` / `LookBackKill.cs` 添加结果写入 + `SceneManager.LoadScene("Prototype")` 返回
- [x] `GameManager.cs` 添加 `ApplyCorridorResult()` 读取结果并设难度系数（SerumAcquired=0.7, OperatorLost=1.5）
- [x] `Zombie.cs` 覆写 `Initialize` 和 `GetAttackDamage` 应用难度系数
- [x] `WaveManager.cs` 在第 2 波后自动加载 SilentCorridor
- [x] `PrototypeSetupMenu.cs` 新增 `SetBool` 并配置走廊任务触发参数
- [ ] 在 Unity 中打开项目，等 URP/TextMeshPro 包导入完成后，运行 `Jiangshi/Setup/Create Prototype Scene` 重建场景
- [ ] 完整测试两条路径

## 融合后场景切换流程

```text
Prototype 场景 → 第 2 波结束 → 自动加载 SampleScene (SilentCorridor)
  → 输入 1958 成功 → MissionResultState.SerumAcquired → LoadScene("Prototype") → 敌人削弱 (0.7x)
  → 被追逐/回头杀 → MissionResultState.OperatorLost → LoadScene("Prototype") → 敌人增强 (1.5x)
```
- ~~WaveManager 暂停行为确认~~ → 已确认：`WaitForSeconds`（缩放时间）为设计意图，暂停应同时暂停波次倒计时。
- ~~`PrototypeHud.cs:567-579` 与 `TrainingPanel.cs:113-127` 重复资源格式化代码合并~~ → 已提取到 `Economy/ResourceTypeExtensions.cs` 的 `GetLabel()` 扩展方法。
- ~~`PlacementSystem.cs:194` 建筑快捷键上限解除硬编码~~ → 已支持 `0` 键选择第 10 个建筑。

### 低优先级
- 16K 地形 GameObject 考虑改 Tilemap/Mesh 合并
- FlowField 每秒数组分配改为对象池

## 操控方式

| 键 | 功能 |
|----|------|
| 1-9, 0 | 选择建筑类型 |
| 左键 | 放置建筑 / 选择单位 |
| 右键 / Esc | 取消建造预览 / 命令单位移动或攻击 |
| WASD | 移动镜头 |
| 滚轮 | 缩放镜头 |
| P | 暂停/继续 |
| F9 | 强制失败 |
| F10 | 强制胜利 |

## 代码架构

```
Assets/Scripts/
├── Building/      — Building.cs, BuildingData.cs, BuildingManager.cs, PlacementSystem.cs, BuildingGroundShadow.cs
├── Combat/        — AttackController.cs, Damageable.cs, DeathEffect.cs, HitFlash.cs, Projectile.cs, FactionMember.cs
├── Core/          — GameManager.cs, MapGenerator.cs, TerrainGenerator.cs, SurvivalTimer.cs
├── Economy/       — ResourceManager.cs, ResourceProducer.cs, ResourceType.cs, ResourceAmount.cs
├── Grid/          — GridManager.cs, GridPosition.cs, GridCell.cs, CellContent.cs
├── Pathfinding/   — FlowField.cs (BFS 流场，每 1s 刷新，128×128)
├── Pools/         — ComponentPool.cs, ObjectPool.cs
├── UI/            — PrototypeHud.cs, RtsCameraController.cs, TrainingPanel.cs, UnitHealthBar.cs, SpriteAnimator.cs
├── Units/         — Unit.cs, UnitData.cs, Zombie.cs, Soldier.cs, Swordsman.cs, Archer.cs, UnitSpawner.cs, UnitManager.cs, UnitCommandController.cs, UnitVisualAnimator.cs, IMovableUnit.cs
└── Waves/         — WaveManager.cs, WaveData.cs
```

## 编码规范

- 命名空间：`Jiangshi.<模块名>`（如 `Jiangshi.Units`、`Jiangshi.Combat`）
- 基类：`Unit` — `Zombie`/`Soldier`/`Swordsman` 基类
- 派系：`Faction.Player` / `Faction.Enemy`
- 资源类型：`ResourceType` 枚举（Gold/Wood/Food/Power/Iron/Copper/Population）
- ScriptableObject 数据驱动：`BuildingData`（建筑配置）、`UnitData`（单位配置）、`WaveData`（波次配置）
- 编辑器设置脚本：`Assets/Editor/` — `PrototypeSetupMenu.cs` 通过菜单 `Jiangshi/Setup/` 一键生成场景

## 常用命令

无 CLI，一切在 Unity Editor 中操作：
1. 菜单 `Jiangshi/Setup/Create Prototype Scene` — 生成场景
2. 点击 Play 运行

## 修改注意事项

- 修改 `PrototypeSetupMenu.cs` 后，**必须**在 Unity 中重新执行 `Jiangshi/Setup/Create Prototype Scene` 才能生效
- 修改 ScriptableObject 属性（如 `BuildingData`、`UnitData`、`WaveData`），如果场景已存在，需在 Inspector 中手动刷新或重建场景
- FlowField 依赖 `GridManager`，修改网格逻辑需同步检查 FlowField
- 编辑 Agent 代码时，**先阅读目标文件及其依赖**，了解当前状态再动手
