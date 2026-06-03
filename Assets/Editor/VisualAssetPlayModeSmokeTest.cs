using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Jiangshi.Combat;
using Jiangshi.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jiangshi.Editor
{
    public static class VisualAssetPlayModeSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const double MaxStartupSeconds = 30.0;
        private const double RenderDelaySeconds = 2.0;
        private const string ActiveKey = "Jiangshi.VisualAssetPlayModeSmokeTest.Active";
        private const string StartedKey = "Jiangshi.VisualAssetPlayModeSmokeTest.Started";
        private const string FailureCountKey = "Jiangshi.VisualAssetPlayModeSmokeTest.FailureCount";

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

            UnityEngine.Debug.Log($"VisualAssetPlayModeSmokeTest starting scene: {ScenePath}");
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
                    UnityEngine.Debug.Log("VisualAssetPlayModeSmokeTest entered play mode.");
                }
                else if (Stopwatch.Elapsed.TotalSeconds > MaxStartupSeconds)
                {
                    UnityEngine.Debug.LogError("Timed out waiting for play mode to start.");
                    Exit(1);
                }

                return;
            }

            if (Stopwatch.Elapsed.TotalSeconds < RenderDelaySeconds)
            {
                return;
            }

            CheckVisualAsset("GoldMine", "Assets/Prefabs/GoldMine.prefab", new Vector3(-2.5f, 0f, 0f), 1500);
            CheckVisualAsset("Soldier", "Assets/Prefabs/Soldier.prefab", Vector3.zero, 400);
            CheckVisualAsset("Swordsman", "Assets/Prefabs/Swordsman.prefab", new Vector3(2.5f, 0f, 0f), 400);
            CheckVisualAsset("Projectile", "Assets/Prefabs/Projectile.prefab", new Vector3(5f, 0f, 0f), 20);
            CheckVisualAsset("Rotated Wall", "Assets/Prefabs/Wall.prefab", new Vector3(-5f, 0f, 0f), 400, Quaternion.Euler(0f, 90f, 0f));
            CheckSoldierProjectileBinding();
            CheckSoldierFiresProjectile();
            CheckRuntimeObjectExists("Settings Guide Button");
            CheckRuntimeObjectExists("Game Guide Panel");

            UnityEngine.Debug.Log($"VisualAssetPlayModeSmokeTest completed. Failures: {failureCount}");
            Exit(failureCount == 0 ? 0 : 1);
        }

        private static void CheckVisualAsset(string label, string prefabPath, Vector3 position, int minVisiblePixels, Quaternion? extraRotation = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                UnityEngine.Debug.LogError($"{label} prefab missing: {prefabPath}");
                return;
            }

            var rotation = extraRotation.HasValue
                ? extraRotation.Value * prefab.transform.rotation
                : prefab.transform.rotation;
            var instance = Object.Instantiate(prefab, position, rotation);
            instance.name = $"{label} Visual Smoke Instance";
            var spriteRenderer = instance.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                UnityEngine.Debug.LogError($"{label} has no visible sprite assigned.");
                Object.DestroyImmediate(instance);
                return;
            }

            spriteRenderer.sortingOrder = 100;
            var visiblePixels = CountRenderedPixels(instance.transform.position);
            if (visiblePixels < minVisiblePixels)
            {
                UnityEngine.Debug.LogError($"{label} rendered too few visible pixels: {visiblePixels} < {minVisiblePixels}.");
            }
            else
            {
                UnityEngine.Debug.Log($"{label} rendered visible pixels: {visiblePixels}.");
            }

            Object.DestroyImmediate(instance);
        }

        private static void CheckSoldierProjectileBinding()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Soldier.prefab");
            var soldier = prefab != null ? prefab.GetComponent<Jiangshi.Units.Soldier>() : null;
            if (soldier == null)
            {
                UnityEngine.Debug.LogError("Soldier prefab missing Soldier component.");
                return;
            }

            var serializedObject = new SerializedObject(soldier);
            var property = serializedObject.FindProperty("projectilePrefab");
            if (property == null || property.objectReferenceValue == null)
            {
                UnityEngine.Debug.LogError("Soldier projectilePrefab is not assigned.");
                return;
            }

            UnityEngine.Debug.Log($"Soldier projectilePrefab assigned: {property.objectReferenceValue.name}.");
        }

        private static void CheckSoldierFiresProjectile()
        {
            var soldierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Soldier.prefab");
            var soldierData = AssetDatabase.LoadAssetAtPath<UnitData>("Assets/ScriptableObjects/Units/SoldierData.asset");
            if (soldierPrefab == null || soldierData == null)
            {
                UnityEngine.Debug.LogError("Soldier fire test missing Soldier prefab or SoldierData.");
                return;
            }

            var beforeCount = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            var soldierObject = Object.Instantiate(soldierPrefab, Vector3.zero, soldierPrefab.transform.rotation);
            var soldier = soldierObject.GetComponent<Soldier>();
            soldier.Initialize(soldierData);

            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyObject.name = "Soldier Fire Test Enemy";
            enemyObject.transform.position = new Vector3(2f, 0f, 0f);
            enemyObject.AddComponent<Damageable>();
            enemyObject.AddComponent<FactionMember>().SetFaction(Faction.Enemy);

            typeof(Soldier)
                .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(soldier, null);

            var afterCount = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            if (afterCount <= beforeCount)
            {
                UnityEngine.Debug.LogError("Soldier did not spawn a projectile when firing.");
            }
            else
            {
                UnityEngine.Debug.Log($"Soldier spawned projectile count: {afterCount - beforeCount}.");
            }

            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(soldierObject);
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

        private static int CountRenderedPixels(Vector3 focus)
        {
            var cameraObject = new GameObject("Visual Smoke Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 3.0f;
            camera.transform.position = focus + new Vector3(0f, 8f, -8f);
            camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

            var renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            var visiblePixels = 0;
            foreach (var pixel in texture.GetPixels32())
            {
                if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8)
                {
                    visiblePixels++;
                }
            }

            RenderTexture.active = previousActive;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
            return visiblePixels;
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
