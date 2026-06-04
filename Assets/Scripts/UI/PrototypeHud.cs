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
        private Button settingsGuideButton;
        private Button settingsQuitButton;
        private Button guideCloseButton;
        private GameObject settingsPanel;
        private GameObject guidePanel;
        private bool settingsPausedGame;
        private GameObject demolitionPanel;
        private Text demolitionTitleText;
        private Text demolitionRefundText;
        private Button demolitionButton;
        private Text demolitionButtonLabel;
        private Jiangshi.Building.Building selectedDemolitionBuilding;
        private GameObject buildTooltipPanel;
        private Text buildTooltipText;
        private int hoveredBuildIndex = -1;

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
            EnsureBuildTooltipUi();
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
            UpdateBuildTooltipPosition();
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

            if (settingsGuideButton != null)
            {
                settingsGuideButton.onClick.RemoveListener(OpenGuide);
            }

            if (settingsQuitButton != null)
            {
                settingsQuitButton.onClick.RemoveListener(QuitGame);
            }

            if (guideCloseButton != null)
            {
                guideCloseButton.onClick.RemoveListener(CloseGuide);
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

            CloseGuide();

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

        private void OpenGuide()
        {
            if (guidePanel != null)
            {
                guidePanel.SetActive(true);
            }
        }

        private void CloseGuide()
        {
            if (guidePanel != null)
            {
                guidePanel.SetActive(false);
            }
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
                SetText(baseHealthText, "基地: 已摧毁");
                return;
            }

            SetText(baseHealthText, $"基地: {commandBase.CurrentHealth}/{commandBase.MaxHealth}");
        }

        private void RefreshWaveStatus()
        {
            SetText(waveStatusText, waveManager != null ? waveManager.StatusText : "无波次");
        }

        private void RefreshSurvivalTime()
        {
            if (survivalTimer == null)
            {
                SetText(survivalText, "存活: --:--");
                return;
            }

            var hudRemaining = Mathf.CeilToInt(survivalTimer.RemainingSeconds);
            SetText(survivalText, $"存活: {hudRemaining / 60:00}:{hudRemaining % 60:00}");
        }

        private void RefreshGameState()
        {
            var hudState = gameManager != null ? gameManager.State : GameState.Boot;
            var hudStateLabel = hudState switch
            {
                GameState.Playing => "进行中",
                GameState.Paused => "已暂停",
                GameState.Defeat => "失败",
                GameState.Victory => "胜利",
                _ => "启动中"
            };
            SetText(gameStateText, $"状态: {hudStateLabel}");
            SetText(pauseButtonLabel, hudState == GameState.Paused ? "继续" : "暂停");

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(hudState == GameState.Defeat);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(hudState == GameState.Victory);
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
                var trigger = button.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<EventTrigger>();
                }

                AddBuildTooltipHandler(trigger, EventTriggerType.PointerEnter, () => ShowBuildTooltip(index));
                AddBuildTooltipHandler(trigger, EventTriggerType.PointerExit, () => HideBuildTooltip(index));
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
                    var trigger = button.GetComponent<EventTrigger>();
                    if (trigger != null)
                    {
                        trigger.triggers.Clear();
                    }
                }
            }
        }

        private static void AddBuildTooltipHandler(EventTrigger trigger, EventTriggerType eventType, System.Action action)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
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

                var canSelect = placementSystem == null || placementSystem.CanSelectBuilding(data);
                var canAfford = CanAffordBuildData(data);
                var canBuild = canSelect && canAfford;
                var isSelected = placementSystem != null && placementSystem.SelectedBuilding == data;
                var icon = EnsureBuildButtonIcon(button, data);
                button.interactable = canBuild;
                SetBuildButtonVisual(button, label, icon, canBuild, isSelected);
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
            labelBuilder.Append(index == 9 ? 0 : index + 1);
            labelBuilder.Append(". ");
            labelBuilder.Append(string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName);

            var costText = FormatBuildCosts(data);
            if (!string.IsNullOrEmpty(costText))
            {
                labelBuilder.Append('\n');
                labelBuilder.Append(costText);
            }

            return labelBuilder.ToString();
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

                costBuilder.Append($"电{data.powerCost}");
            }

            if (data.populationCost > 0)
            {
                if (costBuilder.Length > 0)
                {
                    costBuilder.Append("  ");
                }

                costBuilder.Append($"人口{data.populationCost}");
            }

            return costBuilder.ToString();
        }

        private string FormatCost(ResourceAmount cost)
        {
            return $"{cost.type.GetLabel()}{cost.amount}";
        }

        private void ShowBuildTooltip(int index)
        {
            if (buildTooltipPanel == null || buildTooltipText == null || buildButtonData == null || index < 0 || index >= buildButtonData.Length)
            {
                return;
            }

            hoveredBuildIndex = index;
            var data = buildButtonData[index];
            buildTooltipText.text = FormatBuildTooltip(data);
            buildTooltipPanel.SetActive(true);
            UpdateBuildTooltipPosition();
        }

        private void HideBuildTooltip(int index)
        {
            if (hoveredBuildIndex != index)
            {
                return;
            }

            hoveredBuildIndex = -1;
            if (buildTooltipPanel != null)
            {
                buildTooltipPanel.SetActive(false);
            }
        }

        private void UpdateBuildTooltipPosition()
        {
            if (buildTooltipPanel == null || !buildTooltipPanel.activeSelf)
            {
                return;
            }

            var canvasRect = transform as RectTransform;
            var tooltipRect = buildTooltipPanel.transform as RectTransform;
            if (canvasRect == null || tooltipRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out var localPoint))
            {
                return;
            }

            var x = Mathf.Clamp(localPoint.x + 180f, canvasRect.rect.xMin + 170f, canvasRect.rect.xMax - 170f);
            var y = Mathf.Clamp(localPoint.y - 72f, canvasRect.rect.yMin + 80f, canvasRect.rect.yMax - 80f);
            tooltipRect.anchoredPosition = new Vector2(x, y);
        }

        private string FormatBuildTooltip(BuildingData data)
        {
            if (data == null)
            {
                return "无建筑信息";
            }

            var builder = new StringBuilder();
            builder.AppendLine(string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName);

            var cost = FormatBuildCosts(data);
            builder.Append("消耗: ");
            builder.AppendLine(string.IsNullOrEmpty(cost) ? "无" : cost);

            var shortage = FormatShortage(data);
            if (!string.IsNullOrEmpty(shortage))
            {
                builder.Append("缺少: ");
                builder.AppendLine(shortage);
            }

            builder.Append("产出: ");
            builder.AppendLine(FormatProduction(data));

            if (data.trainableUnits != null && data.trainableUnits.Length > 0)
            {
                builder.Append("训练: ");
                for (var i = 0; i < data.trainableUnits.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("、");
                    }

                    var unit = data.trainableUnits[i];
                    builder.Append(unit != null && !string.IsNullOrWhiteSpace(unit.displayName) ? unit.displayName : "未知单位");
                }
            }

            return builder.ToString().TrimEnd();
        }

        private string FormatShortage(BuildingData data)
        {
            if (data == null || resourceManager == null) return string.Empty;

            var shortage = new StringBuilder();

            if (data.buildCost != null)
            {
                foreach (var cost in data.buildCost)
                {
                    var shortageAmount = cost.amount - resourceManager.Get(cost.type);
                    if (shortageAmount > 0)
                        shortage.Append($"  {cost.type.GetLabel()}缺{shortageAmount}");
                }
            }

            if (data.powerCost > 0)
            {
                var powerShortage = data.powerCost - resourceManager.Get(ResourceType.Power);
                if (powerShortage > 0)
                    shortage.Append($"  电力缺{powerShortage}");
            }

            if (data.populationCost > 0)
            {
                var popShortage = data.populationCost - resourceManager.Get(ResourceType.Population);
                if (popShortage > 0)
                    shortage.Append($"  人口缺{popShortage}");
            }

            return shortage.Length > 0 ? shortage.ToString() : string.Empty;
        }

        private string FormatProduction(BuildingData data)
        {
            var builder = new StringBuilder();
            AppendProduction(builder, data.produceType, data.produceAmount, data.produceInterval);

            if (data.extraProduction != null)
            {
                foreach (var production in data.extraProduction)
                {
                    AppendProduction(builder, production.type, production.amount, data.produceInterval);
                }
            }

            if (builder.Length == 0 && data.scaleWithContent != Jiangshi.Grid.CellContent.None)
            {
                builder.Append($"{ResourceName(data.produceType)} 随周围资源变化");
            }

            return builder.Length > 0 ? builder.ToString() : "无固定产出";
        }

        private static void AppendProduction(StringBuilder builder, ResourceType type, int amount, float interval)
        {
            if (amount <= 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("，");
            }

            var tick = interval > 0f ? interval : 5f;
            builder.Append($"每{tick:0.#}秒 +{amount}{ResourceName(type)}");
        }

        private static string ResourceName(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => "金币",
                ResourceType.Wood => "木材",
                ResourceType.Food => "食物",
                ResourceType.Power => "电力",
                ResourceType.Population => "人口",
                ResourceType.Iron => "铁",
                ResourceType.Copper => "铜",
                _ => type.ToString()
            };
        }

        private Image EnsureBuildButtonIcon(Button button, BuildingData data)
        {
            if (button == null)
            {
                return null;
            }

            var iconTransform = button.transform.Find("Icon");
            Image icon;
            if (iconTransform == null)
            {
                var iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(button.transform, false);
                iconObject.transform.SetAsFirstSibling();
                icon = iconObject.AddComponent<Image>();
                icon.preserveAspect = true;
            }
            else
            {
                icon = iconTransform.GetComponent<Image>();
                if (icon == null)
                {
                    icon = iconTransform.gameObject.AddComponent<Image>();
                }
            }

            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(25f, 0f);
            iconRect.sizeDelta = new Vector2(34f, 34f);

            icon.sprite = GetBuildingIcon(data);
            icon.enabled = icon.sprite != null;

            return icon;
        }

        private static Sprite GetBuildingIcon(BuildingData data)
        {
            if (data == null || data.prefab == null)
            {
                return null;
            }

            var spriteRenderer = data.prefab.GetComponentInChildren<SpriteRenderer>(true);
            return spriteRenderer != null ? spriteRenderer.sprite : null;
        }

        private void SetBuildButtonVisual(Button button, Text label, Image icon, bool canBuild, bool isSelected)
        {
            SetButtonColor(button, !canBuild ? buildButtonDisabledColor : isSelected ? buildButtonSelectedColor : buildButtonNormalColor);
            SetButtonOutline(button, !canBuild ? buildButtonDisabledOutlineColor : isSelected ? buildButtonSelectedOutlineColor : buildButtonNormalOutlineColor, isSelected ? 3f : 1f);

            if (icon != null)
            {
                icon.color = canBuild ? Color.white : new Color(0.45f, 0.48f, 0.48f, 0.72f);
            }

            if (label != null)
            {
                label.alignment = TextAnchor.MiddleLeft;
                var labelRect = label.GetComponent<RectTransform>();
                if (labelRect != null)
                {
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.pivot = new Vector2(0.5f, 0.5f);
                    labelRect.offsetMin = new Vector2(52f, 2f);
                    labelRect.offsetMax = new Vector2(-8f, -2f);
                }

                label.color = canBuild ? buildButtonTextColor : buildButtonDisabledTextColor;
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

        private Text portalAnnouncementText;
        private Coroutine portalAnnouncementRoutine;

        public void ShowPortalAnnouncement(string text)
        {
            EnsurePortalAnnouncement();

            if (portalAnnouncementText == null) return;

            if (portalAnnouncementRoutine != null)
                StopCoroutine(portalAnnouncementRoutine);

            portalAnnouncementText.text = text;
            portalAnnouncementText.gameObject.SetActive(true);
            portalAnnouncementRoutine = StartCoroutine(HidePortalAnnouncementAfterDelay(4f));
        }

        private void EnsurePortalAnnouncement()
        {
            if (portalAnnouncementText != null) return;

            var obj = new GameObject("Portal Announcement");
            obj.transform.SetParent(transform, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(600f, 80f);

            portalAnnouncementText = obj.AddComponent<Text>();
            portalAnnouncementText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            portalAnnouncementText.fontSize = 48;
            portalAnnouncementText.alignment = TextAnchor.MiddleCenter;
            portalAnnouncementText.color = new Color(0.3f, 0.95f, 1f, 1f);
            portalAnnouncementText.horizontalOverflow = HorizontalWrapMode.Wrap;
            portalAnnouncementText.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            obj.SetActive(false);
        }

        private System.Collections.IEnumerator HidePortalAnnouncementAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (portalAnnouncementText != null)
                portalAnnouncementText.gameObject.SetActive(false);
        }

        private void EnsureBuildTooltipUi()
        {
            if (buildTooltipPanel != null)
            {
                return;
            }

            buildTooltipPanel = CreateRuntimePanel(transform, "Building Info Tooltip", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 128f), new Color(0.035f, 0.047f, 0.052f, 0.96f));
            var canvasGroup = buildTooltipPanel.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            buildTooltipText = CreateRuntimeText(buildTooltipPanel.transform, "Tooltip Text", "", 16, Vector2.zero, new Vector2(312f, 104f), TextAnchor.UpperLeft);
            buildTooltipPanel.SetActive(false);
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
            demolitionTitleText.text = "未选择建筑";
            demolitionButtonLabel.text = "拆除";
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
            settingsButton.GetComponentInChildren<Text>().text = "菜单";

            settingsPanel = CreateRuntimePanel(root, "Settings Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 260f), new Color(0.035f, 0.045f, 0.052f, 0.94f));
            CreateRuntimeText(settingsPanel.transform, "Settings Title", "设置", 30, new Vector2(0f, 86f), new Vector2(340f, 44f), TextAnchor.MiddleCenter);
            CreateRuntimeText(settingsPanel.transform, "Settings Message", "游戏已暂停", 20, new Vector2(0f, 48f), new Vector2(340f, 32f), TextAnchor.MiddleCenter);

            settingsGuideButton = CreateRuntimeButton(settingsPanel.transform, "Settings Guide Button", "游戏指南", new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(180f, 42f));
            settingsCloseButton = CreateRuntimeButton(settingsPanel.transform, "Settings Close Button", "继续游戏", new Vector2(0.5f, 0.5f), new Vector2(-96f, -70f), new Vector2(150f, 44f));
            settingsQuitButton = CreateRuntimeButton(settingsPanel.transform, "Settings Quit Button", "退出游戏", new Vector2(0.5f, 0.5f), new Vector2(96f, -70f), new Vector2(150f, 44f));
            settingsCloseButton.onClick.AddListener(CloseSettings);
            settingsGuideButton.onClick.AddListener(OpenGuide);
            settingsQuitButton.onClick.AddListener(QuitGame);
            var settingsTexts = settingsPanel.GetComponentsInChildren<Text>();
            if (settingsTexts.Length > 0)
            {
                settingsTexts[0].text = "设置";
            }

            if (settingsTexts.Length > 1)
            {
                settingsTexts[1].text = "游戏已暂停";
            }

            settingsCloseButton.GetComponentInChildren<Text>().text = "继续";
            settingsGuideButton.GetComponentInChildren<Text>().text = "游戏指南";
            settingsQuitButton.GetComponentInChildren<Text>().text = "退出";
            settingsPanel.SetActive(false);

            guidePanel = CreateRuntimePanel(root, "Game Guide Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 430f), new Color(0.032f, 0.042f, 0.048f, 0.97f));
            CreateRuntimeText(guidePanel.transform, "Guide Title", "游戏指南", 30, new Vector2(0f, 176f), new Vector2(540f, 42f), TextAnchor.MiddleCenter);
            CreateRuntimeText(
                guidePanel.transform,
                "Guide Body",
                "目标\n守住指挥基地直到倒计时结束。第2波后会出现传送门，派人调查走廊可改变战局。\n\n视野\nWASD 或鼠标移到屏幕边缘移动视野，滚轮缩放。\n\n建造\n数字键 1-9/0 或右侧按钮选择建筑，左键放置，右键或 Esc 取消。城墙可按 Tab 旋转。鼠标悬停按钮可查看消耗和缺少的资源。\n\n资源\n木屋、伐木场、农场、电厂和矿场提供资源。电力不足建筑停摆，人口靠木屋提供，资源不够时按钮变暗。\n\n战斗\n兵工厂训练士兵和剑客。左键拖拽可框选多个单位，右键点敌人攻击、右键点地面移动。士兵远程开枪，剑客近战拦截，单位会自动绕过湖泊和树林。箭塔自动攻击范围内的敌人。\n\n操作\nP 暂停或继续；打开设置时自动暂停。点击建筑可查看拆除返还，核心建筑不能拆除。",
                18,
                new Vector2(0f, 4f),
                new Vector2(540f, 300f),
                TextAnchor.UpperLeft);
            guideCloseButton = CreateRuntimeButton(guidePanel.transform, "Guide Close Button", "关闭", new Vector2(0.5f, 0.5f), new Vector2(0f, -176f), new Vector2(150f, 42f));
            guideCloseButton.onClick.AddListener(CloseGuide);
            guideCloseButton.GetComponentInChildren<Text>().text = "关闭";
            guidePanel.SetActive(false);
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
