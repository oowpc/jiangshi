using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jiangshi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jiangshi.Editor
{
    public static class MainMenuSetupMenu
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string BackgroundPath = "Assets/Art/Menu/MainMenuBackground.png";
        private const string MenuMusicPath = "Assets/Resources/Audio/Menu/PrizeMenu.mp3";

        [MenuItem("Jiangshi/Setup/Create Main Menu Scene")]
        public static void CreateMainMenuScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Jiangshi", "Exit Play Mode before creating the main menu scene.", "OK");
                return;
            }

            CreateMainMenuSceneForBatch();
            EditorUtility.DisplayDialog("Jiangshi", $"Main menu scene created:\n{ScenePath}", "OK");
        }

        public static void CreateMainMenuSceneForBatch()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            CreateCamera();
            var controller = CreateMenuCanvas();
            _ = controller;
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetMainMenuFirstInBuildSettings();
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.AddComponent<AudioListener>();
        }

        private static MainMenuController CreateMenuCanvas()
        {
            var canvasObject = new GameObject("Main Menu Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            var controller = canvasObject.AddComponent<MainMenuController>();

            var background = CreateBackground(canvasObject.transform);
            background.transform.SetAsFirstSibling();

            var shade = CreateImage(canvasObject.transform, "Menu Readability Shade", new Color(0f, 0f, 0f, 0.34f));
            SetupStretch(shade.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var title = CreateText(canvasObject.transform, "Title", "僵尸入侵", 66, TextAnchor.MiddleLeft, new Color(0.94f, 0.96f, 0.9f, 1f));
            SetupAnchored(title.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(124f, 168f), new Vector2(520f, 82f));

            var subtitle = CreateText(canvasObject.transform, "Subtitle", "守住基地，穿过异常传送门", 24, TextAnchor.MiddleLeft, new Color(0.78f, 0.88f, 0.84f, 1f));
            SetupAnchored(subtitle.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(128f, 106f), new Vector2(520f, 40f));

            var startButton = CreateButton(canvasObject.transform, "Start Button", "开始游戏", new Vector2(0f, 0.5f), new Vector2(232f, -8f), new Vector2(240f, 56f));
            var settingsButton = CreateButton(canvasObject.transform, "Main Settings Button", "设置", new Vector2(0f, 0.5f), new Vector2(232f, -82f), new Vector2(240f, 56f));

            var settingsPanel = CreateSettingsPanel(canvasObject.transform, out var volumeSlider, out var volumeValueText, out var windowButton, out var windowButtonLabel, out var closeButton);

            SetString(controller, "gameSceneName", "Prototype");
            SetObjectReference(controller, "startButton", startButton);
            SetObjectReference(controller, "settingsButton", settingsButton);
            SetObjectReference(controller, "settingsCloseButton", closeButton);
            SetObjectReference(controller, "windowModeButton", windowButton);
            SetObjectReference(controller, "settingsPanel", settingsPanel);
            SetObjectReference(controller, "volumeSlider", volumeSlider);
            SetObjectReference(controller, "volumeValueText", volumeValueText);
            SetObjectReference(controller, "windowModeButtonLabel", windowButtonLabel);
            SetObjectReference(controller, "menuMusicClip", AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicPath));
            SetFloat(controller, "menuMusicVolume", 0.75f);

            return controller;
        }

        private static RawImage CreateBackground(Transform parent)
        {
            var backgroundObject = new GameObject("Main Menu Background");
            backgroundObject.transform.SetParent(parent, false);
            SetupStretch(backgroundObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var image = backgroundObject.AddComponent<RawImage>();
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            image.color = Color.white;
            return image;
        }

        private static GameObject CreateSettingsPanel(
            Transform parent,
            out Slider volumeSlider,
            out Text volumeValueText,
            out Button windowButton,
            out Text windowButtonLabel,
            out Button closeButton)
        {
            var panel = CreateImage(parent, "Main Menu Settings Panel", new Color(0.035f, 0.045f, 0.052f, 0.94f));
            SetupAnchored(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 300f));
            panel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.45f);

            var title = CreateText(panel.transform, "Settings Title", "设置", 32, TextAnchor.MiddleCenter, new Color(0.94f, 0.96f, 0.93f, 1f));
            SetupAnchored(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(320f, 44f));

            var volumeLabel = CreateText(panel.transform, "Volume Label", "主音量", 18, TextAnchor.MiddleRight, new Color(0.94f, 0.96f, 0.93f, 1f));
            SetupAnchored(volumeLabel.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-130f, -100f), new Vector2(86f, 28f));
            volumeSlider = CreateSlider(panel.transform, "Main Menu Volume Slider", new Vector2(0.5f, 1f), new Vector2(22f, -100f), new Vector2(208f, 28f));
            volumeValueText = CreateText(panel.transform, "Main Menu Volume Value", "100%", 18, TextAnchor.MiddleLeft, new Color(0.94f, 0.96f, 0.93f, 1f));
            SetupAnchored(volumeValueText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(164f, -100f), new Vector2(64f, 28f));

            windowButton = CreateButton(panel.transform, "Main Menu Window Mode Button", "窗口化", new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(180f, 42f));
            windowButtonLabel = windowButton.GetComponentInChildren<Text>();
            closeButton = CreateButton(panel.transform, "Main Menu Settings Close Button", "关闭", new Vector2(0.5f, 1f), new Vector2(0f, -222f), new Vector2(160f, 42f));

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            SetupAnchored(buttonObject, anchor, anchor, new Vector2(0.5f, 0.5f), anchoredPosition, size);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.19f, 0.22f, 0.94f);
            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.62f, 0.62f, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.18f, 0.31f, 0.34f, 1f);
            colors.pressedColor = new Color(0.08f, 0.13f, 0.15f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var text = CreateText(buttonObject.transform, "Label", label, 22, TextAnchor.MiddleCenter, new Color(0.94f, 0.96f, 0.93f, 1f));
            SetupStretch(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var sliderObject = new GameObject(name);
            sliderObject.transform.SetParent(parent, false);
            SetupAnchored(sliderObject, anchor, anchor, new Vector2(0.5f, 0.5f), anchoredPosition, size);

            var background = CreateImage(sliderObject.transform, "Background", new Color(0.08f, 0.105f, 0.12f, 0.96f));
            SetupStretch(background.gameObject, Vector2.zero, Vector2.one, new Vector2(8f, 9f), new Vector2(-8f, -9f));

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            SetupStretch(fillArea, Vector2.zero, Vector2.one, new Vector2(8f, 9f), new Vector2(-8f, -9f));

            var fill = CreateImage(fillArea.transform, "Fill", new Color(0.1f, 0.72f, 0.7f, 1f));
            SetupStretch(fill.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            SetupStretch(handleArea, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));

            var handle = CreateImage(handleArea.transform, "Handle", new Color(0.94f, 0.96f, 0.93f, 1f));
            SetupAnchored(handle.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 28f));

            var slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle;
            return slider;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void SetMainMenuFirstInBuildSettings()
        {
            var existing = EditorBuildSettings.scenes
                .Where(scene => !string.IsNullOrEmpty(scene.path) && scene.path != ScenePath)
                .ToList();

            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            scenes.AddRange(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetupAnchored(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = obj.AddComponent<RectTransform>();
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void SetupStretch(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = obj.AddComponent<RectTransform>();
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new System.InvalidOperationException($"Missing serialized property {propertyName} on {target.name}");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new System.InvalidOperationException($"Missing serialized property {propertyName} on {target.name}");
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new System.InvalidOperationException($"Missing serialized property {propertyName} on {target.name}");
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
