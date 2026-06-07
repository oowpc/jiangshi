using System.Diagnostics;
using Jiangshi.UI;
using Jiangshi.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Jiangshi.Editor
{
    public static class SceneTransitionSmokeTest
    {
        private const string PrototypeScenePath = "Assets/Scenes/Prototype.unity";
        private const string PrototypeSceneName = "Prototype";
        private const string CorridorSceneName = "SampleScene";
        private const double MaxPhaseSeconds = 45.0;
        private const string ActiveKey = "Jiangshi.SceneTransitionSmokeTest.Active";
        private const string StartedKey = "Jiangshi.SceneTransitionSmokeTest.Started";
        private const string FailureCountKey = "Jiangshi.SceneTransitionSmokeTest.FailureCount";
        private const string PhaseKey = "Jiangshi.SceneTransitionSmokeTest.Phase";

        private static readonly Stopwatch Stopwatch = new();
        private static int failureCount;
        private static bool playStarted;
        private static int phase;
        private static int frameDelay;

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (SessionState.GetInt(ActiveKey, 0) == 0)
            {
                return;
            }

            failureCount = SessionState.GetInt(FailureCountKey, 0);
            playStarted = SessionState.GetInt(StartedKey, 0) != 0;
            phase = SessionState.GetInt(PhaseKey, 0);
            Stopwatch.Restart();
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        public static void Run()
        {
            failureCount = 0;
            playStarted = false;
            phase = 0;
            frameDelay = 0;
            Stopwatch.Restart();
            SessionState.SetInt(ActiveKey, 1);
            SessionState.SetInt(StartedKey, 0);
            SessionState.SetInt(FailureCountKey, 0);
            SessionState.SetInt(PhaseKey, 0);

            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update += OnEditorUpdate;

            var scene = EditorSceneManager.OpenScene(PrototypeScenePath);
            if (!scene.IsValid())
            {
                UnityEngine.Debug.LogError($"Failed to open scene: {PrototypeScenePath}");
                Exit(1);
                return;
            }

            UnityEngine.Debug.Log($"SceneTransitionSmokeTest starting scene: {PrototypeScenePath}");
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
                    UnityEngine.Debug.Log("SceneTransitionSmokeTest entered play mode.");
                }
                else if (Stopwatch.Elapsed.TotalSeconds > MaxPhaseSeconds)
                {
                    UnityEngine.Debug.LogError("Timed out waiting for play mode to start.");
                    Exit(1);
                }

                return;
            }

            if (Stopwatch.Elapsed.TotalSeconds > MaxPhaseSeconds)
            {
                UnityEngine.Debug.LogError($"SceneTransitionSmokeTest timed out in phase {phase}.");
                Exit(1);
                return;
            }

            switch (phase)
            {
                case 0:
                    BeginCorridorLoad();
                    break;
                case 1:
                    WaitForCorridorScene();
                    break;
                case 2:
                    VerifyCorridorIsolationAndReturn();
                    break;
                case 3:
                    VerifyReturnedPrototype();
                    break;
            }
        }

        private static void BeginCorridorLoad()
        {
            UnityEngine.Debug.Log("SceneTransitionSmokeTest loading corridor additively.");
            Time.timeScale = 0f;
            SceneManager.LoadScene(CorridorSceneName, LoadSceneMode.Additive);
            SetPhase(1);
        }

        private static void WaitForCorridorScene()
        {
            var scene = SceneManager.GetSceneByName(CorridorSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (frameDelay < 3)
            {
                frameDelay++;
                return;
            }

            SetPhase(2);
        }

        private static void VerifyCorridorIsolationAndReturn()
        {
            CheckNoEnabledPrototypeComponents<Camera>("Camera");
            CheckNoEnabledPrototypeComponents<EventSystem>("EventSystem");
            CheckNoEnabledPrototypeComponents<Canvas>("Canvas");
            CheckNoEnabledPrototypeComponents<PrototypeHud>("PrototypeHud");
            CheckNoEnabledPrototypeComponents<TrainingPanel>("TrainingPanel");
            CheckNoEnabledPrototypeComponents<UnitHealthBar>("UnitHealthBar");
            CheckNoEnabledPrototypeComponents<DebugController>("DebugController");
            CheckNoEnabledPrototypeComponents<UnitCommandController>("UnitCommandController");

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            MissionResultState.Result = MissionResult.SerumAcquired;
            var beforeReturnBuildingCount = CountPrototypeBuildings();
            SessionState.SetInt("Jiangshi.SceneTransitionSmokeTest.BuildingCount", beforeReturnBuildingCount);

            UnityEngine.Debug.Log("SceneTransitionSmokeTest returning to Prototype.");
            CorridorSceneBridge.ReturnToDefenseScene(MissionResult.SerumAcquired);
            SetPhase(3);
        }

        private static void VerifyReturnedPrototype()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != PrototypeSceneName)
            {
                return;
            }

            if (frameDelay < 3)
            {
                frameDelay++;
                return;
            }

            if (Cursor.lockState != CursorLockMode.None)
            {
                UnityEngine.Debug.LogError($"Cursor lock state was not reset after returning to Prototype: {Cursor.lockState}");
            }

            if (!Cursor.visible)
            {
                UnityEngine.Debug.LogError("Cursor was not visible after returning to Prototype.");
            }

            var expectedBuildingCount = SessionState.GetInt("Jiangshi.SceneTransitionSmokeTest.BuildingCount", -1);
            var actualBuildingCount = CountPrototypeBuildings();
            if (expectedBuildingCount >= 0 && actualBuildingCount != expectedBuildingCount)
            {
                UnityEngine.Debug.LogError($"Prototype building state was not preserved after corridor return. Before={expectedBuildingCount}, after={actualBuildingCount}");
            }

            UnityEngine.Debug.Log($"SceneTransitionSmokeTest completed. Failures: {failureCount}");
            Exit(failureCount == 0 ? 0 : 1);
        }

        private static void CheckNoEnabledPrototypeComponents<T>(string label) where T : Behaviour
        {
            foreach (var component in Object.FindObjectsOfType<T>())
            {
                if (component.gameObject.scene.name == PrototypeSceneName && component.enabled)
                {
                    UnityEngine.Debug.LogError($"{label} from Prototype remained enabled in corridor scene: {component.name}");
                }
            }
        }

        private static int CountPrototypeBuildings()
        {
            var count = 0;
            foreach (var building in Resources.FindObjectsOfTypeAll<Jiangshi.Building.Building>())
            {
                if (building != null && building.gameObject.scene.name == PrototypeSceneName)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetPhase(int nextPhase)
        {
            phase = nextPhase;
            frameDelay = 0;
            Stopwatch.Restart();
            SessionState.SetInt(PhaseKey, phase);
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
            SessionState.EraseInt(PhaseKey);
            SessionState.EraseInt("Jiangshi.SceneTransitionSmokeTest.BuildingCount");
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
