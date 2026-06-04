using UnityEngine;

/// <summary>
/// 控制物体的显示和隐藏，用于鬼影出现、文字显现等
/// 挂在需要控制的物体上，通过LoopManager的UnityEvent调用
/// </summary>
public class ObjectRevealer : MonoBehaviour
{
    public GameObject targetObject; // 要控制的物体，不设置则控制自身

    void Start()
    {
        // 默认隐藏
        GetTarget().SetActive(false);
    }

    public void Show()
    {
        GetTarget().SetActive(true);
    }

    public void Hide()
    {
        GetTarget().SetActive(false);
    }

    /// <summary>
    /// 延迟显示（比如走到某处3秒后出现）
    /// </summary>
    public void ShowAfterDelay(float delay)
    {
        Invoke(nameof(Show), delay);
    }

    private GameObject GetTarget()
    {
        return targetObject != null ? targetObject : gameObject;
    }
}
