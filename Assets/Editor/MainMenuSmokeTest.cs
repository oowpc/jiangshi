using System.Diagnostics;
using Jiangshi.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Jiangshi.Editor
{
    public static class MainMenuSmokeTest
    {
        private const double MaxStartupSeconds = 30.0;
        private const double PlaySeconds = 2.0;
        private const string ActiveKey = "Jiangshi.MainMenuSmokeTest.Active";
        private const string StartedKey = "Jiangshi.MainMenuSmokeTest.Started";
        private const string FailureCountKey = "Jiangshi.MainMenuSmokeTest.FailureCount";

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

        public static void RunMainMenu()
        {
            failureCount = 0;
            playStarted = false;
            Stopwatch.Restart();
            SessionState.SetInt(ActiveKey, 1);
            SessionState.SetInt(StartedKey, 0);
            SessionState.SetInt(FailureCountKey, 0);

            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update += OnEditorUpdate;

            MainMenuSetupMenu.CreateMainMenuSceneForBatch();
            UnityEngine.Debug.Log($"MainMenuSmokeTest starting scene: {MainMenuSetupMenu.ScenePath}");
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
                    UnityEngine.Debug.Log("MainMenuSmokeTest entered play mode.");
                }
                else if (Stopwatch.Elapsed.TotalSeconds > MaxStartupSeconds)
                {
                    UnityEngine.Debug.LogError("Timed out waiting for main menu play mode to start.");
                    Exit(1);
                }

                return;
            }

            if (Stopwatch.Elapsed.TotalSeconds < PlaySeconds)
            {
                return;
            }

            CheckRuntimeObjectExists("Main Menu Canvas");
            CheckRuntimeObjectExists("Start Button");
            CheckRuntimeObjectExists("Main Settings Button");
            CheckRuntimeObjectExists("Main Menu Settings Panel");
            CheckRuntimeObjectExists("Main Menu Volume Slider");
            CheckRuntimeObjectExists("Main Menu Window Mode Button");
            CheckBackgroundTexture();
            CheckControllerExists();
            CheckResourceAudioClip("Audio/Menu/PrizeMenu");

            UnityEngine.Debug.Log($"MainMenuSmokeTest completed. Failures: {failureCount}");
            Exit(failureCount == 0 ? 0 : 1);
        }

        private static void CheckRuntimeObjectExists(string objectName)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == objectName)
                {
                    UnityEngine.Debug.Log($"{objectName} exists.");
                    return;
                }
            }

            UnityEngine.Debug.LogError($"{objectName} is missing.");
        }

        private static void CheckBackgroundTexture()
        {
            var background = GameObject.Find("Main Menu Background");
            var rawImage = background != null ? background.GetComponent<RawImage>() : null;
            if (rawImage == null || rawImage.texture == null)
            {
                UnityEngine.Debug.LogError("Main menu background texture is missing.");
                return;
            }

            UnityEngine.Debug.Log($"Main menu background texture assigned: {rawImage.texture.name}.");
        }

        private static void CheckControllerExists()
        {
            if (Object.FindObjectOfType<MainMenuController>() == null)
            {
                UnityEngine.Debug.LogError("MainMenuController is missing.");
                return;
            }

            UnityEngine.Debug.Log("MainMenuController exists.");
        }

        private static void CheckResourceAudioClip(string resourcePath)
        {
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                UnityEngine.Debug.LogError($"AudioClip resource is missing: {resourcePath}");
                return;
            }

            UnityEngine.Debug.Log($"AudioClip resource loaded: {resourcePath}");
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
