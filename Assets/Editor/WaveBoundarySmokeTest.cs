using System.Diagnostics;
using Jiangshi.Grid;
using Jiangshi.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jiangshi.Editor
{
    public static class WaveBoundarySmokeTest
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const double MaxStartupSeconds = 30.0;
        private const double PlaySeconds = 42.0;
        private const string ActiveKey = "Jiangshi.WaveBoundarySmokeTest.Active";
        private const string StartedKey = "Jiangshi.WaveBoundarySmokeTest.Started";
        private const string FailureCountKey = "Jiangshi.WaveBoundarySmokeTest.FailureCount";

        private static readonly Stopwatch Stopwatch = new();
        private static int failureCount;
        private static bool playStarted;

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (SessionState.GetInt(ActiveKey, 0) == 0)
            {
                return;
            }

            failureCount = SessionState.GetInt(FailureCountKey, 0);
            playStarted = SessionState.GetInt(StartedKey, 0) != 0;
            Stopwatch.Restart();
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        public static void RunPrototype()
        {
            failureCount = 0;
            playStarted = false;
            Stopwatch.Restart();
            SessionState.SetInt(ActiveKey, 1);
            SessionState.SetInt(StartedKey, 0);
            SessionState.SetInt(FailureCountKey, 0);

            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update += OnEditorUpdate;

            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"Failed to open scene: {ScenePath}");
                Exit(1);
                return;
            }

            UnityEngine.Debug.Log($"WaveBoundarySmokeTest starting scene: {ScenePath}");
            EditorApplication.EnterPlaymode();
        }

        private static void OnEditorUpdate()
        {
            if (!playStarted)
            {
                if (EditorApplication.isPlaying)
                {
                    playStarted = true;
                    SessionState.SetInt(StartedKey, 1);
                    Stopwatch.Restart();
                    UnityEngine.Debug.Log("WaveBoundarySmokeTest entered play mode.");
                }
                else if (Stopwatch.Elapsed.TotalSeconds > MaxStartupSeconds)
                {
                    UnityEngine.Debug.LogError("Timed out waiting for play mode to start.");
                    Exit(1);
                }

                return;
            }

            if (Stopwatch.Elapsed.TotalSeconds < PlaySeconds)
            {
                return;
            }

            CheckZombiesInsideMap();
            UnityEngine.Debug.Log($"WaveBoundarySmokeTest completed. Failures: {failureCount}");
            Exit(failureCount == 0 ? 0 : 1);
        }

        private static void CheckZombiesInsideMap()
        {
            var grid = Object.FindObjectOfType<GridManager>();
            if (grid == null)
            {
                UnityEngine.Debug.LogError("WaveBoundarySmokeTest could not find GridManager.");
                return;
            }

            var outsideCount = 0;
            foreach (var zombie in Object.FindObjectsOfType<Zombie>())
            {
                var position = grid.WorldToGrid(zombie.transform.position);
                if (!grid.IsInside(position))
                {
                    outsideCount++;
                }
            }

            if (outsideCount > 0)
            {
                UnityEngine.Debug.LogError($"WaveBoundarySmokeTest found {outsideCount} zombies outside the map after the first wave.");
            }
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                failureCount++;
                SessionState.SetInt(FailureCountKey, failureCount);
            }
        }

        private static void Exit(int exitCode)
        {
            SessionState.EraseInt(ActiveKey);
            SessionState.EraseInt(StartedKey);
            SessionState.EraseInt(FailureCountKey);
            Application.logMessageReceived -= OnLogMessageReceived;
            EditorApplication.update -= OnEditorUpdate;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            EditorApplication.Exit(exitCode);
        }
    }
}
