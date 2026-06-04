using UnityEngine;

/// <summary>
/// 灯光闪烁效果，挂在有Light组件的物体上
/// 默认关闭，由LoopManager的事件激活
/// </summary>
public class LightFlicker : MonoBehaviour
{
    public Light targetLight;
    public float minInterval = 0.05f;
    public float maxInterval = 0.3f;
    public float flickerDuration = 5f; // 闪烁持续时间，0表示一直闪

    private bool isFlickering = false;
    private float timer = 0f;
    private float nextToggle = 0f;
    private float elapsed = 0f;

    /// <summary>
    /// 在Inspector的UnityEvent里调用这个方法来启动闪烁
    /// </summary>
    public void StartFlicker()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();
        isFlickering = true;
        elapsed = 0f;
        nextToggle = Random.Range(minInterval, maxInterval);
    }

    public void StopFlicker()
    {
        isFlickering = false;
        if (targetLight != null) targetLight.enabled = true;
    }

    void Update()
    {
        if (!isFlickering) return;

        timer += Time.deltaTime;
        elapsed += Time.deltaTime;

        if (flickerDuration > 0 && elapsed >= flickerDuration)
        {
            StopFlicker();
            return;
        }

        if (timer >= nextToggle)
        {
            targetLight.enabled = !targetLight.enabled;
            timer = 0f;
            nextToggle = Random.Range(minInterval, maxInterval);
        }
    }
}
