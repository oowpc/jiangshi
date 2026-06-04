using UnityEngine;

/// <summary>
/// 简单音频管理器，用于播放环境音和事件音效
/// 场景中放一个空物体挂这个脚本
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频源")]
    public AudioSource ambientSource;   // 环境音（循环播放）
    public AudioSource sfxSource;       // 事件音效（单次播放）

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 播放一次性音效（如门响、脚步声）
    /// 可以在LoopManager的UnityEvent里直接调用
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 切换环境音
    /// </summary>
    public void ChangeAmbient(AudioClip clip)
    {
        ambientSource.clip = clip;
        ambientSource.loop = true;
        ambientSource.Play();
    }

    /// <summary>
    /// 停止环境音
    /// </summary>
    public void StopAmbient()
    {
        ambientSource.Stop();
    }
}
