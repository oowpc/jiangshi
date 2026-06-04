using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 循环管理器：控制玩家走到走廊尽头后传送回起点，并触发对应Loop的事件
/// 场景中放一个空物体挂这个脚本，设置好起点位置和各Loop事件
/// </summary>
public class LoopManager : MonoBehaviour
{
    public static LoopManager Instance { get; private set; }

    [Header("设置")]
    public Transform playerTransform;   // 拖入Player
    public Transform startPoint;        // 走廊起点（空物体，标记位置）
    public int maxLoops = 7;

    [Header("状态（只读）")]
    public int currentLoop = 0;

    [Header("每个Loop的事件")]
    public List<LoopEvents> loopEventsList = new List<LoopEvents>();

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 当玩家触碰走廊尽头的Trigger时调用此方法
    /// </summary>
    public void OnPlayerReachEnd()
    {
        // 最终Loop不再传送，让玩家可以开门
        if (currentLoop >= maxLoops - 1)
        {
            Debug.Log("最终循环，不再传送");
            return;
        }

        // 传送玩家回起点
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        cc.enabled = false;
        playerTransform.position = startPoint.position;
        playerTransform.rotation = startPoint.rotation;
        cc.enabled = true;

        // 进入下一个Loop
        currentLoop++;
        Debug.Log($"进入 Loop {currentLoop}");

        // 触发对应Loop的事件
        if (currentLoop - 1 < loopEventsList.Count)
        {
            loopEventsList[currentLoop - 1].onLoopStart?.Invoke();
        }
    }
}

/// <summary>
/// 每个Loop对应的事件配置
/// </summary>
[System.Serializable]
public class LoopEvents
{
    public string loopName;         // 方便在Inspector里看，比如"Loop3-脚步声"
    public UnityEvent onLoopStart;  // 进入这个Loop时触发的事件
}
