using UnityEngine;

/// <summary>
/// 恐怖音效控制器，挂在GameManager上
/// 在LoopManager的每个Loop事件里调用对应方法
/// </summary>
public class HorrorAudio : MonoBehaviour
{
    [Header("音效文件（从Assets/Audio拖入）")]
    public AudioClip ambientDrone;       // 环境嗡鸣
    public AudioClip footsteps;          // 身后脚步声
    public AudioClip doorBang;           // 砸门声
    public AudioClip lightBuzz;          // 灯泡闪烁声
    public AudioClip whisper;            // 鬼语

    [Header("音频源")]
    public AudioSource ambientSource;    // 环境音源（循环）
    public AudioSource sfxSource;        // 事件音效源
    public AudioSource behindSource;     // 身后音效源（放在玩家身后制造方向感）

    /// <summary>
    /// Loop1调用：开始播放环境嗡鸣
    /// </summary>
    public void StartAmbient()
    {
        ambientSource.clip = ambientDrone;
        ambientSource.loop = true;
        ambientSource.volume = 0.5f;
        ambientSource.Play();
    }

    /// <summary>
    /// Loop2调用：环境音变大，增加不安感
    /// </summary>
    public void IncreaseAmbient()
    {
        ambientSource.volume = 0.8f;
    }

    /// <summary>
    /// Loop3调用：播放身后脚步声
    /// </summary>
    public void PlayFootstepsBehind()
    {
        behindSource.clip = footsteps;
        behindSource.Play();
    }

    /// <summary>
    /// Loop4调用：播放灯泡闪烁声
    /// </summary>
    public void PlayLightBuzz()
    {
        sfxSource.PlayOneShot(lightBuzz);
    }

    /// <summary>
    /// Loop5调用：播放低语声
    /// </summary>
    public void PlayWhisper()
    {
        sfxSource.PlayOneShot(whisper, 0.7f);
    }

    /// <summary>
    /// Loop6调用：砸门声
    /// </summary>
    public void PlayDoorBang()
    {
        sfxSource.PlayOneShot(doorBang);
    }

    /// <summary>
    /// 延迟播放（用于制造节奏感）
    /// </summary>
    public void PlayFootstepsDelayed()
    {
        Invoke(nameof(PlayFootstepsBehind), 2f);
    }

    public void PlayWhisperDelayed()
    {
        Invoke(nameof(PlayWhisper), 3f);
    }
}
