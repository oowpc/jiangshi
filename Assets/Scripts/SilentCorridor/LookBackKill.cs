using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 回头杀：特定Loop中如果玩家回头看到身后的鬼，游戏直接结束
/// 挂在身后的鬼物体上，由LoopManager事件激活
/// </summary>
public class LookBackKill : MonoBehaviour
{
    public Transform player;
    public GameObject ghostBehind;      // 身后的鬼（默认隐藏）
    public GameObject deathPanel;       // 操作者失联黑屏UI
    public AudioClip scareSound;        // 吓人音效

    private bool isActive = false;
    private bool isDead = false;

    void Start()
    {
        if (ghostBehind != null)
            ghostBehind.SetActive(false);
    }

    /// <summary>
    /// 由LoopManager事件调用，激活回头杀机制
    /// </summary>
    public void Activate()
    {
        isActive = true;
        if (ghostBehind != null)
            ghostBehind.SetActive(true);
    }

    void Update()
    {
        if (!isActive || isDead) return;

        // 检测玩家是否面朝身后的鬼（点积为正说明面朝它）
        Vector3 toGhost = (ghostBehind.transform.position - player.position).normalized;
        float dot = Vector3.Dot(player.forward, toGhost);

        // dot > 0.5 说明玩家大致面朝鬼的方向（回头了）
        if (dot > 0.5f)
        {
            // 检测是否真的看到了（射线检测）
            Camera cam = player.GetComponentInChildren<Camera>();
            Vector3 screenPos = cam.WorldToViewportPoint(ghostBehind.transform.position);
            if (screenPos.x > 0.1f && screenPos.x < 0.9f && screenPos.y > 0.1f && screenPos.y < 0.9f && screenPos.z > 0)
            {
                TriggerDeath();
            }
        }
    }

    void TriggerDeath()
    {
        isDead = true;
        MissionResultState.Result = MissionResult.OperatorLost;

        if (scareSound != null)
            AudioSource.PlayClipAtPoint(scareSound, player.position);

        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        foreach (var src in allAudio) src.Stop();

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
            TMPro.TextMeshProUGUI resultText = deathPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (resultText != null)
                resultText.text = "操作者失联\n未取得血清原液";
        }

        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        Invoke(nameof(ReturnToDefenseScene), 3f);
    }

    void ReturnToDefenseScene()
    {
        SceneManager.LoadScene("Prototype");
    }
}
