using UnityEngine;
using TMPro;

/// <summary>
/// 公告栏文字控制器，每个Loop显示不同内容
/// 挂在有TextMeshPro组件的物体上
/// </summary>
public class NoticeBoardText : MonoBehaviour
{
    [TextArea(2, 5)]
    public string[] loopTexts = new string[]
    {
        "期末考试安排：\n高等代数 6月15日\n实变函数 6月18日\n请同学们认真复习",
        "校心理咨询中心\n开放时间：周一至周五 9:00-17:00\n地点：学生活动中心302\n如需帮助请拨打：xxxx-xxxx",
        "通知：\n近期宿舍楼有同学反映\n夜间听到异常声响\n请大家注意安全 不要单独外出",
        "别\n回\n头",
        "你还记得那天发生了什么吗？",
        "醒醒",
        "门开了。"
    };

    private TextMeshPro tmp;

    void Start()
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp != null)
            tmp.text = loopTexts[0];
    }

    /// <summary>
    /// 由LoopManager事件调用，传入Loop编号（从0开始）
    /// </summary>
    public void UpdateText(int loopIndex)
    {
        if (tmp == null) tmp = GetComponent<TextMeshPro>();
        if (loopIndex < loopTexts.Length)
            tmp.text = loopTexts[loopIndex];
    }

    /// <summary>
    /// 无参版本，根据LoopManager当前Loop自动更新
    /// </summary>
    public void UpdateToCurrentLoop()
    {
        int loop = LoopManager.Instance.currentLoop;
        UpdateText(loop);
    }
}
