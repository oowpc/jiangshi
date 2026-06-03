# 前 10 分钟游戏节奏 · 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 实现高压生存风格的 4 波 10 分钟节奏，包含三种丧尸类型、多敌混编波次、紧巴开局经济。

**Architecture:** 扩展 `WaveData` 支持多敌混编（`EnemyGroup[]`）+ 生成方向控制。新建两种丧尸 UnitData/Prefab。调整起始资源和生存时长。所有资产通过 `PrototypeSetupMenu` 编辑器工具生成。

**Tech Stack:** Unity 2022.3 LTS, C#, ScriptableObject

---

## 文件结构

### 修改
- `Assets/Scripts/Waves/WaveData.cs` — 增加 `EnemyGroup` 结构体和 `spawnDirections` 字段
- `Assets/Scripts/Waves/WaveManager.cs` — 多敌混编生成逻辑
- `Assets/Scripts/Economy/ResourceManager.cs` — 起始资源改为 200金+50木+10食物+30电
- `Assets/Scripts/Core/SurvivalTimer.cs` — `durationSeconds` 默认值改为 600
- `Assets/Editor/PrototypeSetupMenu.cs` — 创建新丧尸 prefab/数据 + 4 波 WaveData 资产

### 新建
- `Assets/ScriptableObjects/Units/FastZombieData.asset` — 高速丧尸（编辑器生成，不手写）
- `Assets/ScriptableObjects/Units/LargeZombieData.asset` — 大型丧尸（编辑器生成）
- `Assets/ScriptableObjects/Waves/Wave01.asset` → 覆盖为新配置
- `Assets/ScriptableObjects/Waves/Wave02.asset` — 新建（编辑器生成）
- `Assets/ScriptableObjects/Waves/Wave03.asset` — 新建（编辑器生成）
- `Assets/ScriptableObjects/Waves/Wave04.asset` — 新建（编辑器生成）
- `Assets/Prefabs/FastZombie.prefab` — 高速丧尸 Prefab（编辑器生成）
- `Assets/Prefabs/LargeZombie.prefab` — 大型丧尸 Prefab（编辑器生成）

---

### Task 1: 扩展 WaveData 支持多敌混编与方向控制

**Files:**
- Modify: `Assets/Scripts/Waves/WaveData.cs`

- [x] **Step 1: 在 WaveData 末尾增加 EnemyGroup 结构和多敌字段**

在 `Assets/Scripts/Waves/WaveData.cs` 的 `warningText` 字段之后、结束括号之前，增加以下代码：

```csharp
        public EnemyGroup[] enemyGroups;
        public int spawnDirections = 1;

        [System.Serializable]
        public struct EnemyGroup
        {
            public UnitData enemy;
            public int count;
        }
```

完整修改后的文件：

```csharp
using Jiangshi.Units;
using UnityEngine;

namespace Jiangshi.Waves
{
    [CreateAssetMenu(menuName = "Jiangshi/Wave Data")]
    public sealed class WaveData : ScriptableObject
    {
        public float startTime = 60f;
        public UnitData enemy;
        public int count = 20;
        public float spawnInterval = 0.25f;
        public string warningText;
        public EnemyGroup[] enemyGroups;
        public int spawnDirections = 1;

        [System.Serializable]
        public struct EnemyGroup
        {
            public UnitData enemy;
            public int count;
        }
    }
}
```

- [x] **Step 2: 验证编译（在 Unity 中打开项目检查）**

无需 CLI 命令，此任务完成后在 Unity Editor 中确认脚本编译无误。

---

### Task 2: 更新 WaveManager 支持多敌混编生成

**Files:**
- Modify: `Assets/Scripts/Waves/WaveManager.cs`

- [x] **Step 1: 重写 RunWave 协程处理 enemyGroups**

将 `RunWave` 方法中遍历 `wave.count` 次调用 `SpawnEnemy(wave)` 的循环替换为以下逻辑：

```csharp
        private IEnumerator RunWave(WaveData wave)
        {
            yield return new WaitForSeconds(wave.startTime);

            activeWaveCount++;
            activeWaveText = string.IsNullOrWhiteSpace(wave.warningText) ? "波次进行中" : wave.warningText;
            RefreshStatusText();

            // 多敌混编模式：按 enemyGroups 交叉生成
            if (wave.enemyGroups != null && wave.enemyGroups.Length > 0)
            {
                var groupIndex = 0;
                var spawnDirections = Mathf.Max(1, wave.spawnDirections);
                for (var i = 0; i < wave.count; i++)
                {
                    var group = wave.enemyGroups[groupIndex];
                    groupIndex = (groupIndex + 1) % wave.enemyGroups.Length;
                    SpawnEnemyFromGroup(group, spawnDirections);
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }
            else
            {
                // 原有逻辑：单一敌人生成
                for (var i = 0; i < wave.count; i++)
                {
                    SpawnEnemy(wave);
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }

            activeWaveCount = Mathf.Max(0, activeWaveCount - 1);
            completedWaveCount++;
            RefreshStatusText();
        }
```

- [x] **Step 2: 增加 SpawnEnemyFromGroup 私有方法**

在 `SpawnEnemy` 方法之后添加：

```csharp
        private void SpawnEnemyFromGroup(WaveData.EnemyGroup group, int directionCount)
        {
            if (unitManager == null || group.enemy == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            var spawnIndex = Random.Range(0, Mathf.Min(directionCount, spawnPoints.Length));
            var spawnPoint = spawnPoints[spawnIndex];
            var unit = unitManager.Spawn(group.enemy, GetSpawnPosition(spawnPoint.position), spawnPoint.rotation);

            if (unit is Zombie zombie)
            {
                zombie.SetTarget(defaultTarget);
                zombie.SetAggressive();
            }
        }
```

完整修改：在 `WaveManager.cs` 中 `SpawnEnemy` 方法的第 87 行之后插入以上 `SpawnEnemyFromGroup` 方法，并用 Step 1 的代码替换第 52~69 行的 `RunWave` 协程体。

---

### Task 3: 更新 WaveManager 中文化波次状态文本

**Files:**
- Modify: `Assets/Scripts/Waves/WaveManager.cs`

- [x] **Step 1: 更新默认状态文本为中文化**

修改 `StatusText` 相关字符串：

| 行号 | 原文本 | 新文本 |
|------|--------|--------|
| 24 | `"无波次"` | 不变，已是中文 |
| 57 | `"波次进行中"` | 不变，已是中文 |
| 200-201 | `"无波次"` | 不变，已是中文 |
| 206-208 | `activeWaveText` 逻辑 | 不变 |
| 215-216 | `$"{label} {Mathf.CeilToInt(wave.startTime - elapsed)}秒后"` | 不变，已是中文 |
| 221 | `"波次结束"` 或 `"无波次"` | 不变，已是中文 |

WaveManager 的中文化已在之前完成，确认无需修改。

---

### Task 4: 创建高速丧尸与大丧尸 Prefab 和 UnitData

**Files:**
- Modify: `Assets/Editor/PrototypeSetupMenu.cs`

- [x] **Step 1: 在 CreatePrototypeAssets 中增加新丧尸 Prefab 和 UnitData 生成**

在 `CreatePrototypeAssets` 方法中，找到 `CreateWaveData("Wave01", zombieData);` 这一行（约第 104 行），在其后添加以下代码：

```csharp
            // 高速丧尸
            var fastZombiePrefab = CreateUnitPrefab<Zombie>("FastZombie", PrimitiveType.Capsule, new Color(0.9f, 0.25f, 0.15f), Faction.Enemy);
            SetupSpriteAnimation(fastZombiePrefab, "Zombie", 4, 16);
            // 缩放高速丧尸使其体积小于普通丧尸
            fastZombiePrefab.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            var fastZombieData = CreateUnitData("FastZombieData", "高速僵尸", fastZombiePrefab, 30, 4f, 4, 1.0f, 1.0f);

            // 大型丧尸
            var largeZombiePrefab = CreateUnitPrefab<Zombie>("LargeZombie", PrimitiveType.Capsule, new Color(0.6f, 0.25f, 0.7f), Faction.Enemy);
            SetupSpriteAnimation(largeZombiePrefab, "Zombie", 4, 16);
            largeZombiePrefab.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var largeZombieData = CreateUnitData("LargeZombieData", "巨型僵尸", largeZombiePrefab, 120, 1f, 12, 1.5f, 1.3f);

            EditorUtility.SetDirty(fastZombieData);
            EditorUtility.SetDirty(largeZombieData);
```

注意：`CreateUnitPrefab` 保存 Prefab 后会返回 prefab 引用（`SavePrefab` 方法已返回正确的 GameObject）。需要确认直接对返回的 prefab 设置 `transform.localScale` 在 Prefab Mode 下是否生效——由于 `SavePrefab` 先 Instantiate 再保存，需要改为在保存前设置 scale。

- [x] **Step 2: 修正 CreateUnitPrefab 支持 scale 参数**

将 `CreateUnitPrefab<T>` 方法改为接受 scale 参数。修改方法签名和调用：

在 `CreateUnitPrefab` 方法签名中添加 `Vector3? scale = null` 参数：

```csharp
        private static GameObject CreateUnitPrefab<T>(string name, PrimitiveType primitiveType, Color color, Faction faction, Vector3? scale = null) where T : Unit
        {
            var instance = new GameObject(name);
            instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var sr = instance.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateColorSprite(name, color);
            instance.AddComponent<BoxCollider>().size = Vector3.one;
            instance.AddComponent<Damageable>();
            instance.AddComponent<HitFlash>();
            instance.AddComponent<DeathEffect>();
            instance.AddComponent<FactionMember>().SetFaction(faction);
            instance.AddComponent<T>();
            instance.AddComponent<Jiangshi.UI.UnitHealthBar>();
            if (scale.HasValue)
            {
                instance.transform.localScale = scale.Value;
            }
            return SavePrefab(instance, $"{PrefabRoot}/{name}.prefab");
        }
```

然后 Step 1 的代码改为：

```csharp
            // 高速丧尸
            var fastZombiePrefab = CreateUnitPrefab<Zombie>("FastZombie", PrimitiveType.Capsule, new Color(0.9f, 0.25f, 0.15f), Faction.Enemy, new Vector3(0.75f, 0.75f, 0.75f));
            SetupSpriteAnimation(fastZombiePrefab, "Zombie", 4, 16);
            var fastZombieData = CreateUnitData("FastZombieData", "高速僵尸", fastZombiePrefab, 30, 4f, 4, 1.0f, 1.0f);

            // 大型丧尸
            var largeZombiePrefab = CreateUnitPrefab<Zombie>("LargeZombie", PrimitiveType.Capsule, new Color(0.6f, 0.25f, 0.7f), Faction.Enemy, new Vector3(1.5f, 1.5f, 1.5f));
            SetupSpriteAnimation(largeZombiePrefab, "Zombie", 4, 16);
            var largeZombieData = CreateUnitData("LargeZombieData", "巨型僵尸", largeZombiePrefab, 120, 1f, 12, 1.5f, 1.3f);

            EditorUtility.SetDirty(fastZombieData);
            EditorUtility.SetDirty(largeZombieData);
```

---

### Task 5: 创建 4 个波次的 WaveData 资产

**Files:**
- Modify: `Assets/Editor/PrototypeSetupMenu.cs`
- Replace: `Assets/ScriptableObjects/Waves/Wave01.asset`
- New: `Assets/ScriptableObjects/Waves/Wave02.asset`, `Wave03.asset`, `Wave04.asset`

- [x] **Step 1: 重写 CreateWaveData 为通用创建方法，增加 enemyGroups 支持**

将 `PrototypeSetupMenu` 类中的 `CreateWaveData` 方法替换为：

```csharp
        private static WaveData CreateWaveData(string assetName, float startTime, int count, float spawnInterval, string warningText, int spawnDirections, params (UnitData enemy, int count)[] groups)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            wave.startTime = startTime;
            wave.count = count;
            wave.spawnInterval = spawnInterval;
            wave.warningText = warningText;
            wave.spawnDirections = spawnDirections;

            if (groups != null && groups.Length > 0)
            {
                wave.enemy = groups[0].enemy;
                wave.enemyGroups = new WaveData.EnemyGroup[groups.Length];
                for (var i = 0; i < groups.Length; i++)
                {
                    wave.enemyGroups[i] = new WaveData.EnemyGroup
                    {
                        enemy = groups[i].enemy,
                        count = groups[i].count
                    };
                }
            }

            SaveAsset(wave, $"{WaveDataRoot}/{assetName}.asset");
            return wave;
        }
```

- [x] **Step 2: 在 CreatePrototypeAssets 中替换旧的 WaveData 创建**

找到 `CreateWaveData("Wave01", zombieData);`（约第 104 行），替换为 4 个波次的创建：

```csharp
            // 4 波配置
            CreateWaveData("Wave01", 120f, 20, 0.35f, "第一波：丧尸接近中", 2, (zombieData, 20));
            CreateWaveData("Wave02", 240f, 30, 0.30f, "第二波：它们更快了", 2, (zombieData, 21), (fastZombieData, 9));
            CreateWaveData("Wave03", 390f, 35, 0.25f, "第三波：庞然大物出现", 3, (zombieData, 15), (fastZombieData, 12), (largeZombieData, 8));
            CreateWaveData("Wave04", 510f, 40, 0.20f, "第四波：最终冲击", 4, (zombieData, 10), (fastZombieData, 15), (largeZombieData, 15));
```

注意：需要确保 `CreateWaveData` 调用在 `zombieData`、`fastZombieData`、`largeZombieData` 变量定义之后。

- [x] **Step 3: 更新场景生成中 WaveData 引用**

在 `CreatePrototypeScene` 方法中，将第 194 行的：

```csharp
            var waveData = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave01.asset");
```

替换为加载全部 4 个波次：

```csharp
            var wave01 = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave01.asset");
            var wave02 = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave02.asset");
            var wave03 = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave03.asset");
            var wave04 = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave04.asset");
```

然后将第 215 行的：

```csharp
            SetObjectArray(waveManager, "waves", new Object[] { waveData });
```

替换为：

```csharp
            SetObjectArray(waveManager, "waves", new Object[] { wave01, wave02, wave03, wave04 });
```

---

### Task 6: 更新起始资源为 200金/50木/10食物/30电

**Files:**
- Modify: `Assets/Scripts/Economy/ResourceManager.cs`

- [x] **Step 1: 修改 ResourceManager 中 startingResources 默认值**

将第 9-15 行：

```csharp
        [SerializeField] private ResourceAmount[] startingResources =
        {
            new ResourceAmount { type = ResourceType.Gold, amount = 300 },
            new ResourceAmount { type = ResourceType.Wood, amount = 120 },
            new ResourceAmount { type = ResourceType.Food, amount = 10 },
            new ResourceAmount { type = ResourceType.Power, amount = 10 }
        };
```

替换为：

```csharp
        [SerializeField] private ResourceAmount[] startingResources =
        {
            new ResourceAmount { type = ResourceType.Gold, amount = 200 },
            new ResourceAmount { type = ResourceType.Wood, amount = 50 },
            new ResourceAmount { type = ResourceType.Food, amount = 10 },
            new ResourceAmount { type = ResourceType.Power, amount = 30 }
        };
```

---

### Task 7: 更新生存时长为 10 分钟 (600s)

**Files:**
- Modify: `Assets/Scripts/Core/SurvivalTimer.cs`

- [x] **Step 1: 修改 durationSeconds 默认值**

将第 8 行：

```csharp
        [SerializeField] private float durationSeconds = 180f;
```

改为：

```csharp
        [SerializeField] private float durationSeconds = 600f;
```

---

### Task 8: 最终验证与清理

- [ ] **Step 1: 在 Unity 中重新生成原型场景**

在 Unity Editor 菜单中运行 `Jiangshi/Setup/Create Prototype Scene`，确认：
- 4 个 WaveData 资产正确创建
- 高速/大型丧尸 prefab 和 UnitData 正确创建
- 场景中 WaveManager 正确引用了 4 个波次
- 起始资源显示为 200金/50木/10食物/30电
- 生存倒计时显示 10:00

- [ ] **Step 2: 运行场景验证**

按 Play 测试：
1. 0:00 → 资源为 200金/50木/10食物/30电
2. 2:00 → 第一波 20 普通丧尸抵达
3. 4:00 → 第二波 21普通 + 9高速抵达
4. 6:30 → 第三波 15普通 + 12高速 + 8大型抵达
5. 8:30 → 第四波 10普通 + 15高速 + 15大型从 4 方向抵达
6. 10:00 → 胜利面板弹出

- [ ] **Step 3: 检查错误修复**

如果发现 bug，用 systematic-debugging skill 排查。常见问题：
- WaveManager 中 `enemyGroups[groupIndex]` 的 `group.enemy` 可能为 null → 增加空检查
- 新丧尸 prefab 的 scale 未生效 → 检查 `CreateUnitPrefab` 中 scale 设置时机（必须在 `SavePrefab` 前设置）

---

### Task 9: 提交

```bash
git add Assets/Scripts/Waves/WaveData.cs
git add Assets/Scripts/Waves/WaveManager.cs
git add Assets/Scripts/Economy/ResourceManager.cs
git add Assets/Scripts/Core/SurvivalTimer.cs
git add Assets/Editor/PrototypeSetupMenu.cs
git add Assets/ScriptableObjects/Waves/
git add Assets/ScriptableObjects/Units/
git add Assets/Prefabs/
git commit -m "feat: 实现高压生存4波10分钟节奏（多敌混编/高速丧尸/大型丧尸/紧巴开局）"
```

---

## 实施顺序注意事项

1. **Task 1 → Task 2**：WaveData 扩展在前，WaveManager 消费在后
2. **Task 4 → Task 5**：丧尸 UnitData/Prefab 必须在 WaveData 创建前有可用引用
3. **Task 5 → Task 8**：WaveData 资产创建后才能验证场景
4. **Task 6, 7**：独立任务，可与 Task 4~5 并行
5. 所有 C# 修改完成后必须在 Unity Editor 中确认编译通过
