using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// 追逐者：激活后朝玩家移动，追上触发跳脸
/// </summary>
public class Chaser : MonoBehaviour
{
    public Transform player;
    public float speed = 2.5f;
    public float catchDistance = 1.5f;      // 追上的距离
    public float lookBackDotThreshold = 0.3f; // 玩家回头判定

    [Header("跳脸")]
    public GameObject jumpscarePanel;       // 跳脸图片UI
    public AudioClip jumpscareSound;
    public GameObject deathPanel;           // 操作者失联黑屏

    [Header("追逐BGM")]
    public AudioSource chaseBGMSource;
    public AudioClip chaseBGM;

    private bool isChasing = false;
    private bool isDead = false;

    void Start()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 开始追逐
    /// </summary>
    public void StartChase()
    {
        if (MissionResultState.Result == MissionResult.SerumAcquired)
            return;

        gameObject.SetActive(true);
        isChasing = true;

        // 切换BGM
        if (chaseBGMSource != null && chaseBGM != null)
        {
            // 停止所有其他音效
            foreach (var src in FindObjectsOfType<AudioSource>())
            {
                if (src != chaseBGMSource) src.Stop();
            }
            chaseBGMSource.clip = chaseBGM;
            chaseBGMSource.loop = true;
            chaseBGMSource.Play();
        }
    }

    /// <summary>
    /// 停止追逐（密码输入成功时调用）
    /// </summary>
    public void StopChase()
    {
        isChasing = false;
        CancelInvoke(nameof(ShowDeath));
        if (jumpscarePanel != null) jumpscarePanel.SetActive(false);
        if (chaseBGMSource != null) chaseBGMSource.Stop();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isChasing || isDead) return;

        // 朝玩家移动
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.position += dir * speed * Time.deltaTime;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        // 检测是否追上
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < catchDistance)
        {
            TriggerJumpscare();
        }

        // 检测玩家是否回头看
        Vector3 toChaser = (transform.position - player.position).normalized;
        float dot = Vector3.Dot(player.forward, toChaser);
        if (dot > lookBackDotThreshold)
        {
            // 确认在视野内
            Camera cam = player.GetComponentInChildren<Camera>();
            Vector3 screenPos = cam.WorldToViewportPoint(transform.position);
            if (screenPos.x > 0 && screenPos.x < 1 && screenPos.y > 0 && screenPos.y < 1 && screenPos.z > 0)
            {
                TriggerJumpscare();
            }
        }
    }

    void TriggerJumpscare()
    {
        if (MissionResultState.Result == MissionResult.SerumAcquired) return;
        if (isDead) return;
        isDead = true;
        isChasing = false;

        // 停止追逐BGM
        if (chaseBGMSource != null) chaseBGMSource.Stop();

        // 播放跳脸音效
        if (jumpscareSound != null)
            AudioSource.PlayClipAtPoint(jumpscareSound, player.position);

        // 显示跳脸图片
        if (jumpscarePanel != null)
            jumpscarePanel.SetActive(true);

        // 锁定玩家
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        // 1秒后切换到任务失败
        Invoke(nameof(ShowDeath), 1.5f);
    }

    void ShowDeath()
    {
        if (MissionResultState.Result == MissionResult.SerumAcquired) return;

        MissionResultState.Result = MissionResult.OperatorLost;

        if (jumpscarePanel != null) jumpscarePanel.SetActive(false);
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
            TMPro.TextMeshProUGUI resultText = deathPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (resultText != null)
                resultText.text = "操作者失联\n未取得血清原液";
        }

        Invoke(nameof(ReturnToDefenseScene), 3f);
    }

    void ReturnToDefenseScene()
    {
        SceneManager.LoadScene("Prototype");
    }
}
