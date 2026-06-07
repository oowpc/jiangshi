using UnityEngine;
using UnityEngine.SceneManagement;
using Jiangshi.Core;

public class CorridorEntry : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;
        GameAudioSettings.ApplySavedMasterVolume();
        GameDisplaySettings.ApplySavedWindowMode();
        SceneManager.SetActiveScene(gameObject.scene);
        CorridorSceneBridge.SuspendDefenseScenes(gameObject.scene);
    }
}
