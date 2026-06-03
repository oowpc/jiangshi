using UnityEditor;
using UnityEngine;

namespace Jiangshi.Editor
{
    public static class SpriteFactory
    {
        private const string SpriteRoot = "Assets/Art/Sprites";

        public static Sprite CreateColorSprite(string name, Color color, int size = 32)
        {
            System.IO.Directory.CreateDirectory(SpriteRoot);

            // If a hand-drawn sprite already exists, use it
            var existing = LoadExistingSprite(name);
            if (existing != null) return existing;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            var path = $"{SpriteRoot}/{name}.png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Sprite LoadExistingSprite(string name)
        {
            var path = $"{SpriteRoot}/{name}.png";
            if (!System.IO.File.Exists(path)) return null;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Sprite[] SliceSpriteSheet(string name, int frameCount, int frameSize = 32)
        {
            var path = $"{SpriteRoot}/{name}.png";
            if (!System.IO.File.Exists(path)) return null;

            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return null;

            var cols = tex.width / frameSize;
            var rows = tex.height / frameSize;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = frameSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var count = Mathf.Min(frameCount, cols * rows);
            var rects = new SpriteMetaData[count];
            for (var i = 0; i < count; i++)
            {
                var col = i % cols;
                var row = i / cols;
                // Unity texture Y starts from bottom, so flip row
                var yPos = tex.height - (row + 1) * frameSize;
                rects[i] = new SpriteMetaData
                {
                    name = $"{name}_{i}",
                    rect = new Rect(col * frameSize, yPos, frameSize, frameSize),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }

#pragma warning disable 618
            importer.spritesheet = rects;
#pragma warning restore 618
            importer.SaveAndReimport();

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var asset in assets)
            {
                if (asset is Sprite s)
                    sprites.Add(s);
            }
            sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return sprites.ToArray();
        }

        public static Sprite[] SliceGridSpriteSheet(string name, int columns, int rows)
        {
            var path = $"{SpriteRoot}/{name}.png";
            if (!System.IO.File.Exists(path)) return null;

            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null || columns <= 0 || rows <= 0) return null;

            var tileWidth = tex.width / columns;
            var tileHeight = tex.height / rows;
            if (tileWidth <= 0 || tileHeight <= 0) return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = Mathf.Max(tileWidth, tileHeight);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;

            var rects = new SpriteMetaData[columns * rows];
            for (var i = 0; i < rects.Length; i++)
            {
                var col = i % columns;
                var row = i / columns;
                var yPos = tex.height - (row + 1) * tileHeight;
                rects[i] = new SpriteMetaData
                {
                    name = $"{name}_{i:00}",
                    rect = new Rect(col * tileWidth, yPos, tileWidth, tileHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }

#pragma warning disable 618
            importer.spritesheet = rects;
#pragma warning restore 618
            importer.SaveAndReimport();

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var asset in assets)
            {
                if (asset is Sprite s)
                    sprites.Add(s);
            }

            sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return sprites.ToArray();
        }
    }
}
