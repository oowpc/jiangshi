using Jiangshi.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jiangshi.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string DefaultMenuMusicResource = "Audio/Menu/PrizeMenu";

        [SerializeField] private string gameSceneName = "Prototype";
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Button windowModeButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text volumeValueText;
        [SerializeField] private Text windowModeButtonLabel;
        [SerializeField] private AudioClip menuMusicClip;
        [SerializeField, Range(0f, 1f)] private float menuMusicVolume = 0.75f;

        private AudioSource menuMusicSource;

        private void Start()
        {
            GameAudioSettings.ApplySavedMasterVolume();
            GameDisplaySettings.ApplySavedWindowMode();
            EnsureMenuMusicSource();
            PlayMenuMusic();

            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(ToggleSettings);
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.AddListener(CloseSettings);
            }

            if (windowModeButton != null)
            {
                windowModeButton.onClick.AddListener(ToggleWindowMode);
            }

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(GameAudioSettings.MasterVolume);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            RefreshVolumeText(GameAudioSettings.MasterVolume);
            RefreshWindowModeText();
            CloseSettings();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(ToggleSettings);
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.RemoveListener(CloseSettings);
            }

            if (windowModeButton != null)
            {
                windowModeButton.onClick.RemoveListener(ToggleWindowMode);
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            }
        }

        private void StartGame()
        {
            Time.timeScale = 1f;
            if (menuMusicSource != null)
            {
                menuMusicSource.Stop();
            }

            SceneManager.LoadScene(gameSceneName);
        }

        private void ToggleSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            }
        }

        private void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void OnVolumeChanged(float volume)
        {
            GameAudioSettings.SetMasterVolume(volume);
            RefreshVolumeText(volume);
        }

        private void ToggleWindowMode()
        {
            GameDisplaySettings.SetWindowed(!GameDisplaySettings.IsWindowed);
            RefreshWindowModeText();
        }

        private void RefreshVolumeText(float volume)
        {
            if (volumeValueText != null)
            {
                volumeValueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%";
            }
        }

        private void RefreshWindowModeText()
        {
            if (windowModeButtonLabel != null)
            {
                windowModeButtonLabel.text = GameDisplaySettings.IsWindowed ? "全屏" : "窗口化";
            }
        }

        private void EnsureMenuMusicSource()
        {
            if (menuMusicClip == null)
            {
                menuMusicClip = Resources.Load<AudioClip>(DefaultMenuMusicResource);
            }

            if (menuMusicSource == null)
            {
                menuMusicSource = gameObject.AddComponent<AudioSource>();
                menuMusicSource.playOnAwake = false;
                menuMusicSource.loop = true;
                menuMusicSource.spatialBlend = 0f;
            }

            menuMusicSource.volume = menuMusicVolume;
        }

        private void PlayMenuMusic()
        {
            EnsureMenuMusicSource();
            if (menuMusicClip == null || menuMusicSource == null)
            {
                return;
            }

            menuMusicSource.clip = menuMusicClip;
            menuMusicSource.volume = menuMusicVolume;
            menuMusicSource.loop = true;
            menuMusicSource.Play();
        }
    }
}
