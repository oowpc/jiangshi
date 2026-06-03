using System.Text;
using Jiangshi.Building;
using Jiangshi.Combat;
using Jiangshi.Core;
using Jiangshi.Economy;
using Jiangshi.Waves;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [SerializeField] private Color buildButtonNormalColor = new Color(0.10f, 0.16f, 0.19f, 0.96f);
        [SerializeField] private Color buildButtonSelectedColor = new Color(0.16f, 0.42f, 0.31f, 0.98f);
        [SerializeField] private Color buildButtonDisabledColor = new Color(0.08f, 0.09f, 0.10f, 0.72f);
        [SerializeField] private Color buildButtonNormalOutlineColor = new Color(0.28f, 0.38f, 0.40f, 0.75f);
        [SerializeField] private Color buildButtonSelectedOutlineColor = new Color(0.95f, 0.74f, 0.33f, 1f);
        [SerializeField] private Color buildButtonDisabledOutlineColor = new Color(0.13f, 0.15f, 0.16f, 0.55f);
        [SerializeField] private Color buildButtonTextColor = new Color(0.92f, 0.96f, 0.93f, 1f);
        [SerializeField] private Color buildButtonDisabledTextColor = new Color(0.48f, 0.52f, 0.52f, 0.92f);
        [SerializeField] private bool enableDebugDefeatKey = true;
        [SerializeField] private KeyCode debugDefeatKey = KeyCode.F9;
        [SerializeField] private bool enableDebugVictoryKey = true;
        [SerializeField] private KeyCode debugVictoryKey = KeyCode.F10;

        private Button settingsButton;
        private Button settingsCloseButton;
        private Button settingsQuitButton;
        private GameObject settingsPanel;
        private bool settingsPausedGame;
        private GameObject demolitionPanel;
        private Text demolitionTitleText;
        private Text demolitionRefundText;
        private Button demolitionButton;
        private Text demolitionButtonLabel;
        private Jiangshi.Building.Building selectedDemolitionBuilding;

        private void Awake()
        {
            EnsureCanvasVisible();

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (commandBase == null)
            {
                commandBase = FindCriticalBuildingHealth();
            }
        }

        private void EnsureCanvasVisible()
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform != null && rectTransform.localScale == Vector3.zero)
            {
                rectTransform.localScale = Vector3.one;
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

            EnsureSettingsUi();
            EnsureDemolitionUi();
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
            HandleBuildingSelectionInput();
            RefreshDemolitionUi();
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

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(ToggleSettings);
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.RemoveListener(CloseSettings);
            }

            if (settingsQuitButton != null)
            {
                settingsQuitButton.onClick.RemoveListener(QuitGame);
            }

            if (demolitionButton != null)
            {
                demolitionButton.onClick.RemoveListener(DemolishSelectedBuilding);
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

        private void ToggleSettings()
        {
            if (settingsPanel == null)
            {
                return;
            }

            if (settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }

        private void OpenSettings()
        {
            if (settingsPanel == null)
            {
                return;
            }

            settingsPausedGame = gameManager != null && gameManager.State == GameState.Playing;
            if (settingsPausedGame)
            {
                gameManager.SetPaused(true);
            }

            settingsPanel.SetActive(true);
        }

        private void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (settingsPausedGame && gameManager != null && gameManager.State == GameState.Paused)
            {
                gameManager.SetPaused(false);
            }

            settingsPausedGame = false;
        }

        private void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleBuildingSelectionInput()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                selectedDemolitionBuilding = null;
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 500f, -1, QueryTriggerInteraction.Ignore))
            {
                if (placementSystem == null || placementSystem.SelectedBuilding == null)
                {
                    selectedDemolitionBuilding = null;
                }
                return;
            }

            var building = hit.collider.GetComponentInParent<Jiangshi.Building.Building>();
            if (building != null)
            {
                selectedDemolitionBuilding = building;
                placementSystem?.SelectBuilding(null);
                return;
            }

            if (placementSystem == null || placementSystem.SelectedBuilding == null)
            {
                selectedDemolitionBuilding = null;
            }
        }

        private void DemolishSelectedBuilding()
        {
            if (selectedDemolitionBuilding == null || !selectedDemolitionBuilding.CanDemolish())
            {
                return;
            }

            var building = selectedDemolitionBuilding;
            selectedDemolitionBuilding = null;
            building.Demolish(resourceManager);
            RefreshBuildButtons();
            RefreshDemolitionUi();
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
            SetText(goldText, $"GOLD  {GetResource(ResourceType.Gold)}");
            SetText(woodText, $"WOOD  {GetResource(ResourceType.Wood)}");
            SetText(foodText, $"FOOD  {GetResource(ResourceType.Food)}");
            SetText(powerText, $"PWR {GetResource(ResourceType.Power)}   POP {GetResource(ResourceType.Population)}");
            return;

            SetText(goldText, $"金币: {GetResource(ResourceType.Gold)}");
            SetText(woodText, $"木材: {GetResource(ResourceType.Wood)}");
            SetText(foodText, $"食物: {GetResource(ResourceType.Food)}");
            SetText(powerText, $"电力: {GetResource(ResourceType.Power)}  人口: {GetResource(ResourceType.Population)}");
        }

        private int GetResource(ResourceType resourceType)
        {
            return resourceManager != null ? resourceManager.Get(resourceType) : 0;
        }

        private void RefreshBaseHealth()
        {
            if (commandBase == null)
            {
                SetText(baseHealthText, "BASE  DESTROYED");
                return;
            }

            SetText(baseHealthText, $"BASE  {commandBase.CurrentHealth}/{commandBase.MaxHealth}");
            return;

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
                SetText(survivalText, "SURVIVE  --:--");
                return;
            }

            var hudRemaining = Mathf.CeilToInt(survivalTimer.RemainingSeconds);
            SetText(survivalText, $"SURVIVE  {hudRemaining / 60:00}:{hudRemaining % 60:00}");
            return;

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
            var hudState = gameManager != null ? gameManager.State : GameState.Boot;
            var hudStateLabel = hudState switch
            {
                GameState.Playing => "PLAYING",
                GameState.Paused => "PAUSED",
                GameState.Defeat => "DEFEAT",
                GameState.Victory => "VICTORY",
                _ => "BOOTING"
            };
            SetText(gameStateText, $"STATE  {hudStateLabel}");
            SetText(pauseButtonLabel, hudState == GameState.Paused ? "RESUME" : "PAUSE");

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(hudState == GameState.Defeat);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(hudState == GameState.Victory);
            }

            return;

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

                var canAfford = CanAffordBuildData(data);
                var isSelected = placementSystem != null && placementSystem.SelectedBuilding == data;
                button.interactable = canAfford;
                SetBuildButtonVisual(button, label, canAfford, isSelected);
                SetText(label, FormatBuildButtonLabel(i, data));
            }
        }

        private bool CanAffordBuildData(BuildingData data)
        {
            if (resourceManager == null)
            {
                return true;
            }

            return resourceManager.CanAfford(data.buildCost)
                && (data.powerCost <= 0 || resourceManager.Get(ResourceType.Power) >= data.powerCost)
                && (data.populationCost <= 0 || resourceManager.Get(ResourceType.Population) >= data.populationCost);
        }

        private string FormatBuildButtonLabel(int index, BuildingData data)
        {
            var labelBuilder = new StringBuilder();
            labelBuilder.Append('[');
            labelBuilder.Append(index == 9 ? 0 : index + 1);
            labelBuilder.Append("] ");
            labelBuilder.Append(string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName);

            var costText = FormatBuildCosts(data);
            if (!string.IsNullOrEmpty(costText))
            {
                labelBuilder.Append('\n');
                labelBuilder.Append(costText);
            }

            return labelBuilder.ToString();

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

                if (data.populationCost > 0)
                {
                    builder.Append($"  人{data.populationCost}");
                }
            }

            return builder.ToString();
        }

        private string FormatBuildCosts(BuildingData data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            var costBuilder = new StringBuilder();
            if (data.buildCost != null)
            {
                for (var i = 0; i < data.buildCost.Length; i++)
                {
                    if (i > 0)
                    {
                        costBuilder.Append("  ");
                    }

                    costBuilder.Append(FormatCost(data.buildCost[i]));
                }
            }

            if (data.powerCost > 0)
            {
                if (costBuilder.Length > 0)
                {
                    costBuilder.Append("  ");
                }

                costBuilder.Append($"P{data.powerCost}");
            }

            if (data.populationCost > 0)
            {
                if (costBuilder.Length > 0)
                {
                    costBuilder.Append("  ");
                }

                costBuilder.Append($"POP{data.populationCost}");
            }

            return costBuilder.ToString();
        }

        private string FormatCost(ResourceAmount cost)
        {
            return cost.type switch
            {
                ResourceType.Gold => $"G{cost.amount}",
                ResourceType.Wood => $"W{cost.amount}",
                ResourceType.Food => $"F{cost.amount}",
                ResourceType.Power => $"P{cost.amount}",
                ResourceType.Population => $"POP{cost.amount}",
                ResourceType.Iron => $"FE{cost.amount}",
                ResourceType.Copper => $"CU{cost.amount}",
                _ => $"{cost.amount} {cost.type}"
            };

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

        private void SetBuildButtonVisual(Button button, Text label, bool canAfford, bool isSelected)
        {
            SetButtonColor(button, !canAfford ? buildButtonDisabledColor : isSelected ? buildButtonSelectedColor : buildButtonNormalColor);
            SetButtonOutline(button, !canAfford ? buildButtonDisabledOutlineColor : isSelected ? buildButtonSelectedOutlineColor : buildButtonNormalOutlineColor, isSelected ? 3f : 1f);

            if (label != null)
            {
                label.color = canAfford ? buildButtonTextColor : buildButtonDisabledTextColor;
            }
        }

        private void SetButtonColor(Button button, Color color)
        {
            var graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = color;
            }
        }

        private void SetButtonOutline(Button button, Color color, float thickness)
        {
            var outline = button.GetComponent<Outline>();
            if (outline == null)
            {
                outline = button.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);
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

        private void EnsureDemolitionUi()
        {
            if (demolitionPanel != null)
            {
                return;
            }

            demolitionPanel = CreateRuntimePanel(transform, "Demolition Panel", new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(360f, 132f), new Color(0.035f, 0.047f, 0.052f, 0.92f));
            demolitionTitleText = CreateRuntimeText(demolitionPanel.transform, "Demolition Title", "未选择建筑", 20, new Vector2(0f, 38f), new Vector2(320f, 30f), TextAnchor.MiddleCenter);
            demolitionRefundText = CreateRuntimeText(demolitionPanel.transform, "Demolition Refund", "", 16, new Vector2(0f, 10f), new Vector2(320f, 28f), TextAnchor.MiddleCenter);
            demolitionButton = CreateRuntimeButton(demolitionPanel.transform, "Demolition Button", "拆除", new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(132f, 38f));
            demolitionButtonLabel = demolitionButton.GetComponentInChildren<Text>();
            demolitionTitleText.text = "NO BUILDING SELECTED";
            demolitionButtonLabel.text = "DEMOLISH";
            demolitionButton.onClick.AddListener(DemolishSelectedBuilding);
            demolitionPanel.SetActive(false);
        }

        private void RefreshDemolitionUi()
        {
            if (demolitionPanel == null)
            {
                return;
            }

            if (selectedDemolitionBuilding == null || selectedDemolitionBuilding.Data == null)
            {
                demolitionPanel.SetActive(false);
                return;
            }

            demolitionPanel.SetActive(true);
            var data = selectedDemolitionBuilding.Data;
            var name = string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName;
            SetText(demolitionTitleText, name);

            if (!selectedDemolitionBuilding.CanDemolish())
            {
                SetText(demolitionRefundText, "Core building cannot be demolished");
                SetText(demolitionButtonLabel, "LOCKED");
                demolitionButton.interactable = false;
                return;
            }

            SetText(demolitionRefundText, $"Refund: {FormatRefund(data)}");
            SetText(demolitionButtonLabel, "DEMOLISH");
            demolitionButton.interactable = true;
            return;

            if (!selectedDemolitionBuilding.CanDemolish())
            {
                SetText(demolitionRefundText, "Core building cannot be demolished");
                SetText(demolitionButtonLabel, "LOCKED");
                demolitionButton.interactable = false;
                return;

                SetText(demolitionRefundText, "核心建筑不能拆除");
                SetText(demolitionButtonLabel, "不可拆除");
                demolitionButton.interactable = false;
                return;
            }

            SetText(demolitionRefundText, $"返还: {FormatRefund(data)}");
            SetText(demolitionButtonLabel, "拆除");
            demolitionButton.interactable = true;
        }

        private string FormatRefund(BuildingData data)
        {
            if (data == null || data.buildCost == null || data.buildCost.Length == 0)
            {
                return "无";
            }

            var builder = new StringBuilder();
            foreach (var cost in data.buildCost)
            {
                var amount = Mathf.FloorToInt(cost.amount * 0.5f);
                if (amount <= 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(FormatCost(new ResourceAmount { type = cost.type, amount = amount }));
            }

            return builder.Length > 0 ? builder.ToString() : "无";
        }

        private void EnsureSettingsUi()
        {
            if (settingsButton != null)
            {
                return;
            }

            var root = transform;
            settingsButton = CreateRuntimeButton(root, "Settings Button", "设置", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(104f, 38f));
            settingsButton.onClick.AddListener(ToggleSettings);
            SetupRuntimeRect(settingsButton.gameObject, new Vector2(1f, 1f), new Vector2(-76f, -24f), new Vector2(116f, 38f));
            settingsButton.GetComponentInChildren<Text>().text = "MENU";

            settingsPanel = CreateRuntimePanel(root, "Settings Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 210f), new Color(0.035f, 0.045f, 0.052f, 0.94f));
            CreateRuntimeText(settingsPanel.transform, "Settings Title", "设置", 30, new Vector2(0f, 62f), new Vector2(300f, 44f), TextAnchor.MiddleCenter);
            CreateRuntimeText(settingsPanel.transform, "Settings Message", "游戏已暂停", 20, new Vector2(0f, 20f), new Vector2(300f, 32f), TextAnchor.MiddleCenter);

            settingsCloseButton = CreateRuntimeButton(settingsPanel.transform, "Settings Close Button", "继续游戏", new Vector2(0.5f, 0.5f), new Vector2(-82f, -52f), new Vector2(132f, 44f));
            settingsQuitButton = CreateRuntimeButton(settingsPanel.transform, "Settings Quit Button", "退出游戏", new Vector2(0.5f, 0.5f), new Vector2(82f, -52f), new Vector2(132f, 44f));
            settingsCloseButton.onClick.AddListener(CloseSettings);
            settingsQuitButton.onClick.AddListener(QuitGame);
            var settingsTexts = settingsPanel.GetComponentsInChildren<Text>();
            if (settingsTexts.Length > 0)
            {
                settingsTexts[0].text = "SETTINGS";
            }

            if (settingsTexts.Length > 1)
            {
                settingsTexts[1].text = "Game paused";
            }

            settingsCloseButton.GetComponentInChildren<Text>().text = "RESUME";
            settingsQuitButton.GetComponentInChildren<Text>().text = "QUIT";
            settingsPanel.SetActive(false);
        }

        private static GameObject CreateRuntimePanel(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            SetupRuntimeRect(panel, anchor, anchoredPosition, size);

            var image = panel.AddComponent<Image>();
            image.color = color;

            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.38f);
            shadow.effectDistance = new Vector2(0f, -5f);
            return panel;
        }

        private static Button CreateRuntimeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            SetupRuntimeRect(buttonObject, anchor, anchoredPosition, size);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.13f, 0.2f, 0.24f, 0.96f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.18f, 0.31f, 0.36f, 1f);
            colors.pressedColor = new Color(0.08f, 0.13f, 0.16f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.10f, 0.11f, 0.12f, 0.72f);
            button.colors = colors;

            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.38f, 0.40f, 0.75f);
            outline.effectDistance = new Vector2(1f, -1f);

            CreateRuntimeText(buttonObject.transform, "Label", label, 18, Vector2.zero, size, TextAnchor.MiddleCenter);
            return button;
        }

        private static Text CreateRuntimeText(Transform parent, string name, string value, int fontSize, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            SetupRuntimeRect(textObject, new Vector2(0.5f, 0.5f), anchoredPosition, size);

            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.94f, 0.96f, 0.93f, 1f);
            return text;
        }

        private static void SetupRuntimeRect(GameObject obj, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = obj.AddComponent<RectTransform>();
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
