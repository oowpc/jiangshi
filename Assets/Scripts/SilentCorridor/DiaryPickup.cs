using UnityEngine;
using TMPro;

/// <summary>
/// 实验记录拾取物：玩家按E捡起后激活实验记录系统，物体消失
/// 挂在场景中的记录本物体上，需要设置Layer为Interactable
/// </summary>
public class DiaryPickup : MonoBehaviour, IInteractable
{
    public DiaryUI diaryUI;                // 拖入DiaryUI
    public TextMeshProUGUI hintText;       // 拖入提示文字（和通讯器共用那个Hint）
    public GameObject glowEffect;          // 可选：发光提示物体

    private bool picked = false;

    public void Interact()
    {
        if (picked) return;
        picked = true;

        // 激活实验记录系统
        if (diaryUI != null)
            diaryUI.PickUpDiary();

        // 显示提示
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "获得了一份实验记录  [按J查看]";
            Invoke(nameof(HideHint), 4f);
        }

        // 隐藏地上的记录本
        gameObject.SetActive(false);
    }

    void HideHint()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }
}
