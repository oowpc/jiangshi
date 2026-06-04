using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Jiangshi.Editor
{
    public static class UpgradeToURP
    {
        private const string PipelineAssetPath = "Assets/UniversalRenderPipelineAsset.asset";
        private const string RendererAssetPath = "Assets/UniversalRendererAsset.asset";

        [MenuItem("Jiangshi/Setup/Upgrade Project to URP")]
        public static void Upgrade()
        {
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "UniversalRendererData";
            AssetDatabase.CreateAsset(rendererData, RendererAssetPath);

            var pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            pipelineAsset.name = "UniversalRenderPipelineAsset";
            AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);

            GraphicsSettings.renderPipelineAsset = pipelineAsset;

            var materials = AssetDatabase.FindAssets("t:Material");
            foreach (var guid in materials)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) continue;

                var shaderName = material.shader != null ? material.shader.name : "";
                if (shaderName == "Universal Render Pipeline/Lit" || shaderName == "Hidden/Universal Render Pipeline/FallbackError")
                    continue;

                if (shaderName.StartsWith("TextMeshPro") || shaderName.StartsWith("Hidden/TextMeshPro"))
                    continue;

                if (shaderName == "Sprites/Default" || shaderName == "Universal Render Pipeline/2D/Sprite-Lit-Default")
                    continue;

                material.shader = Shader.Find("Universal Render Pipeline/Lit");
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            FixTMPFontMaterials();
        }

        private static void FixTMPFontMaterials()
        {
            var fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (var guid in fontGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
                if (fontAsset == null || fontAsset.material == null) continue;

                var mat = fontAsset.material;
                if (mat.shader.name.StartsWith("TextMeshPro")) continue;

                mat.shader = Shader.Find("TextMeshPro/Distance Field");
                EditorUtility.SetDirty(mat);
                EditorUtility.SetDirty(fontAsset);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
