using System;
using System.Collections;
using Jiangshi.Combat;
using Jiangshi.Grid;
using Jiangshi.UI;
using Jiangshi.Units;
using Jiangshi.Waves;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Jiangshi.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState initialState = GameState.Playing;
        [SerializeField] private GameObject serumPlacementEffect;
        [SerializeField] private int operatorLostZombieCount = 200;
        [SerializeField] private int operatorLostSpawnDirections = 4;

        public GameState State { get; private set; }
        public event Action<GameState> StateChanged;

        private Camera cachedMainCamera;
        private bool serumPlacementMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            State = GameState.Boot;
        }

        private void Start()
        {
            cachedMainCamera = Camera.main;
            ApplyCorridorResult();
            SetState(initialState);
        }

        private void Update()
        {
            if (serumPlacementMode && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                TryPlaceSerum();
            }
        }

        private void ApplyCorridorResult()
        {
            var result = MissionResultState.Result;
            Debug.Log($"[GameManager] ApplyCorridorResult: result={result}");
            MissionResultState.Result = MissionResult.None;

            switch (result)
            {
                case MissionResult.SerumAcquired:
                    StartCoroutine(EnterSerumPlacementMode());
                    break;
                case MissionResult.OperatorLost:
                    TriggerOperatorLost();
                    break;
            }
        }

        private IEnumerator EnterSerumPlacementMode()
        {
            Debug.Log("[GameManager] EnterSerumPlacementMode 开始");
            yield return null;
            serumPlacementMode = true;

            var hud = FindObjectOfType<PrototypeHud>();
            if (hud != null)
                hud.ShowPortalAnnouncement("点击地图任意位置投放血清原液");
        }

        private void TryPlaceSerum()
        {
            if (cachedMainCamera == null) return;

            var ray = cachedMainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 1000f)) return;

            var gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                var gp = gridManager.WorldToGrid(hit.point);
                var cell = gridManager.GetCell(gp);
                if (cell == null || !cell.IsWalkable || cell.IsOccupied) return;
            }

            serumPlacementMode = false;

            if (serumPlacementEffect != null)
                Instantiate(serumPlacementEffect, hit.point, Quaternion.identity);

            StartCoroutine(KillAllZombiesAndWin());
        }

        private IEnumerator KillAllZombiesAndWin()
        {
            yield return null;

            foreach (var zombie in FindObjectsOfType<Zombie>())
            {
                if (zombie == null) continue;
                var damageable = zombie.GetComponent<Damageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    damageable.TakeDamage(9999);
                }
                else
                {
                    Destroy(zombie.gameObject);
                }
            }

            yield return null;
            Win();
        }

        private void TriggerOperatorLost()
        {
            var waveManager = FindObjectOfType<WaveManager>();
            var unitManager = FindObjectOfType<UnitManager>();
            var gridManager = FindObjectOfType<GridManager>();

            if (waveManager == null || unitManager == null || gridManager == null) return;

            var unitData = GetZombieUnitData(waveManager);
            if (unitData == null) return;

            var directions = Mathf.Max(1, operatorLostSpawnDirections);
            var perDirection = Mathf.CeilToInt((float)operatorLostZombieCount / directions);

            for (var d = 0; d < directions; d++)
            {
                var angle = (float)d / directions * 360f;
                var rad = angle * Mathf.Deg2Rad;
                var edgeX = Mathf.RoundToInt(gridManager.Width / 2f + Mathf.Cos(rad) * gridManager.Width / 2f);
                var edgeY = Mathf.RoundToInt(gridManager.Height / 2f + Mathf.Sin(rad) * gridManager.Height / 2f);
                edgeX = Mathf.Clamp(edgeX, 1, gridManager.Width - 2);
                edgeY = Mathf.Clamp(edgeY, 1, gridManager.Height - 2);

                var center = gridManager.GridToWorld(new GridPosition(edgeX, edgeY));

                for (var i = 0; i < perDirection; i++)
                {
                    var offset = UnityEngine.Random.insideUnitCircle * 3f;
                    var pos = center + new Vector3(offset.x, 0f, offset.y);
                    var unit = unitManager.Spawn(unitData, pos, Quaternion.identity);
                    if (unit is Zombie zombie)
                    {
                        zombie.SetAggressive();
                    }
                }
            }

            var hud = FindObjectOfType<PrototypeHud>();
            if (hud != null)
                hud.ShowPortalAnnouncement("操作者失联，异常生物质爆发！");
        }

        private static UnitData GetZombieUnitData(WaveManager waveManager)
        {
            var wavesField = waveManager.GetType().GetField("waves",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wavesField == null) return null;

            var waves = wavesField.GetValue(waveManager) as WaveData[];
            if (waves == null) return null;

            foreach (var wave in waves)
            {
                if (wave == null || wave.enemy == null) continue;
                if (wave.enemy.prefab != null && wave.enemy.prefab.GetComponent<Zombie>() != null)
                    return wave.enemy;
            }

            return null;
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        public void SetState(GameState nextState)
        {
            if (State == nextState) return;

            State = nextState;
            StateChanged?.Invoke(State);
        }

        public void Win()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetState(GameState.Victory);
        }

        public void Lose()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetState(GameState.Defeat);
        }

        public void TogglePause()
        {
            SetPaused(State != GameState.Paused);
        }

        public void SetPaused(bool paused)
        {
            if (State == GameState.Defeat || State == GameState.Victory) return;

            Time.timeScale = paused ? 0f : 1f;
            SetState(paused ? GameState.Paused : GameState.Playing);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else if (!string.IsNullOrEmpty(activeScene.name))
                SceneManager.LoadScene(activeScene.name);
        }
    }
}
