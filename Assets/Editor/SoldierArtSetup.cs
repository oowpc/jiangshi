using System;
using System.Linq;
using Jiangshi.Combat;
using Jiangshi.Units;
using UnityEditor;
using UnityEngine;

namespace Jiangshi.Editor
{
    [InitializeOnLoad]
    internal static class SoldierArtSetup
    {
        private const int FrameWidth = 384;
        private const int FrameHeight = 512;
        private const float PixelsPerUnit = 512f;
        private const string SoldierPrefabPath = "Assets/Prefabs/Soldier.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";

        private static readonly AnimationSheet[] Sheets =
        {
            new("Assets/Art/Sprites/SoldierIdle.png", "SoldierIdle", 8, "idleFrames"),
            new("Assets/Art/Sprites/SoldierWalk.png", "SoldierWalk", 8, "walkFrames"),
            new("Assets/Art/Sprites/SoldierShoot.png", "SoldierShoot", 6, "attackFrames"),
            new("Assets/Art/Sprites/SoldierReload.png", "SoldierReload", 6, "reloadFrames"),
            new("Assets/Art/Sprites/SoldierDeath.png", "SoldierDeath", 7, "deathFrames")
        };

        static SoldierArtSetup()
        {
            EditorApplication.delayCall += ApplyIfNeeded;
        }

        [MenuItem("Jiangshi/Setup/Apply Soldier Art")]
        public static void Apply()
        {
            foreach (var sheet in Sheets)
            {
                ConfigureSheet(sheet);
            }

            BindSoldierPrefab();
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

            if (NeedsImport() || NeedsPrefabBinding())
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

        private static bool NeedsPrefabBinding()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoldierPrefabPath);
            if (prefab == null)
            {
                return true;
            }

            var animator = prefab.GetComponent<UnitVisualAnimator>();
            if (animator == null)
            {
                return true;
            }

            var serializedObject = new SerializedObject(animator);
            foreach (var sheet in Sheets)
            {
                var property = serializedObject.FindProperty(sheet.SerializedField);
                if (property == null || property.arraySize != sheet.FrameCount)
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
                Debug.LogWarning($"Soldier animation sheet missing: {sheet.Path}");
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

        private static void BindSoldierPrefab()
        {
            var prefab = PrefabUtility.LoadPrefabContents(SoldierPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Soldier prefab missing: {SoldierPrefabPath}");
                return;
            }

            try
            {
                var spriteRenderer = prefab.GetComponent<SpriteRenderer>();
                var soldier = prefab.GetComponent<Soldier>();
                var visualAnimator = prefab.GetComponent<UnitVisualAnimator>();
                if (visualAnimator == null)
                {
                    visualAnimator = prefab.AddComponent<UnitVisualAnimator>();
                }

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
                BindProjectile(soldier);
                PrefabUtility.SaveAsPrefabAsset(prefab, SoldierPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static void BindProjectile(Soldier soldier)
        {
            if (soldier == null)
            {
                return;
            }

            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            var projectile = projectilePrefab != null ? projectilePrefab.GetComponent<Projectile>() : null;
            if (projectile == null)
            {
                Debug.LogWarning($"Projectile prefab missing: {ProjectilePrefabPath}");
                return;
            }

            var serializedObject = new SerializedObject(soldier);
            serializedObject.FindProperty("projectilePrefab").objectReferenceValue = projectile;
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
