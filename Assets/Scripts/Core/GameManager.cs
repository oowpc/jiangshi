using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jiangshi.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState initialState = GameState.Playing;

        public GameState State { get; private set; }
        public event Action<GameState> StateChanged;

        public static float CorridorDifficultyMultiplier { get; private set; } = 1f;
        public static string CorridorResultMessage { get; private set; } = "";

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
            ApplyCorridorResult();
            SetState(initialState);
        }

        private void ApplyCorridorResult()
        {
            switch (MissionResultState.Result)
            {
                case MissionResult.SerumAcquired:
                    CorridorDifficultyMultiplier = 0.7f;
                    CorridorResultMessage = "血清原液已投放，异常生物质正在消退。";
                    break;
                case MissionResult.OperatorLost:
                    CorridorDifficultyMultiplier = 1.5f;
                    CorridorResultMessage = "操作者失联，未取得血清原液。";
                    break;
                default:
                    CorridorDifficultyMultiplier = 1f;
                    CorridorResultMessage = "";
                    break;
            }

            MissionResultState.Result = MissionResult.None;
        }

        public void SetState(GameState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            State = nextState;
            StateChanged?.Invoke(State);
        }

        public void Win()
        {
            Time.timeScale = 0f;
            SetState(GameState.Victory);
        }

        public void Lose()
        {
            Time.timeScale = 0f;
            SetState(GameState.Defeat);
        }

        public void TogglePause()
        {
            SetPaused(State != GameState.Paused);
        }

        public void SetPaused(bool paused)
        {
            if (State == GameState.Defeat || State == GameState.Victory)
            {
                return;
            }

            Time.timeScale = paused ? 0f : 1f;
            SetState(paused ? GameState.Paused : GameState.Playing);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            if (!string.IsNullOrEmpty(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }
    }
}
