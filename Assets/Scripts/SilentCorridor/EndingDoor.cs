using UnityEngine;

/// <summary>
/// 结局门：只有在最终Loop才能交互，按E开门触发任务完成转场
/// </summary>
public class EndingDoor : MonoBehaviour, IInteractable
{
    public int requiredLoop = 6;         // 第7轮（index从0开始为6）才能开
    public AudioClip doorOpenSound;
    public GameObject fadeOutPanel;       // 一个全屏黑色UI Panel，用于渐黑效果

    private bool ended = false;

    public void Interact()
    {
        if (ended) return;
        if (LoopManager.Instance.currentLoop < requiredLoop) return;

        ended = true;
        Debug.Log("任务完成转场触发：门打开了");

        // 停止所有音效
        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        foreach (var src in allAudio)
            src.Stop();

        // 播放开门声
        if (doorOpenSound != null)
        {
            AudioSource.PlayClipAtPoint(doorOpenSound, transform.position);
        }

        // 触发渐黑效果
        if (fadeOutPanel != null)
        {
            fadeOutPanel.SetActive(true);
            // 简单处理：直接显示黑屏，你也可以加动画渐变
        }

        // 后续真正融合时可在这里加载防守场景或交给任务结果管理器处理。
        Invoke(nameof(ShowMissionTransition), 3f);
    }

    void ShowMissionTransition()
    {
        Debug.Log("任务完成：等待返回基地流程接入");
    }
}
