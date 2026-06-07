using System.Diagnostics;
using System.Reflection;
using Jiangshi.Combat;
using Jiangshi.UI;
using Jiangshi.Units;
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

            CheckRuntimeObjectExists("Settings Volume Slider");
            CheckRuntimeObjectExists("Settings Volume Value");
            CheckRuntimeObjectExists("Settings Window Mode Button");
            CheckRuntimeObjectExists("Game Guide Scroll View");
            CheckRuntimeObjectExists("Guide Scrollbar");
            CheckRtsCameraClamp();
            CheckResourceAudioClip("Audio/Prototype/DefenseTheme");
            CheckResourceAudioClip("Audio/Prototype/HordeTheme");
            CheckResourceAudioClip("Audio/Prototype/DefeatTheme");
            CheckResourceAudioClip("Audio/Prototype/SerumVictoryTheme");
            CheckZombieAssignedTargetReach();
            UnityEngine.Debug.Log($"PlayModeSmokeTest completed. Failures: {failureCount}");
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

        private static void CheckRtsCameraClamp()
        {
            var controller = Object.FindObjectOfType<RtsCameraController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError("RtsCameraController is missing.");
                return;
            }

            var clampMethod = typeof(RtsCameraController).GetMethod("ClampPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            if (clampMethod == null)
            {
                UnityEngine.Debug.LogError("RtsCameraController clamp method is missing.");
                return;
            }

            var originalPosition = controller.transform.position;
            controller.transform.position = new Vector3(500f, originalPosition.y, 500f);
            clampMethod.Invoke(controller, null);
            var clampedPosition = controller.transform.position;
            controller.transform.position = originalPosition;

            if (clampedPosition.x > 160f || clampedPosition.z > 170f)
            {
                UnityEngine.Debug.LogError($"RtsCameraController did not clamp near the map: {clampedPosition}.");
                return;
            }

            UnityEngine.Debug.Log($"RtsCameraController clamped camera near map: {clampedPosition}.");
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

        private static void CheckZombieAssignedTargetReach()
        {
            var baseObject = new GameObject("Smoke Test Command Base");
            var zombieObject = new GameObject("Smoke Test Zombie");

            try
            {
                baseObject.transform.position = Vector3.zero;
                var baseCollider = baseObject.AddComponent<BoxCollider>();
                baseCollider.size = new Vector3(2f, 2f, 2f);
                var baseFaction = baseObject.AddComponent<FactionMember>();
                baseFaction.SetFaction(Faction.Player);
                var baseDamageable = baseObject.AddComponent<Damageable>();

                zombieObject.transform.position = new Vector3(2.2f, 0f, 1.2f);
                var zombie = zombieObject.AddComponent<Zombie>();

                SetPrivateField(zombie, "target", baseObject.transform);
                SetPrivateField(zombie, "overlapHits", new Collider[8]);

                var findAttackTarget = typeof(Zombie).GetMethod("FindAttackTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                if (findAttackTarget == null)
                {
                    UnityEngine.Debug.LogError("Zombie FindAttackTarget method is missing.");
                    return;
                }

                var result = findAttackTarget.Invoke(zombie, null) as Damageable;
                if (result != baseDamageable)
                {
                    UnityEngine.Debug.LogError("Zombie did not use assigned target collider reach fallback.");
                    return;
                }

                UnityEngine.Debug.Log("Zombie assigned target collider reach fallback works.");
            }
            finally
            {
                Object.Destroy(zombieObject);
                Object.Destroy(baseObject);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                UnityEngine.Debug.LogError($"{target.GetType().Name} field is missing: {fieldName}");
                return;
            }

            field.SetValue(target, value);
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
