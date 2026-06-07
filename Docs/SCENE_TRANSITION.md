# 场景跳转机制说明

## 概述

项目包含两个场景，通过传送门相互切换：

```
Prototype（2D 防御）  ──传送门──▶  SampleScene（3D 恐怖走廊）
                      ◀──结算返回──
```

**核心原则**：进入走廊时 2D 场景暂停，返回时携带结算结果，根据结果触发不同结局。

---

## 1. 进入走廊（Prototype → SampleScene）

### 触发条件
- 第 2 波完成后，`WaveManager` 将 `corridorTriggered` 设为 `true`
- 当前波次所有敌人死亡后，生成传送门 `CorridorPortal`
- 玩家单位走到传送门上触发 `CorridorPortal.OnTriggerEnter()`

### 执行逻辑（`CorridorPortal.cs`）
```csharp
Time.timeScale = 0f;                                              // 冻结 2D 场景
SceneManager.LoadScene("SampleScene", LoadSceneMode.Additive);    // 叠加加载 3D 场景
```

### 走廊启动（`CorridorEntry.cs`）
SampleScene 加载后，挂在场景中任意空物体上的 `CorridorEntry` 立即执行：

```csharp
Time.timeScale = 1f;                        // 恢复时间流速（仅 3D 场景需要）
禁用 Prototype 的 Camera 组件                // 避免两场景相机冲突
禁用 Prototype 的 EventSystem 组件           // 避免两套 UI 输入冲突
禁用 Prototype 的 Canvas / OnGUI UI 组件      // 避免 2D HUD 覆盖走廊 UI
```

**关键**：只禁用组件（`enabled = false`），不禁用 GameObject。这样走廊场景可以保持独立相机和 UI，Prototype 场景暂停在后台等待结算返回。

---

## 2. 返回 2D 场景（SampleScene → Prototype）

### 三种结局路径

| 结局 | 触发条件 | 脚本 | 结果枚举 |
|------|---------|------|---------|
| 成功 | 输入密码 1958 → 进门 → 捡起药剂 | `SerumPickup.cs` | `MissionResult.SerumAcquired` |
| 失败（追逐） | 被 Chaser 追上 | `Chaser.cs` | `MissionResult.OperatorLost` |
| 失败（回头杀） | 特定 Loop 回头看鬼 | `LookBackKill.cs` | `MissionResult.OperatorLost` |

### 结算流程

```
结局脚本 设置 MissionResultState.Result
    │
    ▼
SceneManager.LoadScene("Prototype")  ← 单模式加载，自动卸载 SampleScene
    │
    ▼
GameManager.Start() 解锁并显示鼠标，然后读取 MissionResultState.Result
    │
    ├── SerumAcquired  →  血清投放模式（点击地图 → 全灭僵尸 → 胜利）
    └── OperatorLost   →  200只僵尸从四面包围（必败）
```

### 数据跨场景传递

`MissionResultState` 是一个**静态类**，存放在全局命名空间，不随场景销毁：

```csharp
// MissionResultState.cs
public enum MissionResult { None, SerumAcquired, OperatorLost }
public static class MissionResultState {
    public static MissionResult Result = MissionResult.None;
}
```

任何脚本都可以读写 `MissionResultState.Result`，场景加载/卸载不会清空它。

---

## 3. 涉及的关键文件

| 文件 | 作用 |
|------|------|
| `Scripts/Units/CorridorPortal.cs` | 传送门触发：冻结 + 叠加加载 |
| `Scripts/SilentCorridor/CorridorEntry.cs` | 走廊启动：恢复 timeScale + 禁用 2D 组件 |
| `Scripts/SilentCorridor/MissionResultState.cs` | 结算结果枚举（跨场景存活） |
| `Scripts/SilentCorridor/PasswordLock.cs` | 密码锁：验证 1958 → 开门 |
| `Scripts/SilentCorridor/SerumPickup.cs` | 药剂可交互物：捡起 → 设置结果 → 加载 Prototype |
| `Scripts/SilentCorridor/Chaser.cs` | 追逐者：追上 → 设置结果 → 加载 Prototype |
| `Scripts/SilentCorridor/LookBackKill.cs` | 回头杀：回头看见鬼 → 设置结果 → 加载 Prototype |
| `Scripts/Core/GameManager.cs` | 游戏主控：`Start()` 读取结果 → 血清投放 / 大规模刷怪 / 胜利 / 失败 |
| `Scripts/Waves/WaveManager.cs` | 波次管理：第 2 波后触发传送门生成 |
| `Scripts/UI/PrototypeHud.cs` | HUD：`ShowPortalAnnouncement()` 显示提示文字 |

---

## 4. 如何添加新的结局

1. 在 `MissionResult` 枚举中添加新值
2. 在走廊场景中创建触发脚本，设置 `MissionResultState.Result` 并调用 `SceneManager.LoadScene("Prototype")`
3. 在 `GameManager.ApplyCorridorResult()` 的 switch 中添加对应处理

```csharp
// 示例：添加"隐藏结局"
case MissionResult.SecretEnding:
    StartCoroutine(TriggerSecretEnding());
    break;
```

---

## 5. Unity Editor 配置清单

要让场景跳转正常工作，需确保以下配置：

- [ ] SampleScene 已添加到 **Build Settings**（File → Build Settings → Add Open Scenes）
- [ ] SampleScene 中有一个挂载了 `CorridorEntry.cs` 的空 GameObject
- [ ] Prototype 场景中 `GameManager` 的 `Serum Placement Effect` 字段可选填（血清投放特效预制体）
- [ ] 走廊**药剂物体**（serum）挂载了 `SerumPickup.cs`，Layer 设为 Interactable
- [ ] 药剂物体的 `Cap Object` 字段引用了盖子子物体
- [ ] 门洞的缝隙足够宽（建议 2m+），避免 CharacterController 卡住
- [ ] URP 的 **Per Object Limit** 调大到 8+（避免点光源闪烁）

---

## 6. 常见问题

| 症状 | 原因 | 解决 |
|------|------|------|
| 进走廊后 2D 游戏在跑 | `CorridorEntry` 未挂载或未设置 timeScale=1 | 检查 SampleScene 有无 CorridorEntry 脚本 |
| 进走廊后 Prototype HUD 还在 | `CorridorEntry` 未禁用 Prototype 的 Canvas / OnGUI UI | 已在 CorridorEntry 中禁用外部场景 UI 组件 |
| 返回 Prototype 后鼠标不能动 | 走廊第一人称控制器把 Cursor 锁定且隐藏 | 已在 GameManager.Start 中重置为 `CursorLockMode.None` + visible |
| 捡起药剂无反应 | `gameObject.SetActive(false)` 会导致 Invoke 失效 | 已修复为禁用 Renderer/Collider |
| 返回后黑屏 | Prototype 相机被禁用后无法恢复 | 已修复为禁用组件而非 GameObject |
| `Unloading last loaded scene` 报错 | 叠加场景不能直接 UnloadAsync | 已改用 `LoadScene("Prototype")` 单模式 |
| 点光源闪烁/消失 | URP 默认每物体 4 个附加光源 | 调高 URP Asset → Additional Lights → Per Object Limit |
