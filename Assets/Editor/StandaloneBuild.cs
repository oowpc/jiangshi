using System.IO;
using UnityEditor;

namespace Jiangshi.Editor
{
    public static class StandaloneBuild
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const string SilentCorridorScenePath = "Assets/Scenes/SilentCorridor/SampleScene.unity";
        private const string OutputPath = "Builds/Windows/Jiangshi.exe";

        [MenuItem("Jiangshi/Build/Windows (x64)")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            UpgradeToURP.Upgrade();
            TMPCharacterFixer.AddCharactersToAllFonts();
            PrototypeSetupMenu.CreatePrototypeScene();

            var report = BuildPipeline.BuildPlayer(
                new[] { ScenePath, SilentCorridorScenePath },
                OutputPath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);

            var summary = report.summary;
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException($"Windows build failed: {summary.result}");
            }
        }
    }
}
