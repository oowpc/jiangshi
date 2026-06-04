using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 通讯器系统：按Tab打开/关闭通讯器，每轮循环新增一条通讯
/// </summary>
public class PhoneUI : MonoBehaviour
{
    public GameObject phonePanel;        // 通讯器UI面板
    public TextMeshProUGUI messageText;  // 显示通讯内容的文本框
    public TextMeshProUGUI hintText;     // 屏幕下方提示文字（"按Tab查看通讯器"）
    public TextMeshProUGUI notifyText;   // 新通讯提示（"收到一条新通讯"）

    [TextArea(2, 4)]
    public string[] messages = new string[]
    {
        "<b>基地指挥</b>  23:42\n通讯确认。请报告走廊内部情况，基地防御压力增大。",
        "<b>医疗兵-陈静</b>  00:08\n收到你发回的档案照片，正在分析。你先别动，等我们找出血清方案。",
        "<b>未知信号</b>  00:27\n你走不出去了。",
        "<b>基地指挥</b>  00:50\n走廊尽头那扇门，你找到没有？攻势越来越猛，我们需要血清原液。",
        "<b>医疗兵-陈静</b>  01:15\n分析完了。血清配方在原实验体的收容区内。密码锁......你应该能拼出来。",
        "<b>未知信号</b>  01:33\n别开门。\n\n门一开，我们就自由了。\n\n你不想让我们出去的。"
    };

    private bool isOpen = false;
    private int visibleCount = 1;

    void Start()
    {
        phonePanel.SetActive(false);
        if (notifyText != null) notifyText.gameObject.SetActive(false);
        UpdateDisplay();
        // 开局显示操作提示
        if (hintText != null)
        {
            hintText.text = "按 Tab 查看通讯器";
            StartCoroutine(FadeOutHint(5f));
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isOpen = !isOpen;
            phonePanel.SetActive(isOpen);
            // 打开通讯器后隐藏新通讯提示
            if (isOpen && notifyText != null)
                notifyText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 由LoopManager事件调用，每轮新增一条通讯
    /// </summary>
    public void AddNewMessage()
    {
        visibleCount = Mathf.Min(visibleCount + 1, messages.Length);
        UpdateDisplay();
        // 显示新通讯提示
        StartCoroutine(ShowNotification());
    }

    private void UpdateDisplay()
    {
        string display = "";
        // 只显示最近3条消息，避免溢出
        int start = Mathf.Max(0, visibleCount - 3);
        for (int i = start; i < visibleCount && i < messages.Length; i++)
        {
            display += messages[i] + "\n\n";
        }
        if (messageText != null)
            messageText.text = display;
    }

    private IEnumerator ShowNotification()
    {
        if (notifyText == null) yield break;
        notifyText.text = "收到一条新通讯 [Tab]";
        notifyText.gameObject.SetActive(true);
        yield return new WaitForSeconds(4f);
        if (!isOpen) // 如果玩家还没打开通讯器就慢慢消失
        {
            notifyText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeOutHint(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }
}
