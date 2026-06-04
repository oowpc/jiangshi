using System.IO;
using System.Linq;
using Jiangshi.Building;
using Jiangshi.Combat;
using Jiangshi.Core;
using Jiangshi.Economy;
using Jiangshi.Grid;
using Jiangshi.UI;
using Jiangshi.Units;
using Jiangshi.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jiangshi.Editor
{
    public static class PrototypeSetupMenu
    {
        private const string PrefabRoot = "Assets/Prefabs";
        private const string SceneRoot = "Assets/Scenes";
        private const string BuildingDataRoot = "Assets/ScriptableObjects/Buildings";
        private const string UnitDataRoot = "Assets/ScriptableObjects/Units";
        private const string WaveDataRoot = "Assets/ScriptableObjects/Waves";

        [MenuItem("Jiangshi/Setup/Create Prototype Assets")]
        public static void CreatePrototypeAssets()
        {
            if (IsPlayModeBlocked("create prototype assets"))
            {
                return;
            }

            EnsureFolders();

            var basePrefab = CreateBuildingPrefab("CommandBase", PrimitiveType.Cube, new Vector3(3f, 1.35f, 3f), new Color(0.25f, 0.55f, 0.95f));
            var wallPrefab = CreateBuildingPrefab("Wall", PrimitiveType.Cube, new Vector3(2f, 1.35f, 1f), new Color(0.45f, 0.45f, 0.45f));
            var projectilePrefab = CreateProjectilePrefab();
            var towerPrefab = CreateBuildingPrefab("Tower", PrimitiveType.Cylinder, new Vector3(1f, 2.35f, 1f), new Color(0.95f, 0.75f, 0.25f), projectilePrefab);
            var soldierPrefab = CreateUnitPrefab<Soldier>("Soldier", PrimitiveType.Capsule, new Color(0.2f, 0.8f, 0.45f), Faction.Player);
            SetObjectReference(soldierPrefab.GetComponent<Soldier>(), "projectilePrefab", projectilePrefab.GetComponent<Projectile>());
            var zombiePrefab = CreateUnitPrefab<Zombie>("Zombie", PrimitiveType.Capsule, new Color(0.55f, 0.85f, 0.2f), Faction.Enemy);
            SetupSpriteAnimation(zombiePrefab, "Zombie", 4, 16);

            var baseData = CreateBuildingData("CommandBaseData", "指挥基地", basePrefab, new Vector2Int(3, 3), 500, 0, 0, true);
            var wallData = CreateBuildingData("WallData", "城墙", wallPrefab, new Vector2Int(2, 1), 120, 10, 5, false);
            var towerData = CreateBuildingData("TowerData", "箭塔", towerPrefab, Vector2Int.one, 180, 80, 30, false);

            var goldMinePrefab = CreateProducerPrefab("GoldMine", new Color(0.65f, 0.45f, 0.25f), new Vector3(1f, 1.45f, 1f));
            var lumberMillPrefab = CreateProducerPrefab("LumberMill", new Color(0.4f, 0.7f, 0.25f), new Vector3(1f, 1.45f, 1f));
            var powerPlantPrefab = CreateProducerPrefab("PowerPlant", new Color(0.3f, 0.75f, 0.95f), new Vector3(1f, 1.45f, 1f));
            var farmPrefab = CreateProducerPrefab("Farm", new Color(0.85f, 0.75f, 0.3f), new Vector3(1f, 1.35f, 1f));
            var goldMineData = CreateProducerBuildingData("GoldMineData", "木屋", goldMinePrefab, 80, 40, 40, ResourceType.Population, 4, 5f);
            var lumberMillData2 = CreateProducerBuildingData("LumberMillData", "伐木场", lumberMillPrefab, 80, 40, 10, ResourceType.Wood, 1, 5f);
            var powerPlantData = CreateProducerBuildingData("PowerPlantData", "发电厂", powerPlantPrefab, 100, 80, 30, ResourceType.Power, 10, 5f);
            var farmData = CreateProducerBuildingData("FarmData", "农场", farmPrefab, 60, 30, 15, ResourceType.Food, 6, 5f);
            var ironMinePrefab = CreateProducerPrefab("IronMine", new Color(0.4f, 0.45f, 0.55f), new Vector3(1f, 1.45f, 1f));
            var copperMinePrefab = CreateProducerPrefab("CopperMine", new Color(0.7f, 0.45f, 0.2f), new Vector3(1f, 1.45f, 1f));
            var ironMineData = CreateProducerBuildingData("IronMineData", "铁矿场", ironMinePrefab, 80, 50, 20, ResourceType.Iron, 1, 5f);
            var copperMineData = CreateProducerBuildingData("CopperMineData", "铜矿场", copperMinePrefab, 80, 50, 15, ResourceType.Copper, 1, 5f);

            // Set power costs and scaling
            goldMineData.buildCost = new[]
            {
                new ResourceAmount { type = ResourceType.Gold, amount = 40 },
                new ResourceAmount { type = ResourceType.Wood, amount = 40 },
                new ResourceAmount { type = ResourceType.Food, amount = 2 }
            };
            goldMineData.powerCost = 0;
            goldMineData.populationCost = 0;
            goldMineData.extraProduction = new[]
            {
                new ResourceAmount { type = ResourceType.Gold, amount = 4 }
            };
            wallData.populationCost = 1;
            wallData.canRotatePlacement = true;
            towerData.populationCost = 2;
            lumberMillData2.powerCost = 2;
            lumberMillData2.populationCost = 2;
            lumberMillData2.scaleWithContent = Jiangshi.Grid.CellContent.Forest;
            ironMineData.powerCost = 2;
            ironMineData.populationCost = 2;
            ironMineData.scaleWithContent = Jiangshi.Grid.CellContent.IronOre;
            copperMineData.powerCost = 2;
            copperMineData.populationCost = 2;
            copperMineData.scaleWithContent = Jiangshi.Grid.CellContent.CopperOre;
            farmData.powerCost = 1;
            farmData.populationCost = 1;
            powerPlantData.populationCost = 2;
            EditorUtility.SetDirty(goldMineData);
            EditorUtility.SetDirty(lumberMillData2);
            EditorUtility.SetDirty(wallData);
            EditorUtility.SetDirty(towerData);
            EditorUtility.SetDirty(ironMineData);
            EditorUtility.SetDirty(copperMineData);
            EditorUtility.SetDirty(powerPlantData);
            EditorUtility.SetDirty(farmData);

            var soldierData = CreateUnitData("SoldierData", "士兵", soldierPrefab, 60, 3.2f, 10, 6f, 1f);
            var swordsmanPrefab = CreateUnitPrefab<Swordsman>("Swordsman", PrimitiveType.Capsule, new Color(0.7f, 0.3f, 0.8f), Faction.Player);
            var swordsmanData = CreateUnitData("SwordsmanData", "剑客", swordsmanPrefab, 75, 3.4f, 16, 1.35f, 0.9f);
            var zombieData = CreateUnitData("ZombieData", "僵尸", zombiePrefab, 35, 2f, 6, 1.2f, 1.1f);
            var fastZombiePrefab = CreateUnitPrefab<Zombie>("FastZombie", PrimitiveType.Capsule, new Color(0.9f, 0.25f, 0.15f), Faction.Enemy, new Vector3(0.75f, 0.75f, 0.75f));
            SetupSpriteAnimation(fastZombiePrefab, "Zombie", 4, 16);
            var fastZombieData = CreateUnitData("FastZombieData", "高速僵尸", fastZombiePrefab, 30, 4f, 4, 1.0f, 1.0f);

            var largeZombiePrefab = CreateUnitPrefab<Zombie>("LargeZombie", PrimitiveType.Capsule, new Color(0.6f, 0.25f, 0.7f), Faction.Enemy, new Vector3(1.5f, 1.5f, 1.5f));
            SetupSpriteAnimation(largeZombiePrefab, "Zombie", 4, 16);
            var largeZombieData = CreateUnitData("LargeZombieData", "大型僵尸", largeZombiePrefab, 120, 1f, 12, 1.5f, 1.3f);

            EditorUtility.SetDirty(fastZombieData);
            EditorUtility.SetDirty(largeZombieData);

            CreateWaveData("Wave01", 120f, 20, 0.35f, "第一波：僵尸接近中", 2, (zombieData, 20));
            CreateWaveData("Wave02", 240f, 30, 0.30f, "第二波：它们更快了", 2, (zombieData, 21), (fastZombieData, 9));
            CreateWaveData("Wave03", 390f, 35, 0.25f, "第三波：庞然大物出现", 3, (zombieData, 15), (fastZombieData, 12), (largeZombieData, 8));
            CreateWaveData("Wave04", 510f, 40, 0.20f, "第四波：最终冲击", 4, (zombieData, 10), (fastZombieData, 15), (largeZombieData, 15));

            // Set training costs
            soldierData.trainingCost = new[]
            {
                new ResourceAmount { type = ResourceType.Gold, amount = 30 },
                new ResourceAmount { type = ResourceType.Population, amount = 1 }
            };
            soldierData.trainingTime = 6f;
            swordsmanData.trainingCost = new[]
            {
                new ResourceAmount { type = ResourceType.Gold, amount = 45 },
                new ResourceAmount { type = ResourceType.Iron, amount = 15 },
                new ResourceAmount { type = ResourceType.Population, amount = 2 }
            };
            swordsmanData.trainingTime = 8f;
            EditorUtility.SetDirty(soldierData);
            EditorUtility.SetDirty(swordsmanData);

            var barracksPrefab = CreateBuildingPrefab("Barracks", PrimitiveType.Cube, new Vector3(1.2f, 1.45f, 1.2f), new Color(0.6f, 0.35f, 0.15f));
            var barracksData = CreateBuildingData("BarracksData", "兵工厂", barracksPrefab, Vector2Int.one, 150, 100, 50, false);
            barracksData.trainableUnits = new[] { soldierData, swordsmanData };
            barracksData.populationCost = 3;
            barracksData.powerCost = 3;
            EditorUtility.SetDirty(barracksData);

            towerData.powerCost = 2;
            EditorUtility.SetDirty(towerData);

            CreateObstaclePrefab("Forest", new Color(0.1f, 0.45f, 0.15f));
            CreateObstaclePrefab("IronOre", new Color(0.55f, 0.55f, 0.6f));
            CreateObstaclePrefab("CopperOre", new Color(0.8f, 0.5f, 0.25f));

            SoldierArtSetup.Apply();
            SwordsmanArtSetup.Apply();

            Selection.activeObject = towerData != null ? towerData : baseData;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Jiangshi/Setup/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (IsPlayModeBlocked("create the prototype scene"))
            {
                return;
            }

            EnsureFolders();
            CreatePrototypeAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Prototype";

            var systems = new GameObject("Systems");
            var gameManager = systems.AddComponent<GameManager>();
            var gridManager = systems.AddComponent<GridManager>();
            var resourceManager = systems.AddComponent<ResourceManager>();
            var survivalTimer = systems.AddComponent<SurvivalTimer>();
            var buildingManager = systems.AddComponent<BuildingManager>();
            var placementSystem = systems.AddComponent<PlacementSystem>();
            var unitManager = systems.AddComponent<UnitManager>();
            var waveManager = systems.AddComponent<WaveManager>();
            var mapGenerator = systems.AddComponent<MapGenerator>();
            var terrainGenerator = systems.AddComponent<TerrainGenerator>();
            var flowField = systems.AddComponent<Jiangshi.Pathfinding.FlowField>();
            var unitCommand = systems.AddComponent<Jiangshi.Units.UnitCommandController>();
            systems.AddComponent<Jiangshi.UI.TrainingPanel>();
            systems.AddComponent<Jiangshi.UI.DebugController>();

            _ = gameManager;
            _ = gridManager;
            _ = resourceManager;
            _ = survivalTimer;
            _ = buildingManager;
            _ = placementSystem;
            _ = unitManager;
            _ = waveManager;

            var camera = CreateCamera();
            CreateLight();
            CreateGround();

            var towerData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/TowerData.asset");
            var wallData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/WallData.asset");
            var baseData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/CommandBaseData.asset");
            var goldMineData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/GoldMineData.asset");
            var lumberMillData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/LumberMillData.asset");
            var barracksData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/BarracksData.asset");
            var powerPlantData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/PowerPlantData.asset");
            var farmData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/FarmData.asset");
            var ironMineData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/IronMineData.asset");
            var copperMineData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/CopperMineData.asset");
            var wave01 = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave01.asset");
            var wave02 = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave02.asset");
            var wave03 = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave03.asset");
            var wave04 = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataRoot}/Wave04.asset");
            var commandBase = CreateCommandBase(baseData);
            var spawnPoints = CreateSpawnPoints();

            SetObjectReference(flowField, "gridManager", gridManager);
            SetObjectReference(flowField, "target", commandBase.transform);

            SetObjectReference(placementSystem, "worldCamera", camera);
            SetObjectReference(placementSystem, "gridManager", gridManager);
            SetObjectReference(placementSystem, "resourceManager", resourceManager);
            SetObjectReference(placementSystem, "buildingManager", buildingManager);
            SetObjectReference(placementSystem, "selectedBuilding", towerData);
            SetObjectArray(placementSystem, "buildingOptions", new Object[] { baseData, wallData, towerData, goldMineData, lumberMillData, barracksData, powerPlantData, farmData, ironMineData, copperMineData });
            SetObjectReference(unitCommand, "worldCamera", camera);
            SetObjectReference(survivalTimer, "gameManager", gameManager);
            SetFloat(survivalTimer, "durationSeconds", 600f);
            SetResourceArray(resourceManager, "startingResources", new[]
            {
                new ResourceAmount { type = ResourceType.Gold, amount = 200 },
                new ResourceAmount { type = ResourceType.Wood, amount = 50 },
                new ResourceAmount { type = ResourceType.Food, amount = 10 },
                new ResourceAmount { type = ResourceType.Power, amount = 30 }
            });
            SetObjectReference(buildingManager, "gridManager", gridManager);

            SetObjectReference(waveManager, "unitManager", unitManager);
            SetObjectReference(waveManager, "gridManager", gridManager);
            SetObjectReference(waveManager, "defaultTarget", commandBase.transform);
            SetObjectArray(waveManager, "spawnPoints", spawnPoints);
            SetObjectArray(waveManager, "waves", new Object[] { wave01, wave02, wave03, wave04 });
            SetBool(waveManager, "enableCorridorMission", true);
            SetInt(waveManager, "corridorTriggerAfterWave", 2);

            var corridorPortalPrefab = CreateCorridorPortalPrefab();
            SetObjectReference(waveManager, "corridorPortalPrefab", corridorPortalPrefab);

            var forestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Forest.prefab");
            var ironOrePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/IronOre.prefab");
            var copperOrePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/CopperOre.prefab");
            SetObjectReference(mapGenerator, "gridManager", gridManager);
            SetObjectReference(mapGenerator, "terrainGenerator", terrainGenerator);
            SetObjectReference(mapGenerator, "forestPrefab", forestPrefab);
            SetObjectArray(mapGenerator, "forestSprites", LoadForestSprites());
            SetObjectReference(mapGenerator, "ironOrePrefab", ironOrePrefab);
            SetObjectReference(mapGenerator, "copperOrePrefab", copperOrePrefab);
            SetObjectReference(mapGenerator, "zombieData", AssetDatabase.LoadAssetAtPath<UnitData>($"{UnitDataRoot}/ZombieData.asset"));
            SetObjectReference(mapGenerator, "unitManager", unitManager);
            SetInt(mapGenerator, "initialZombieCount", 64);
            SetInt(mapGenerator, "initialZombieEdgeBand", 14);
            SetInt(mapGenerator, "initialZombieBoundaryMargin", 3);
            SetFloat(mapGenerator, "initialZombiePositionJitter", 0.35f);
            SetInt(mapGenerator, "initialZombieSpawnAttempts", 80);
            SetObjectReference(mapGenerator, "defaultTarget", commandBase.transform);

            SetObjectReference(terrainGenerator, "gridManager", gridManager);
            SetInt(terrainGenerator, "terrainSortingOrder", -5);
            var grassTiles = CreateTerrainTileset("Grass", new Color(0.28f, 0.45f, 0.2f), new Color(0.22f, 0.35f, 0.15f));
            var snowTiles = CreateTerrainTileset("Snow", new Color(0.9f, 0.92f, 0.95f), new Color(0.7f, 0.75f, 0.8f));
            var dirtTiles = CreateTerrainTileset("Dirt", new Color(0.55f, 0.4f, 0.22f), new Color(0.4f, 0.28f, 0.14f));
            var waterTiles = CreateTerrainTileset("Water", new Color(0.15f, 0.3f, 0.55f), new Color(0.1f, 0.2f, 0.4f));
            SetObjectArray(terrainGenerator, "grassTileset", grassTiles);
            SetObjectArray(terrainGenerator, "snowTileset", snowTiles);
            SetObjectArray(terrainGenerator, "dirtTileset", dirtTiles);
            SetObjectArray(terrainGenerator, "waterTileset", waterTiles);

            CreatePrototypeHud(gameManager, survivalTimer, placementSystem, resourceManager, waveManager, commandBase);

            var scenePath = $"{SceneRoot}/Prototype.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EnsureSceneInBuildSettings(scenePath);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Jiangshi", $"Prototype scene created:\n{scenePath}", "OK");
        }

        [MenuItem("Jiangshi/Setup/Add Or Refresh HUD In Current Scene")]
        public static void AddOrRefreshHudInCurrentScene()
        {
            if (IsPlayModeBlocked("add or refresh the HUD"))
            {
                return;
            }

            var gameManager = Object.FindObjectOfType<GameManager>();
            var survivalTimer = Object.FindObjectOfType<SurvivalTimer>();
            var placementSystem = Object.FindObjectOfType<PlacementSystem>();
            var resourceManager = Object.FindObjectOfType<ResourceManager>();
            var waveManager = Object.FindObjectOfType<WaveManager>();
            var commandBase = FindCommandBaseInCurrentScene();

            if (gameManager == null || placementSystem == null || resourceManager == null || waveManager == null || commandBase == null)
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Jiangshi",
                        "Current scene is missing required prototype objects. Run Jiangshi/Setup/Create Prototype Scene first.",
                        "OK");
                }

                return;
            }

            if (survivalTimer == null)
            {
                survivalTimer = gameManager.gameObject.AddComponent<SurvivalTimer>();
            }

            SetObjectReference(survivalTimer, "gameManager", gameManager);

            var gridManager = Object.FindObjectOfType<GridManager>();
            var buildingManager = Object.FindObjectOfType<BuildingManager>();
            if (gridManager != null && buildingManager != null)
            {
                SetObjectReference(buildingManager, "gridManager", gridManager);
            }

            var existingHud = GameObject.Find("Prototype HUD");
            if (existingHud != null)
            {
                Object.DestroyImmediate(existingHud);
            }

            CreatePrototypeHud(gameManager, survivalTimer, placementSystem, resourceManager, waveManager, commandBase);

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            AssetDatabase.Refresh();
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Jiangshi", "Prototype HUD refreshed in the current scene.", "OK");
            }
        }

        public static void RefreshPrototypeHudSceneForBatch()
        {
            var scenePath = $"{SceneRoot}/Prototype.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AddOrRefreshHudInCurrentScene();
        }

        private static bool IsPlayModeBlocked(string actionName)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            EditorUtility.DisplayDialog(
                "Jiangshi",
                $"Exit Play Mode before trying to {actionName}.",
                "OK");
            return true;
        }

        private static GameObject FindCommandBaseInCurrentScene()
        {
            var baseData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/CommandBaseData.asset");
            foreach (var building in Object.FindObjectsOfType<Jiangshi.Building.Building>())
            {
                if (building != null && building.Data != null && building.Data.triggersDefeatOnDestroyed)
                {
                    return building.gameObject;
                }
            }

            var commandBase = GameObject.Find("CommandBase");
            if (commandBase == null)
            {
                return null;
            }

            var damageable = commandBase.GetComponent<Damageable>();
            if (damageable == null)
            {
                commandBase.AddComponent<Damageable>();
            }

            var factionMember = commandBase.GetComponent<FactionMember>();
            if (factionMember == null)
            {
                factionMember = commandBase.AddComponent<FactionMember>();
            }

            factionMember.SetFaction(Faction.Player);

            var buildingComponent = commandBase.GetComponent<Jiangshi.Building.Building>();
            if (buildingComponent == null)
            {
                buildingComponent = commandBase.AddComponent<Jiangshi.Building.Building>();
            }

            if (baseData != null)
            {
                SetObjectReference(buildingComponent, "data", baseData);
            }

            return commandBase;
        }

        private static GameObject CreateCorridorPortalPrefab()
        {
            var path = $"{PrefabRoot}/CorridorPortal.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            var instance = new GameObject("CorridorPortal");
            instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var sr = instance.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateColorSprite("Portal", new Color(0.3f, 0.9f, 1f, 0.7f), 64);
            sr.sortingOrder = 100;
            instance.transform.localScale = new Vector3(2f, 2f, 1f);
            var collider = instance.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            var rb = instance.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            instance.AddComponent<Jiangshi.Units.CorridorPortal>();
            return SavePrefab(instance, path);
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory(SceneRoot);
            Directory.CreateDirectory(BuildingDataRoot);
            Directory.CreateDirectory(UnitDataRoot);
            Directory.CreateDirectory(WaveDataRoot);
        }

        private static GameObject CreateBuildingPrefab(string name, PrimitiveType primitiveType, Vector3 scale, Color color, GameObject projectilePrefab = null)
        {
            var instance = new GameObject(name);
            instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var sr = instance.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateColorSprite(name, color);
            instance.transform.localScale = scale;
            instance.AddComponent<BoxCollider>().size = Vector3.one;
            instance.AddComponent<Damageable>();
            instance.AddComponent<HitFlash>();
            instance.AddComponent<DeathEffect>();
            instance.AddComponent<FactionMember>().SetFaction(Faction.Player);
            instance.AddComponent<Jiangshi.Building.Building>();
            instance.AddComponent<Jiangshi.UI.UnitHealthBar>();

            if (name == "Tower")
            {
                var atk = instance.AddComponent<AttackController>();
                if (projectilePrefab != null)
                {
                    var saved = SavePrefab(instance, $"{PrefabRoot}/{name}.prefab");
                    var atkComp = saved.GetComponent<AttackController>();
                    var projComp = projectilePrefab.GetComponent<Projectile>();
                    SetObjectReference(atkComp, "projectilePrefab", projComp);
                    return saved;
                }
            }

            return SavePrefab(instance, $"{PrefabRoot}/{name}.prefab");
        }

        private static GameObject CreateProjectilePrefab()
        {
            var instance = new GameObject("Projectile");
            var sr = instance.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateColorSprite("Projectile", new Color(1f, 0.4f, 0.1f), 8);
            sr.sortingOrder = 80;
            instance.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            instance.AddComponent<Projectile>();
            return SavePrefab(instance, $"{PrefabRoot}/Projectile.prefab");
        }

        private static GameObject CreateProducerPrefab(string name, Color color, Vector3 scale)
        {
            var instance = new GameObject(name);
            instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            instance.transform.localScale = scale;
            var sr = instance.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateColorSprite(name, color);
            instance.AddComponent<BoxCollider>().size = Vector3.one;
            instance.AddComponent<Damageable>();
            instance.AddComponent<HitFlash>();
            instance.AddComponent<DeathEffect>();
            instance.AddComponent<FactionMember>().SetFaction(Faction.Player);
            instance.AddComponent<Jiangshi.Building.Building>();
            instance.AddComponent<Jiangshi.UI.UnitHealthBar>();
            instance.AddComponent<ResourceProducer>();
            return SavePrefab(instance, $"{PrefabRoot}/{name}.prefab");
        }

        private static GameObject CreateObstaclePrefab(string name, Color color)
        {
            var instance = new GameObject(name);
            instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var sr = instance.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateColorSprite(name, color);
            var collider = instance.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            collider.enabled = !name.EndsWith("Ore", System.StringComparison.Ordinal);
            return SavePrefab(instance, $"{PrefabRoot}/{name}.prefab");
        }

        private static Object[] CreateTerrainTileset(string name, Color center, Color edge)
        {
            var tilesetPath = $"Assets/Art/Sprites/Terrain_{name}_Tileset.png";
            if (System.IO.File.Exists(tilesetPath))
            {
                var slicedSheet = SpriteFactory.SliceGridSpriteSheet($"Terrain_{name}_Tileset", 3, 3);
                if (slicedSheet != null && slicedSheet.Length >= 9)
                {
                    var result = new Object[9];
                    for (var i = 0; i < 9; i++) result[i] = slicedSheet[i];
                    return result;
                }
            }

            // Fallback: generate colored tiles
            var positions = new[] { "TL", "T", "TR", "L", "C", "R", "BL", "B", "BR" };
            var sprites = new Object[9];

            for (var i = 0; i < 9; i++)
            {
                var col = i % 3;
                var row = i / 3;
                var color = Color.Lerp(center, edge,
                    (col == 1 && row == 1) ? 0f :
                    (col == 1 || row == 1) ? 0.3f :
                    0.5f);
                sprites[i] = SpriteFactory.CreateColorSprite($"Terrain_{name}_{positions[i]}", color);
            }

            return sprites;
        }

        private static Object[] LoadForestSprites()
        {
            var paths = new[]
            {
                "Assets/Art/Sprites/Forest.png",
                "Assets/Art/Sprites/橙色树.png",
                "Assets/Art/Sprites/红色树.png",
                "Assets/Art/Sprites/枯树.png"
            };

            var sprites = new System.Collections.Generic.List<Object>();
            foreach (var path in paths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites.ToArray();
        }

        private static void SetupSpriteAnimation(GameObject prefab, string spriteName, int frameCount, int frameSize = 32)
        {
            var frames = SpriteFactory.SliceSpriteSheet(spriteName, frameCount, frameSize);
            if (frames == null || frames.Length == 0) return;

            var sr = prefab.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = frames[0];

            var animator = prefab.GetComponent<Jiangshi.UI.SpriteAnimator>();
            if (animator == null)
            {
                // Need to instantiate, modify, and re-save
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.GetComponent<SpriteRenderer>().sprite = frames[0];
                var anim = instance.AddComponent<Jiangshi.UI.SpriteAnimator>();
                anim.SetFrames(frames);
                PrefabUtility.SaveAsPrefabAsset(instance, AssetDatabase.GetAssetPath(prefab));
                Object.DestroyImmediate(instance);
            }
        }

        private static BuildingData CreateProducerBuildingData(
            string assetName, string displayName, GameObject prefab,
            int health, int gold, int wood,
            ResourceType produceType, int produceAmount, float produceInterval)
        {
            var data = CreateBuildingData(assetName, displayName, prefab, Vector2Int.one, health, gold, wood, false);
            data.produceType = produceType;
            data.produceAmount = produceAmount;
            data.produceInterval = produceInterval;
            EditorUtility.SetDirty(data);
            return data;
        }

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

        private static GameObject SavePrefab(GameObject instance, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static void ApplyMaterial(GameObject target, Color color)
        {
            var renderer = target.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = color
            };
            renderer.sharedMaterial = material;
        }

        private static BuildingData CreateBuildingData(
            string assetName,
            string displayName,
            GameObject prefab,
            Vector2Int size,
            int health,
            int gold,
            int wood,
            bool triggersDefeatOnDestroyed)
        {
            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.displayName = displayName;
            data.prefab = prefab;
            data.size = size;
            data.maxHealth = health;
            data.blocksMovement = true;
            data.triggersDefeatOnDestroyed = triggersDefeatOnDestroyed;
            data.buildCost = new[]
            {
                new ResourceAmount { type = ResourceType.Gold, amount = gold },
                new ResourceAmount { type = ResourceType.Wood, amount = wood }
            };

            SaveAsset(data, $"{BuildingDataRoot}/{assetName}.asset");
            return data;
        }

        private static UnitData CreateUnitData(string assetName, string displayName, GameObject prefab, int health, float speed, int damage, float range, float interval)
        {
            var data = ScriptableObject.CreateInstance<UnitData>();
            data.displayName = displayName;
            data.prefab = prefab;
            data.maxHealth = health;
            data.moveSpeed = speed;
            data.attackDamage = damage;
            data.attackRange = range;
            data.attackInterval = interval;
            data.trainingCost = new[]
            {
                new ResourceAmount { type = ResourceType.Gold, amount = 25 }
            };
            data.trainingTime = 5f;

            SaveAsset(data, $"{UnitDataRoot}/{assetName}.asset");
            return data;
        }

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

        private static void SaveAsset(Object asset, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(asset, path);
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(64f, 72f, -6f);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 18f;
            cameraObject.AddComponent<Jiangshi.UI.RtsCameraController>();
            return camera;
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(64f, -0.02f, 64f);
            ground.transform.localScale = new Vector3(12.8f, 1f, 12.8f);
            ground.GetComponent<MeshRenderer>().enabled = false;
        }

        private static void CreatePrototypeHud(
            GameManager gameManager,
            SurvivalTimer survivalTimer,
            PlacementSystem placementSystem,
            ResourceManager resourceManager,
            WaveManager waveManager,
            GameObject commandBase)
        {
            var canvasObject = new GameObject("Prototype HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            var hud = canvasObject.AddComponent<PrototypeHud>();

            var statusPanel = CreatePanel(
                canvasObject.transform,
                "Status Panel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(376f, 176f),
                new Color(0.035f, 0.047f, 0.052f, 0.92f));

            CreateAccentBar(statusPanel.transform, "Status Accent", new Vector2(18f, -12f), new Vector2(96f, 4f), new Color(0.1f, 0.72f, 0.7f, 1f));
            CreateInsetBox(statusPanel.transform, "Resource Surface", new Vector2(14f, -54f), new Vector2(348f, 78f), new Color(0.08f, 0.105f, 0.12f, 0.84f));
            CreateInsetBox(statusPanel.transform, "Base Surface", new Vector2(14f, -138f), new Vector2(348f, 34f), new Color(0.075f, 0.095f, 0.11f, 0.78f));

            var gameStateText = CreateText(statusPanel.transform, "Game State Text", "状态: 启动中", 18, new Vector2(18f, -24f), new Vector2(190f, 26f));
            gameStateText.color = new Color(0.8f, 0.93f, 0.9f, 1f);
            var pauseButton = CreateButton(statusPanel.transform, "Pause Button", "暂停", Vector2.zero, new Vector2(106f, 36f));
            SetupRect(pauseButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(260f, -18f), new Vector2(96f, 34f));
            var pauseButtonLabel = pauseButton.GetComponentInChildren<Text>();
            pauseButtonLabel.text = "暂停";
            var goldText = CreateText(statusPanel.transform, "Gold Text", "金币: 0", 18, new Vector2(28f, -66f), new Vector2(150f, 24f));
            var woodText = CreateText(statusPanel.transform, "Wood Text", "木材: 0", 18, new Vector2(196f, -66f), new Vector2(150f, 24f));
            var foodText = CreateText(statusPanel.transform, "Food Text", "食物: 0", 18, new Vector2(28f, -100f), new Vector2(150f, 24f));
            var powerText = CreateText(statusPanel.transform, "Power Text", "电力: 0  人口: 0", 18, new Vector2(196f, -100f), new Vector2(150f, 24f));
            var baseHealthText = CreateText(statusPanel.transform, "Base Health Text", "基地: 0/0", 18, new Vector2(28f, -140f), new Vector2(316f, 24f));

            var objectivePanel = CreatePanel(
                canvasObject.transform,
                "Objective Panel",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(520f, 112f),
                new Color(0.035f, 0.047f, 0.052f, 0.90f));

            CreateAccentBar(objectivePanel.transform, "Objective Accent", new Vector2(28f, -12f), new Vector2(96f, 4f), new Color(0.92f, 0.68f, 0.25f, 1f));
            var survivalText = CreateText(objectivePanel.transform, "Survival Text", "存活: 10:00", 28, new Vector2(26f, -28f), new Vector2(468f, 34f));
            survivalText.alignment = TextAnchor.MiddleCenter;
            survivalText.color = new Color(0.95f, 0.86f, 0.66f, 1f);
            var waveStatusText = CreateText(objectivePanel.transform, "Wave Status Text", "无波次", 18, new Vector2(28f, -70f), new Vector2(464f, 26f));
            waveStatusText.alignment = TextAnchor.MiddleCenter;

            var buildPanel = CreatePanel(
                canvasObject.transform,
                "Build Panel",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(24f, 24f),
                new Vector2(640f, 286f),
                new Color(0.035f, 0.047f, 0.052f, 0.92f));

            CreateAccentBar(buildPanel.transform, "Build Accent", new Vector2(18f, -12f), new Vector2(72f, 4f), new Color(0.92f, 0.68f, 0.25f, 1f));
            var buildTitle = CreateText(buildPanel.transform, "Build Title", "建造", 22, new Vector2(18f, -18f), new Vector2(284f, 28f));
            buildTitle.alignment = TextAnchor.MiddleLeft;
            buildTitle.color = new Color(0.95f, 0.86f, 0.66f, 1f);

            var compactButtonSize = new Vector2(292f, 38f);
            var baseButton = CreateButton(buildPanel.transform, "Command Base Build Button", "1. 指挥基地", Vector2.zero, compactButtonSize);
            var wallButton = CreateButton(buildPanel.transform, "Wall Build Button", "2. 城墙", Vector2.zero, compactButtonSize);
            var towerButton = CreateButton(buildPanel.transform, "Tower Build Button", "3. 箭塔", Vector2.zero, compactButtonSize);
            var goldMineButton = CreateButton(buildPanel.transform, "Gold Mine Build Button", "4. 金矿", Vector2.zero, compactButtonSize);
            var lumberMillButton = CreateButton(buildPanel.transform, "Lumber Mill Build Button", "5. 伐木场", Vector2.zero, compactButtonSize);
            var barracksButton = CreateButton(buildPanel.transform, "Barracks Build Button", "6. 兵工厂", Vector2.zero, compactButtonSize);
            var powerPlantButton = CreateButton(buildPanel.transform, "Power Plant Build Button", "7. 发电厂", Vector2.zero, compactButtonSize);
            var farmButton = CreateButton(buildPanel.transform, "Farm Build Button", "8. 农场", Vector2.zero, compactButtonSize);
            var ironMineButton = CreateButton(buildPanel.transform, "Iron Mine Build Button", "9. 铁矿场", Vector2.zero, compactButtonSize);
            var copperMineButton = CreateButton(buildPanel.transform, "Copper Mine Build Button", "0. 铜矿场", Vector2.zero, compactButtonSize);
            SetupRect(baseButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -58f), compactButtonSize);
            SetupRect(wallButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(324f, -58f), compactButtonSize);
            SetupRect(towerButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -102f), compactButtonSize);
            SetupRect(goldMineButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(324f, -102f), compactButtonSize);
            SetupRect(lumberMillButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -146f), compactButtonSize);
            SetupRect(barracksButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(324f, -146f), compactButtonSize);
            SetupRect(powerPlantButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -190f), compactButtonSize);
            SetupRect(farmButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(324f, -190f), compactButtonSize);
            SetupRect(ironMineButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -234f), compactButtonSize);
            SetupRect(copperMineButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(324f, -234f), compactButtonSize);
            var baseButtonLabel = baseButton.GetComponentInChildren<Text>();
            var wallButtonLabel = wallButton.GetComponentInChildren<Text>();
            var towerButtonLabel = towerButton.GetComponentInChildren<Text>();
            var goldMineButtonLabel = goldMineButton.GetComponentInChildren<Text>();
            var lumberMillButtonLabel = lumberMillButton.GetComponentInChildren<Text>();
            var barracksButtonLabel = barracksButton.GetComponentInChildren<Text>();
            var powerPlantButtonLabel = powerPlantButton.GetComponentInChildren<Text>();
            var farmButtonLabel = farmButton.GetComponentInChildren<Text>();
            var ironMineButtonLabel = ironMineButton.GetComponentInChildren<Text>();
            var copperMineButtonLabel = copperMineButton.GetComponentInChildren<Text>();
            baseButtonLabel.fontSize = 15;
            wallButtonLabel.fontSize = 15;
            towerButtonLabel.fontSize = 15;
            goldMineButtonLabel.fontSize = 15;
            lumberMillButtonLabel.fontSize = 15;
            barracksButtonLabel.fontSize = 15;
            powerPlantButtonLabel.fontSize = 15;
            farmButtonLabel.fontSize = 15;
            ironMineButtonLabel.fontSize = 15;
            copperMineButtonLabel.fontSize = 15;
            var baseData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/CommandBaseData.asset");
            var wallData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/WallData.asset");
            var towerData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/TowerData.asset");
            var goldMineData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/GoldMineData.asset");
            var lumberMillData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/LumberMillData.asset");
            var barracksData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/BarracksData.asset");
            var powerPlantData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/PowerPlantData.asset");
            var farmData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/FarmData.asset");
            var ironMineData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/IronMineData.asset");
            var copperMineData = AssetDatabase.LoadAssetAtPath<BuildingData>($"{BuildingDataRoot}/CopperMineData.asset");

            var defeatPanel = CreatePanel(
                canvasObject.transform,
                "Defeat Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(460f, 248f),
                new Color(0.045f, 0.055f, 0.062f, 0.94f));

            var defeatTitle = CreateText(defeatPanel.transform, "Defeat Title", "失败", 44, new Vector2(0f, 56f), new Vector2(360f, 58f));
            defeatTitle.alignment = TextAnchor.MiddleCenter;
            defeatTitle.text = "失败";
            defeatTitle.color = new Color(0.95f, 0.42f, 0.36f, 1f);

            var defeatMessage = CreateText(defeatPanel.transform, "Defeat Message", "指挥基地被摧毁了。", 22, new Vector2(0f, 6f), new Vector2(360f, 40f));
            defeatMessage.alignment = TextAnchor.MiddleCenter;

            var restartButton = CreateButton(defeatPanel.transform, "Restart Button", "重新开始", new Vector2(0f, -68f), new Vector2(180f, 48f));
            defeatPanel.SetActive(false);

            var victoryPanel = CreatePanel(
                canvasObject.transform,
                "Victory Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(460f, 248f),
                new Color(0.035f, 0.075f, 0.055f, 0.94f));

            var victoryTitle = CreateText(victoryPanel.transform, "Victory Title", "胜利", 44, new Vector2(0f, 56f), new Vector2(360f, 58f));
            victoryTitle.alignment = TextAnchor.MiddleCenter;
            victoryTitle.text = "胜利";
            victoryTitle.color = new Color(0.55f, 0.95f, 0.68f, 1f);

            var victoryMessage = CreateText(victoryPanel.transform, "Victory Message", "指挥基地存活了下来！", 22, new Vector2(0f, 6f), new Vector2(360f, 40f));
            victoryMessage.alignment = TextAnchor.MiddleCenter;

            var victoryRestartButton = CreateButton(victoryPanel.transform, "Victory Restart Button", "重新开始", new Vector2(0f, -68f), new Vector2(180f, 48f));
            victoryPanel.SetActive(false);

            SetObjectReference(hud, "gameManager", gameManager);
            SetObjectReference(hud, "survivalTimer", survivalTimer);
            SetObjectReference(hud, "placementSystem", placementSystem);
            SetObjectReference(hud, "resourceManager", resourceManager);
            SetObjectReference(hud, "waveManager", waveManager);
            SetObjectReference(hud, "commandBase", commandBase.GetComponent<Damageable>());
            SetObjectReference(hud, "gameStateText", gameStateText);
            SetObjectReference(hud, "goldText", goldText);
            SetObjectReference(hud, "woodText", woodText);
            SetObjectReference(hud, "foodText", foodText);
            SetObjectReference(hud, "powerText", powerText);
            SetObjectReference(hud, "baseHealthText", baseHealthText);
            SetObjectReference(hud, "survivalText", survivalText);
            SetObjectReference(hud, "waveStatusText", waveStatusText);
            SetObjectReference(hud, "defeatPanel", defeatPanel);
            SetObjectReference(hud, "victoryPanel", victoryPanel);
            SetObjectReference(hud, "pauseButton", pauseButton);
            SetObjectReference(hud, "pauseButtonLabel", pauseButtonLabel);
            SetObjectReference(hud, "restartButton", restartButton);
            SetObjectReference(hud, "victoryRestartButton", victoryRestartButton);
            SetObjectArray(hud, "buildButtons", new Object[] { baseButton, wallButton, towerButton, goldMineButton, lumberMillButton, barracksButton, powerPlantButton, farmButton, ironMineButton, copperMineButton });
            SetObjectArray(hud, "buildButtonLabels", new Object[] { baseButtonLabel, wallButtonLabel, towerButtonLabel, goldMineButtonLabel, lumberMillButtonLabel, barracksButtonLabel, powerPlantButtonLabel, farmButtonLabel, ironMineButtonLabel, copperMineButtonLabel });
            SetObjectArray(hud, "buildButtonData", new Object[] { baseData, wallData, towerData, goldMineData, lumberMillData, barracksData, powerPlantData, farmData, ironMineData, copperMineData });

            CreateEventSystemIfNeeded();
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            SetupRect(panel, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);

            var background = panel.AddComponent<RoundedBox>();
            background.CornerRadius = 20f;
            background.CornerSegments = 10;
            background.color = color;

            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.38f);
            shadow.effectDistance = new Vector2(0f, -8f);
            return panel;
        }

        private static GameObject CreateInsetBox(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var box = new GameObject(name);
            box.transform.SetParent(parent, false);
            SetupRect(box, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);

            var background = box.AddComponent<RoundedBox>();
            background.CornerRadius = 14f;
            background.CornerSegments = 8;
            background.color = color;
            return box;
        }

        private static GameObject CreateAccentBar(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            SetupRect(bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);

            var background = bar.AddComponent<RoundedBox>();
            background.CornerRadius = sizeDelta.y * 0.5f;
            background.CornerSegments = 4;
            background.color = color;
            return bar;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            SetupRect(textObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.9f, 0.94f, 0.92f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            SetupRect(buttonObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);

            var background = buttonObject.AddComponent<RoundedBox>();
            background.CornerRadius = 14f;
            background.CornerSegments = 8;
            background.color = new Color(0.11f, 0.2f, 0.27f, 0.96f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.14f, 1f);
            colors.pressedColor = new Color(0.74f, 0.82f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.44f, 0.44f, 0.82f);
            button.colors = colors;

            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.38f, 0.40f, 0.75f);
            outline.effectDistance = new Vector2(1f, -1f);

            var shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
            shadow.effectDistance = new Vector2(0f, -3f);

            var labelText = CreateText(buttonObject.transform, "Label", label, 22, Vector2.zero, sizeDelta);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.96f, 0.98f, 0.96f, 1f);
            SetupRect(labelText.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static void SetupRect(
            GameObject target,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = target.AddComponent<RectTransform>();
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static void CreateEventSystemIfNeeded()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (buildScenes.Any(scene => scene.path == scenePath))
            {
                return;
            }

            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        private static GameObject CreateCommandBase(BuildingData baseData)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/CommandBase.prefab");
            var instance = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            instance.name = "CommandBase";
            instance.transform.position = new Vector3(64.5f, 0.5f, 64.5f);
            instance.transform.localScale = new Vector3(3f, 1.35f, 3f);

            var building = instance.GetComponent<Jiangshi.Building.Building>();
            if (building != null && baseData != null)
            {
                SetObjectReference(building, "data", baseData);
            }

            return instance;
        }

        private static Transform[] CreateSpawnPoints()
        {
            var root = new GameObject("SpawnPoints");
            var positions = new[]
            {
                new Vector3(64f, 0f, -2f),
                new Vector3(64f, 0f, 130f),
                new Vector3(-2f, 0f, 64f),
                new Vector3(130f, 0f, 64f)
            };

            var spawnPoints = new Transform[positions.Length];
            for (var i = 0; i < positions.Length; i++)
            {
                var point = new GameObject($"SpawnPoint_{i + 1}");
                point.transform.SetParent(root.transform);
                point.transform.position = positions[i];
                spawnPoints[i] = point.transform;
            }

            return spawnPoints;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string propertyName, Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;

            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetResourceArray(Object target, string propertyName, ResourceAmount[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;

            for (var i = 0; i < values.Length; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("type").enumValueIndex = (int)values[i].type;
                element.FindPropertyRelative("amount").intValue = values[i].amount;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
