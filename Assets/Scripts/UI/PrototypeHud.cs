using System.Text;
using Jiangshi.Building;
using Jiangshi.Combat;
using Jiangshi.Core;
using Jiangshi.Economy;
using Jiangshi.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace Jiangshi.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private SurvivalTimer survivalTimer;
        [SerializeField] private PlacementSystem placementSystem;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private Damageable commandBase;
        [SerializeField] private Text gameStateText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text woodText;
        [SerializeField] private Text foodText;
        [SerializeField] private Text powerText;
        [SerializeField] private Text baseHealthText;
        [SerializeField] private Text survivalText;
        [SerializeField] private Text waveStatusText;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Text pauseButtonLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button victoryRestartButton;
        [SerializeField] private Button[] buildButtons;
        [SerializeField] private Text[] buildButtonLabels;
        [SerializeField] private BuildingData[] buildButtonData;
        [SerializeField] private Color buildButtonNormalColor = new Color(0.13f, 0.2f, 0.24f, 0.96f);
        [SerializeField] private Color buildButtonSelectedColor = new Color(0.16f, 0.46f, 0.31f, 0.98f);
        [SerializeField] private Color buildButtonDisabledColor = new Color(0.12f, 0.13f, 0.14f, 0.72f);
        [SerializeField] private bool enableDebugDefeatKey = true;
        [SerializeField] private KeyCode debugDefeatKey = KeyCode.F9;
        [SerializeField] private bool enableDebugVictoryKey = true;
        [SerializeField] private KeyCode debugVictoryKey = KeyCode.F10;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (commandBase == null)
            {
                commandBase = FindCriticalBuildingHealth();
            }
        }

        private void Start()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (resourceManager != null)
            {
                resourceManager.ResourceChanged += OnResourceChanged;
            }

            if (placementSystem != null)
            {
                placementSystem.SelectedBuildingChanged += OnSelectedBuildingChanged;
            }

            if (commandBase != null)
            {
                commandBase.HealthChanged += OnBaseHealthChanged;
            }

            if (gameManager != null)
            {
                gameManager.StateChanged += OnGameStateChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(TogglePause);
            }

            if (victoryRestartButton != null)
            {
                victoryRestartButton.onClick.AddListener(Restart);
            }

            RegisterBuildButtons();
            RefreshAll();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }

            if (enableDebugDefeatKey && Input.GetKeyDown(debugDefeatKey))
            {
                gameManager?.Lose();
            }

            if (enableDebugVictoryKey && Input.GetKeyDown(debugVictoryKey))
            {
                gameManager?.Win();
            }

            RefreshBaseHealth();
            RefreshSurvivalTime();
            RefreshWaveStatus();
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
            {
                resourceManager.ResourceChanged -= OnResourceChanged;
            }

            if (placementSystem != null)
            {
                placementSystem.SelectedBuildingChanged -= OnSelectedBuildingChanged;
            }

            UnregisterBuildButtons();

            if (commandBase != null)
            {
                commandBase.HealthChanged -= OnBaseHealthChanged;
            }

            if (gameManager != null)
            {
                gameManager.StateChanged -= OnGameStateChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(TogglePause);
            }

            if (victoryRestartButton != null)
            {
                victoryRestartButton.onClick.RemoveListener(Restart);
            }
        }

        private void Restart()
        {
            gameManager?.RestartLevel();
        }

        private void TogglePause()
        {
            gameManager?.TogglePause();
        }

        private void OnResourceChanged(ResourceType resourceType, int value)
        {
            RefreshResources();
            RefreshBuildButtons();
        }

        private void OnBaseHealthChanged(Damageable damageable, int value)
        {
            RefreshBaseHealth();
        }

        private void OnGameStateChanged(GameState state)
        {
            RefreshGameState();
        }

        private void RefreshAll()
        {
            RefreshResources();
            RefreshBaseHealth();
            RefreshSurvivalTime();
            RefreshWaveStatus();
            RefreshBuildButtons();
            RefreshGameState();
        }

        private void RefreshResources()
        {
            SetText(goldText, $"金币: {GetResource(ResourceType.Gold)}");
            SetText(woodText, $"木材: {GetResource(ResourceType.Wood)}");
            SetText(foodText, $"食物: {GetResource(ResourceType.Food)}");
            SetText(powerText, $"电力: {GetResource(ResourceType.Power)}");
        }

        private int GetResource(ResourceType resourceType)
        {
            return resourceManager != null ? resourceManager.Get(resourceType) : 0;
        }

        private void RefreshBaseHealth()
        {
            if (commandBase == null)
            {
                SetText(baseHealthText, "基地: 已摧毁");
                return;
            }

            SetText(baseHealthText, $"基地: {commandBase.CurrentHealth}/{commandBase.MaxHealth}");
        }

        private void RefreshWaveStatus()
        {
            SetText(waveStatusText, waveManager != null ? waveManager.StatusText : "No waves");
        }

        private void RefreshSurvivalTime()
        {
            if (survivalTimer == null)
            {
                SetText(survivalText, "存活: --:--");
                return;
            }

            var remaining = Mathf.CeilToInt(survivalTimer.RemainingSeconds);
            var minutes = remaining / 60;
            var seconds = remaining % 60;
            SetText(survivalText, $"存活: {minutes:00}:{seconds:00}");
        }

        private void RefreshGameState()
        {
            var state = gameManager != null ? gameManager.State : GameState.Boot;
            var stateLabel = state switch
            {
                GameState.Playing => "进行中",
                GameState.Paused => "已暂停",
                GameState.Defeat => "失败",
                GameState.Victory => "胜利",
                _ => "启动中"
            };
            SetText(gameStateText, $"状态: {stateLabel}");
            SetText(pauseButtonLabel, state == GameState.Paused ? "继续" : "暂停");

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(state == GameState.Defeat);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(state == GameState.Victory);
            }
        }

        private void RegisterBuildButtons()
        {
            if (buildButtons == null)
            {
                return;
            }

            for (var i = 0; i < buildButtons.Length; i++)
            {
                var button = buildButtons[i];
                if (button == null)
                {
                    continue;
                }

                var index = i;
                button.onClick.AddListener(() => SelectBuildButton(index));
            }
        }

        private void UnregisterBuildButtons()
        {
            if (buildButtons == null)
            {
                return;
            }

            foreach (var button in buildButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        private void SelectBuildButton(int index)
        {
            if (placementSystem == null || buildButtonData == null || index < 0 || index >= buildButtonData.Length)
            {
                return;
            }

            placementSystem.SelectBuilding(buildButtonData[index]);
        }

        private void OnSelectedBuildingChanged(BuildingData selectedBuilding)
        {
            RefreshBuildButtons();
        }

        private void RefreshBuildButtons()
        {
            if (buildButtons == null || buildButtonData == null)
            {
                return;
            }

            for (var i = 0; i < buildButtons.Length; i++)
            {
                var button = buildButtons[i];
                var data = i < buildButtonData.Length ? buildButtonData[i] : null;
                var label = buildButtonLabels != null && i < buildButtonLabels.Length ? buildButtonLabels[i] : null;

                if (button == null || data == null)
                {
                    if (button != null)
                    {
                        button.interactable = false;
                    }

                    SetText(label, string.Empty);
                    continue;
                }

                var canAfford = resourceManager == null || resourceManager.CanAfford(data.buildCost);
                var isSelected = placementSystem != null && placementSystem.SelectedBuilding == data;
                button.interactable = canAfford;
                SetButtonColor(button, !canAfford ? buildButtonDisabledColor : isSelected ? buildButtonSelectedColor : buildButtonNormalColor);
                SetText(label, FormatBuildButtonLabel(i, data));
            }
        }

        private string FormatBuildButtonLabel(int index, BuildingData data)
        {
            var builder = new StringBuilder();
            builder.Append(index + 1);
            builder.Append(". ");
            builder.Append(string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName);

            if (data.buildCost != null && data.buildCost.Length > 0)
            {
                builder.Append('\n');
                for (var i = 0; i < data.buildCost.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("  ");
                    }

                    builder.Append(FormatCost(data.buildCost[i]));
                }

                if (data.powerCost > 0)
                {
                    builder.Append($"  ⚡{data.powerCost}");
                }
            }

            return builder.ToString();
        }

        private string FormatCost(ResourceAmount cost)
        {
            return cost.type switch
            {
                ResourceType.Gold => $"{cost.amount}金",
                ResourceType.Wood => $"{cost.amount}木",
                ResourceType.Food => $"{cost.amount}食",
                ResourceType.Power => $"{cost.amount}电",
                ResourceType.Population => $"{cost.amount}人",
                ResourceType.Iron => $"{cost.amount}铁",
                ResourceType.Copper => $"{cost.amount}铜",
                _ => $"{cost.amount} {cost.type}"
            };
        }

        private void SetButtonColor(Button button, Color color)
        {
            var graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = color;
            }
        }

        private Damageable FindCriticalBuildingHealth()
        {
            foreach (var building in FindObjectsOfType<Jiangshi.Building.Building>())
            {
                if (building != null && building.Data != null && building.Data.triggersDefeatOnDestroyed)
                {
                    return building.GetComponent<Damageable>();
                }
            }

            return null;
        }

        private void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
