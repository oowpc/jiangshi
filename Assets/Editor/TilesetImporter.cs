using UnityEditor;
using UnityEngine;

namespace Jiangshi.Editor
{
    public static class TilesetImporter
    {
        [MenuItem("Jiangshi/Import Terrain Tilesets/Import All")]
        public static void ImportAllTerrainTilesets()
        {
            ImportGridTileset("Terrain_Grass_Tileset", "grassTileset");
            ImportGridTileset("Terrain_Snow_Tileset", "snowTileset");
            ImportGridTileset("Terrain_Dirt_Tileset", "dirtTileset");
            ImportGridTileset("Terrain_Water_Tileset", "waterTileset");
        }

        [MenuItem("Jiangshi/Import Terrain Tilesets/Import Grass")]
        public static void ImportGrass()
        {
            ImportGridTileset("Terrain_Grass_Tileset", "grassTileset");
        }

        [MenuItem("Jiangshi/Import Terrain Tilesets/Import Snow")]
        public static void ImportSnow()
        {
            ImportGridTileset("Terrain_Snow_Tileset", "snowTileset");
        }

        [MenuItem("Jiangshi/Import Terrain Tilesets/Import Dirt")]
        public static void ImportDirt()
        {
            ImportGridTileset("Terrain_Dirt_Tileset", "dirtTileset");
        }

        [MenuItem("Jiangshi/Import Terrain Tilesets/Import Water")]
        public static void ImportWater()
        {
            ImportGridTileset("Terrain_Water_Tileset", "waterTileset");
        }

        private static void ImportGridTileset(string spriteName, string propertyName)
        {
            var sprites = SpriteFactory.SliceGridSpriteSheet(spriteName, 3, 3);
            if (sprites == null || sprites.Length < 9)
            {
                Debug.LogError($"Assets/Art/Sprites/{spriteName}.png not found or could not be sliced as a 3x3 tileset.");
                return;
            }

            var terrainGen = Object.FindObjectOfType<Core.TerrainGenerator>();
            if (terrainGen == null)
            {
                Debug.LogError("TerrainGenerator not found in scene.");
                return;
            }

            var so = new SerializedObject(terrainGen);
            var prop = so.FindProperty(propertyName);
            prop.arraySize = 9;
            for (var i = 0; i < 9; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(terrainGen);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrainGen.gameObject.scene);
            Debug.Log($"{spriteName} imported and assigned to {propertyName}.");
        }
    }
}
