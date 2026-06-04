using UnityEngine;

/// <summary>
/// 放在走廊尽头的Trigger上（一个带Collider且勾选isTrigger的空物体）
/// 玩家走过去就会触发循环传送
/// </summary>
public class LoopTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LoopManager.Instance.OnPlayerReachEnd();
        }
    }
}
