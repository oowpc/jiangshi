using System;
using System.Linq;
using Jiangshi.Building;
using Jiangshi.Combat;
using Jiangshi.Economy;
using Jiangshi.UI;
using Jiangshi.Units;
using UnityEditor;
using UnityEngine;

namespace Jiangshi.Editor
{
    [InitializeOnLoad]
    internal static class SwordsmanArtSetup
    {
        private const int FrameWidth = 384;
        private const int FrameHeight = 512;
        private const float PixelsPerUnit = 512f;
        private const string SwordsmanPrefabPath = "Assets/Prefabs/Swordsman.prefab";
        private const string SwordsmanDataPath = "Assets/ScriptableObjects/Units/SwordsmanData.asset";
        private const string BarracksDataPath = "Assets/ScriptableObjects/Buildings/BarracksData.asset";

        private static readonly AnimationSheet[] Sheets =
        {
            new("Assets/Art/Sprites/SwordsmanIdle.png", "SwordsmanIdle", 8, "idleFrames"),
            new("Assets/Art/Sprites/SwordsmanWalk.png", "SwordsmanWalk", 8, "walkFrames"),
            new("Assets/Art/Sprites/SwordsmanAttack.png", "SwordsmanAttack", 7, "attackFrames"),
            new("Assets/Art/Sprites/SwordsmanGuard.png", "SwordsmanGuard", 8, "reloadFrames"),
            new("Assets/Art/Sprites/SwordsmanDeath.png", "SwordsmanDeath", 7, "deathFrames")
        };

        static SwordsmanArtSetup()
        {
            EditorApplication.delayCall += ApplyIfNeeded;
        }

        [MenuItem("Jiangshi/Setup/Apply Swordsman Art")]
        public static void Apply()
        {
            foreach (var sheet in Sheets)
            {
                ConfigureSheet(sheet);
            }

            var prefab = CreateOrUpdatePrefab();
            var data = CreateOrUpdateUnitData(prefab);
            AddToBarracks(data);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!Sheets.All(sheet => AssetDatabase.LoadAssetAtPath<Texture2D>(sheet.Path) != null))
            {
                return;
            }

            if (NeedsImport() ||
                AssetDatabase.LoadAssetAtPath<GameObject>(SwordsmanPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<UnitData>(SwordsmanDataPath) == null)
            {
                Apply();
            }
        }

        private static bool NeedsImport()
        {
            foreach (var sheet in Sheets)
            {
                if (AssetImporter.GetAtPath(sheet.Path) is not TextureImporter importer)
                {
                    return true;
                }

                if (importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Multiple ||
                    Math.Abs(importer.spritePixelsPerUnit - PixelsPerUnit) > 0.01f ||
                    importer.spritesheet.Length != sheet.FrameCount)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConfigureSheet(AnimationSheet sheet)
        {
            if (AssetImporter.GetAtPath(sheet.Path) is not TextureImporter importer)
            {
                Debug.LogWarning($"Swordsman animation sheet missing: {sheet.Path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var sprites = new SpriteMetaData[sheet.FrameCount];
            for (var i = 0; i < sprites.Length; i++)
            {
                sprites[i] = new SpriteMetaData
                {
                    name = $"{sheet.SpriteName}_{i}",
                    rect = new Rect(i * FrameWidth, 0, FrameWidth, FrameHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }

            importer.spritesheet = sprites;
            importer.SaveAndReimport();
        }

        private static GameObject CreateOrUpdatePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordsmanPrefabPath);
            GameObject root;
            var isNew = prefab == null;

            if (isNew)
            {
                root = new GameObject("Swordsman");
                root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                root = PrefabUtility.LoadPrefabContents(SwordsmanPrefabPath);
            }

            try
            {
                root.name = "Swordsman";
                EnsureComponent<SpriteRenderer>(root);
                var collider = EnsureComponent<BoxCollider>(root);
                collider.size = Vector3.one;
                collider.center = Vector3.zero;
                EnsureComponent<Damageable>(root);
                EnsureComponent<HitFlash>(root);
                EnsureComponent<DeathEffect>(root);
                SetFaction(EnsureComponent<FactionMember>(root), Faction.Player);
                EnsureComponent<Swordsman>(root);
                EnsureComponent<UnitHealthBar>(root);
                var visualAnimator = EnsureComponent<UnitVisualAnimator>(root);
                BindAnimationFrames(root.GetComponent<SpriteRenderer>(), visualAnimator);

                if (isNew)
                {
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, SwordsmanPrefabPath);
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAsset(root, SwordsmanPrefabPath);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordsmanPrefabPath);
                }
            }
            finally
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
                else
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return prefab;
        }

        private static UnitData CreateOrUpdateUnitData(GameObject prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<UnitData>(SwordsmanDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UnitData>();
                AssetDatabase.CreateAsset(data, SwordsmanDataPath);
            }

            data.displayName = "剑客";
            data.prefab = prefab;
            data.maxHealth = 75;
            data.moveSpeed = 3.4f;
            data.attackDamage = 16;
            data.attackRange = 1.35f;
            data.attackInterval = 0.9f;
            data.trainingCost = new[]
            {
                new ResourceAmount { type = ResourceType.Gold, amount = 45 },
                new ResourceAmount { type = ResourceType.Iron, amount = 15 },
                new ResourceAmount { type = ResourceType.Population, amount = 2 }
            };
            data.trainingTime = 8f;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void AddToBarracks(UnitData swordsmanData)
        {
            var barracks = AssetDatabase.LoadAssetAtPath<BuildingData>(BarracksDataPath);
            if (barracks == null || swordsmanData == null)
            {
                return;
            }

            var units = barracks.trainableUnits ?? Array.Empty<UnitData>();
            if (units.Contains(swordsmanData))
            {
                return;
            }

            barracks.trainableUnits = units.Concat(new[] { swordsmanData }).ToArray();
            EditorUtility.SetDirty(barracks);
        }

        private static void BindAnimationFrames(SpriteRenderer spriteRenderer, UnitVisualAnimator visualAnimator)
        {
            var serializedObject = new SerializedObject(visualAnimator);
            foreach (var sheet in Sheets)
            {
                var sprites = LoadSprites(sheet);
                AssignSprites(serializedObject.FindProperty(sheet.SerializedField), sprites);

                if (sheet.SerializedField == "idleFrames" && sprites.Length > 0 && spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprites[0];
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite[] LoadSprites(AnimationSheet sheet)
        {
            return AssetDatabase.LoadAllAssetsAtPath(sheet.Path)
                .OfType<Sprite>()
                .Where(sprite => sprite.name.StartsWith(sheet.SpriteName, StringComparison.Ordinal))
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static void AssignSprites(SerializedProperty property, Sprite[] sprites)
        {
            if (property == null)
            {
                return;
            }

            property.arraySize = sprites.Length;
            for (var i = 0; i < sprites.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        private static T EnsureComponent<T>(GameObject root) where T : Component
        {
            return root.GetComponent<T>() ?? root.AddComponent<T>();
        }

        private static void SetFaction(FactionMember factionMember, Faction faction)
        {
            var serializedObject = new SerializedObject(factionMember);
            serializedObject.FindProperty("faction").enumValueIndex = (int)faction;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct AnimationSheet
        {
            public AnimationSheet(string path, string spriteName, int frameCount, string serializedField)
            {
                Path = path;
                SpriteName = spriteName;
                FrameCount = frameCount;
                SerializedField = serializedField;
            }

            public string Path { get; }
            public string SpriteName { get; }
            public int FrameCount { get; }
            public string SerializedField { get; }
        }
    }
}
