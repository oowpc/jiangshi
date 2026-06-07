using UnityEngine;

namespace Jiangshi.Core
{
    public static class GameAudioSettings
    {
        private const string MasterVolumeKey = "Jiangshi.MasterVolume";
        public const float DefaultMasterVolume = 1f;

        public static float MasterVolume => PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume);

        public static void ApplySavedMasterVolume()
        {
            AudioListener.volume = Mathf.Clamp01(MasterVolume);
        }

        public static void SetMasterVolume(float volume)
        {
            var clampedVolume = Mathf.Clamp01(volume);
            AudioListener.volume = clampedVolume;
            PlayerPrefs.SetFloat(MasterVolumeKey, clampedVolume);
            PlayerPrefs.Save();
        }
    }
}
