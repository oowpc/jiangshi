# 代码审查报告

> 审查范围：`Assets/Scripts/` 全部 46 个 .cs 文件
> 审查日期：2026-06-02

---

## 一、空引用风险

**结论：无异常。** 所有可能为空的路径均有守卫。

| 位置 | 判定 | 原因 |
|------|------|------|
| `Combat/Projectile.cs:36` `pool.Release(this)` | 无风险 | `Init()` 在 `Get()` 后**同步**调用，`Update()` 下一帧才执行，pool 始终已赋值 |
| `Pathfinding/FlowField.cs:40` `directions` | 已防护 | 入口 `directions == null` 检查 |
| `Building/Building.cs:45` `FindObjectOfType<ResourceManager>` | 已防护 | `if (rm != null)` 守卫 |

---

## 二、Unity 生命周期问题

### 2.1 `TimeManager` 为死代码（建议删除）

- **位置**：`Core/TimeManager.cs`、`Editor/PrototypeSetupMenu.cs:158`
- **现状**：整个运行时无任何代码调用 `TimeManager`（仅有自身定义 + SetupMenu 挂载）
- **风险**：与 `GameManager` 挂同一个 GameObject，若将来被调用会与 `GameManager` 产生 timeScale 冲突

### 2.2 `UnitCommandController.worldCamera` 未赋值

- **位置**：`Editor/PrototypeSetupMenu.cs:169`
- **现状**：创建 `unitCommand` 后未设 `worldCamera`，依赖 `Update` 中 `Camera.main` 兜底（仅首帧执行一次）
- **修复**：在 SetupMenu 中加一行 `SetObjectReference(unitCommand, "worldCamera", camera);`

### 2.3 `WaveManager` 暂停时波次延迟

- **位置**：`Waves/WaveManager.cs:51`
- **现状**：`yield return new WaitForSeconds(wave.startTime)` 使用缩放时间
- **影响**：暂停（timeScale=0）会导致波次延后。需确认是否为设计意图

---

## 三、FindObjectOfType 分布

| 位置 | 触发时机 | 频率 | 影响 |
|------|----------|------|------|
| `Units/Zombie.cs:30` | 每只僵尸 Start() | 累计数百次 | 中等 ⚠️ |
| `Economy/ResourceProducer.cs:37-38` | 每个生产者 Start() | ~10 次 | 可忽略 |
| `Units/UnitSpawner.cs:30-31` | 每个兵工厂 Start() | 少量 | 可忽略 |
| `Building/Building.cs:45,106` | 放置/死亡时 | 低频 | 可忽略 |

**优化建议**：`Zombie.cs:30` 的 `FindObjectOfType<FlowField>()` 改为 `FlowField.Instance` 单例模式。

---

## 四、性能问题

### 4.1 `Physics.OverlapSphere` 未统一使用 NonAlloc（应该改）

| 文件 | 行号 | 方法 | 问题 |
|------|------|------|------|
| `Units/Zombie.cs` | 105 | `DetectPlayerNearby()` | 非攻击态每帧分配新数组 |
| `Units/Zombie.cs` | 141 | `FindAttackTarget()` | 攻击态无目标时每帧分配 |
| `Combat/AttackController.cs` | 54 | `FindTarget()` | 每个塔每帧分配 |

**已有正确模式**：`Units/Archer.cs:47` 使用 `Physics.OverlapSphereNonAlloc`，将上述三处改为一致即可。

### 4.2 地形 16K GameObject（原型可接受）

- **位置**：`Core/TerrainGenerator.cs:87-105`
- **现状**：128×128 格每格一个 GameObject + SpriteRenderer
- **影响**：Unity 动态合批可合并同材质 Sprite，draw call 不会到 16K，但内存开销较大
- **生产优化**：改用 Unity Tilemap 或合并 Mesh

### 4.3 FlowField 每秒数组分配（可接受）

- **位置**：`Pathfinding/FlowField.cs:73,75`
- **现状**：每秒 new `Vector2[128,128]` + `int[128,128]`（共 32K 元素）
- **影响**：1Hz 频率，GC 压力可控

---

## 五、扩展性

| 问题 | 位置 | 说明 |
|------|------|------|
| 资源类型 switch 重复 | `UI/PrototypeHud.cs:567-579` ↔ `UI/TrainingPanel.cs:113-127` | 两份完全相同的格式化逻辑 |
| 建筑快捷键上限 9 | `Building/PlacementSystem.cs:194` | `Mathf.Min(buildingOptions.Length, 9)` 硬编码 |
| 生产模式不可配置 | `Building/Building.cs:169-192` | 三个硬编码分支（标准/缩放/额外） |

---

## 六、死代码

| 文件 | 原因 |
|------|------|
| `Core/TimeManager.cs` | 无运行时调用，与 GameManager 功能重叠 |
| `Pathfinding/GridPathfinder.cs` | A* 实现，0 处引用，已被 FlowField 替代 |
| `Pathfinding/PathfindingManager.cs` | `GetDirectionToward()` 未被调用，`NotifyGridChanged()` 为空体 |

---

## 七、分类汇总

### 应该改（3 项）

| # | 内容 | 工作量 |
|---|------|--------|
| 1 | 统一 `OverlapSphere` → `OverlapSphereNonAlloc`（Zombie ×2、AttackController） | 小 |
| 2 | `Zombie.Start()` 中 `FindObjectOfType<FlowField>` 改单例/引用 | 小 |
| 3 | SetupMenu 中给 `UnitCommandController` 补上 `worldCamera` 引用 | 1 行 |

### 建议（4 项）

| # | 内容 |
|---|------|
| 4 | 删除死代码：`TimeManager.cs`、`GridPathfinder.cs`（`PathfindingManager.cs` 若确认无用也删除） |
| 5 | 合并 `PrototypeHud` 和 `TrainingPanel` 中重复的资源格式化代码 |
| 6 | `Waves/WaveBoundarySmokeTest.cs` 补充提交 |
| 7 | 确认 `WaveManager.WaitForSeconds` 暂停延迟行为是否为设计意图 |

---

## 八、总体评价

**代码质量良好，无崩溃风险。** 主要工作集中在：消除 Frame Allocation（统一 NonAlloc 模式）、删除死代码、补充遗漏的引用赋值。三项"应该改"均为小改动，四项"建议"可按优先级逐步处理。
