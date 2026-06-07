using UnityEngine;

namespace Jiangshi.Core
{
    public static class GameDisplaySettings
    {
        private const string WindowedKey = "Jiangshi.Windowed";

        public static bool IsWindowed => PlayerPrefs.GetInt(WindowedKey, 0) != 0;

        public static void ApplySavedWindowMode()
        {
            ApplyWindowMode(IsWindowed);
        }

        public static void SetWindowed(bool windowed)
        {
            PlayerPrefs.SetInt(WindowedKey, windowed ? 1 : 0);
            PlayerPrefs.Save();
            ApplyWindowMode(windowed);
        }

        private static void ApplyWindowMode(bool windowed)
        {
            if (windowed)
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.fullScreen = false;
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = true;
            }
        }
    }
}
