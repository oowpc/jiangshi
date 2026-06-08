using UnityEngine;
using UnityEngine.SceneManagement;
public class SerumPickup : MonoBehaviour, IInteractable
{
    [Header("组件引用")]
    public GameObject pickupEffect;
    public GameObject successPanel;
    public GameObject capObject;
    public float delayBeforeReturn = 3f;

    private bool picked;

    public void Interact()
    {
        if (picked) return;
        picked = true;

        Debug.Log("[SerumPickup] 药剂被拾取，设置 MissionResult.SerumAcquired");

        MissionResultState.Result = MissionResult.SerumAcquired;

        if (pickupEffect != null)
            pickupEffect.SetActive(true);

        if (successPanel != null)
        {
            successPanel.SetActive(true);
            var tmp = successPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp != null)
                tmp.text = "血清原液已取得\n任务完成，正在返回基地";
        }

        var renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        if (capObject != null)
            capObject.SetActive(false);

        Debug.Log($"[SerumPickup] {delayBeforeReturn}秒后卸载 SampleScene");
        Invoke(nameof(ReturnToDefenseScene), delayBeforeReturn);
    }

    void ReturnToDefenseScene()
    {
        Debug.Log("[SerumPickup] ReturnToDefenseScene 被调用，加载 Prototype");
        SceneManager.LoadScene("Prototype");
    }
}
