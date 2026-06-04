using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 实验记录系统：按J打开/关闭实验记录，每轮循环新增一页内容
/// </summary>
public class DiaryUI : MonoBehaviour
{
    public GameObject diaryPanel;         // 实验记录UI面板
    public TextMeshProUGUI diaryText;     // 实验记录文字内容
    public TextMeshProUGUI pageText;      // 页码显示

    [TextArea(3, 6)]
    public string[] pages = new string[]
    {
        "记录一\n\n基地检测到走廊方向有异常的生物质信号。队长派我进去查。\n\n穿过那扇门之后，我走了一圈，又回到了起点。空间像被折叠了。我在墙上刻了一道记号。这是第 1 次。",

        "记录二\n\n墙上的旧标记已经被人反复划过。分不清之前有多少人来过这里。\n\n在 407 房找到了一些被撕掉大半的实验档案。能拼起来的只有一条：实验代号「1」号计划--针对某种未知病原体的收容实验。\n\n后面全被撕了。只能等下一轮看看有没有其他房间能进。",

        "记录三\n\n听到了脚步声。不是我的。我停下来，它也停。\n\n走廊尽头有一扇门上锁了，需要四位数密码。门旁边有个金属铭牌，锈得厉害，但上面有一行：「样本采集日期：-- 月 9 日」。\n\n墙上不知谁写着：不要回头。",

        "记录四\n\n灯开始闪了。灭掉的时候能看到那个黑影，比以前近了。\n\n又翻到一页档案：实验体共 5 例。前四例在接触后 72 小时内全部异变死亡。第五例的情况被涂掉了，只留了半句话「......失控，收容区封锁」。\n\n我觉得我背后的东西，就是第五例。",

        "记录五\n\n它开始跟我说话了。墙里面传出来的。\n\n「外面的人已经都变成了我们。你守不住了。」\n\n我找到铭牌的另一块残片，拼上去后补全了日期：-- 月 9 日，-- 号房，-- 时 58 分。\n\n密码的线索在逐渐拼起来了。但如果我现在回头看一眼的话，也许能看得更快......\n\n不。绝对不能回头。",

        "记录六\n\n它过来了。\n\n实验代号 1。9 月采集。5 例实验体。58 分失控。\n\n密码是 1958。\n\n如果你能打开那扇门，进去把收容锁关掉--替我们所有人结束这一切。\n\n我要走了。它在看我。\n\n对不起，队长。"
    };

    private bool isOpen = false;
    private int visiblePages = 0;
    private int currentPage = 0;

    void Start()
    {
        diaryPanel.SetActive(false);
    }

    [Header("追逐触发")]
    public Chaser chaser;
    public int chaseOnPage = 5;             // 看到最后一页后关闭触发追逐
    private bool chaseTriggered = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (visiblePages == 0) return; // 还没捡到实验记录
            isOpen = !isOpen;
            diaryPanel.SetActive(isOpen);
            if (isOpen)
            {
                ShowCurrentPage();
            }
            else
            {
                // 关闭实验记录时检查是否触发追逐
                if (!chaseTriggered && currentPage >= chaseOnPage && chaser != null)
                {
                    chaseTriggered = true;
                    chaser.StartChase();
                }
            }
        }

        if (!isOpen) return;

        // 左右翻页
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentPage > 0)
            {
                currentPage--;
                ShowCurrentPage();
            }
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentPage < visiblePages - 1)
            {
                currentPage++;
                ShowCurrentPage();
            }
        }
    }

    /// <summary>
    /// 由LoopManager事件调用，每轮新增一页
    /// </summary>
    public void AddPage()
    {
        if (visiblePages < pages.Length)
        {
            visiblePages++;
            currentPage = visiblePages - 1; // 自动翻到最新页
        }
    }

    /// <summary>
    /// 开局捡到实验记录时调用（第一次获得实验记录+第一页内容）
    /// </summary>
    public void PickUpDiary()
    {
        visiblePages = 1;
        currentPage = 0;
    }

    private void ShowCurrentPage()
    {
        if (currentPage < pages.Length)
            diaryText.text = pages[currentPage];
        if (pageText != null)
            pageText.text = (currentPage + 1) + " / " + visiblePages;
    }
}
