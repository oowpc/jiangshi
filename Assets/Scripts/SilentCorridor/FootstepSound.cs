using UnityEngine;

/// <summary>
/// 玩家脚步声：走路时按节奏播放脚步音效
/// 挂在Player上
/// </summary>
public class FootstepSound : MonoBehaviour
{
    public AudioClip[] footstepClips;   // 可以放多个脚步声随机播放
    public float stepInterval = 0.5f;   // 走路每步间隔
    public float runStepInterval = 0.35f; // 跑步每步间隔
    public float volume = 0.4f;

    private AudioSource audioSource;
    private float stepTimer = 0f;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D音效（自己的脚步不需要方向感）
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f;

        if (isMoving)
        {
            float currentInterval = Input.GetKey(KeyCode.LeftShift) ? runStepInterval : stepInterval;
            stepTimer += Time.deltaTime;
            if (stepTimer >= currentInterval)
            {
                PlayStep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = stepInterval;
            // 停下时立刻停止脚步声
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    void PlayStep()
    {
        if (footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }
}
