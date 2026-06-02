using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jiangshi.Editor
{
    public static class PlayModeSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const double MaxStartupSeconds = 30.0;
        private const double PlaySeconds = 15.0;
        private const string ActiveKey = "Jiangshi.PlayModeSmokeTest.Active";
        private const string StartedKey = "Jiangshi.PlayModeSmokeTest.Started";
        private const string FailureCountKey = "Jiangshi.PlayModeSmokeTest.FailureCount";

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

            UnityEngine.Debug.Log($"PlayModeSmokeTest starting scene: {ScenePath}");
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
                    UnityEngine.Debug.Log("PlayModeSmokeTest entered play mode.");
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

            UnityEngine.Debug.Log($"PlayModeSmokeTest completed. Failures: {failureCount}");
            Exit(failureCount == 0 ? 0 : 1);
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
