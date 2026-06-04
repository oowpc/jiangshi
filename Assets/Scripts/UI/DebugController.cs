using Jiangshi.Economy;
using Jiangshi.Waves;
using UnityEngine;

namespace Jiangshi.UI
{
    public sealed class DebugController : MonoBehaviour
    {
        private bool showDebug;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                showDebug = !showDebug;
        }

        private void OnGUI()
        {
            if (!showDebug) return;

            var x = 20;
            var y = 200;
            var w = 220;
            var h = 30;
            var gap = 4;

            GUI.Box(new Rect(x - 6, y - 24, w + 12, 320), "调试面板");

            if (GUI.Button(new Rect(x, y, w, h), "跳过到下一波"))
            {
                if (WaveManager.Instance != null)
                    WaveManager.Instance.ForceNextWave();
            }
            y += h + gap;

            if (GUI.Button(new Rect(x, y, w, h), "+500 金币"))
            {
                var rm = FindObjectOfType<ResourceManager>();
                if (rm != null) rm.Add(ResourceType.Gold, 500);
            }
            y += h + gap;

            if (GUI.Button(new Rect(x, y, w, h), "+200 木材"))
            {
                var rm = FindObjectOfType<ResourceManager>();
                if (rm != null) rm.Add(ResourceType.Wood, 200);
            }
            y += h + gap;

            if (GUI.Button(new Rect(x, y, w, h), "+50 食物"))
            {
                var rm = FindObjectOfType<ResourceManager>();
                if (rm != null) rm.Add(ResourceType.Food, 50);
            }
            y += h + gap;

            if (GUI.Button(new Rect(x, y, w, h), "+50 电力"))
            {
                var rm = FindObjectOfType<ResourceManager>();
                if (rm != null) rm.Add(ResourceType.Power, 50);
            }
            y += h + gap;

            if (GUI.Button(new Rect(x, y, w, h), "杀死所有僵尸"))
            {
                var zombies = FindObjectsOfType<Units.Zombie>();
                foreach (var z in zombies)
                    z.GetComponent<Combat.Damageable>()?.TakeDamage(99999);
            }
            y += h + gap;

            if (GUI.Button(new Rect(x, y, w, h), "生成传送门"))
            {
                var wm = WaveManager.Instance;
                if (wm != null)
                {
                    var sf = wm.GetType().GetField("corridorTriggered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (sf != null) sf.SetValue(wm, true);
                }
            }
        }
    }
}
