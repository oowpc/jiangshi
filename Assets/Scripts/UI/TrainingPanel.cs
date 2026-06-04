using Jiangshi.Economy;
using Jiangshi.Units;
using UnityEngine;

namespace Jiangshi.UI
{
    public sealed class TrainingPanel : MonoBehaviour
    {
        private UnitSpawner activeSpawner;
        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && cam != null)
            {
                var ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f))
                {
                    var building = hit.collider.GetComponentInParent<Jiangshi.Building.Building>();
                    if (building != null)
                    {
                        var spawner = building.GetComponent<UnitSpawner>();
                        if (spawner != null && spawner.TrainableUnits != null && spawner.TrainableUnits.Length > 0)
                        {
                            activeSpawner = spawner;
                            return;
                        }
                    }
                }

                // Clicked elsewhere, close panel
                if (!IsMouseOverPanel())
                    activeSpawner = null;
            }

            if (Input.GetMouseButtonDown(1))
                activeSpawner = null;
        }

        private void OnGUI()
        {
            if (activeSpawner == null) return;

            var units = activeSpawner.TrainableUnits;
            if (units == null || units.Length == 0) return;

            var panelW = 200f;
            var btnH = 40f;
            var padding = 8f;
            var headerH = 30f;
            var progressH = 16f;
            var panelH = headerH + padding + units.Length * (btnH + 4f) + progressH + padding * 2;

            var panelX = Screen.width - panelW - 20f;
            var panelY = Screen.height - panelH - 20f;
            var panelRect = new Rect(panelX, panelY, panelW, panelH);

            GUI.Box(panelRect, "");
            GUI.Label(new Rect(panelX + padding, panelY + padding, panelW - padding * 2, headerH),
                "<b><size=14>训练单位</size></b>");

            var y = panelY + headerH + padding;
            foreach (var unit in units)
            {
                if (unit == null) continue;

                var cost = GetCostText(unit);
                var label = $"{unit.displayName}  {cost}  ({unit.trainingTime}秒)";
                if (GUI.Button(new Rect(panelX + padding, y, panelW - padding * 2, btnH), label))
                {
                    activeSpawner.TryTrain(unit);
                }
                y += btnH + 4f;
            }

            // Progress bar
            if (activeSpawner.CurrentTraining != null)
            {
                var barRect = new Rect(panelX + padding, y, panelW - padding * 2, progressH);
                GUI.Box(barRect, "");
                var fillRect = new Rect(barRect.x + 2, barRect.y + 2,
                    (barRect.width - 4) * activeSpawner.TrainProgress, barRect.height - 4);
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.cyan, 0, 0);
                GUI.Label(barRect, $" {activeSpawner.CurrentTraining.displayName} 训练中 (队列:{activeSpawner.QueueCount})");
            }
        }

        private bool IsMouseOverPanel()
        {
            if (activeSpawner == null) return false;
            var units = activeSpawner.TrainableUnits;
            var panelW = 200f;
            var btnH = 40f;
            var panelH = 30f + 8f + units.Length * (btnH + 4f) + 16f + 16f;
            var panelX = Screen.width - panelW - 20f;
            var panelY = Screen.height - panelH - 20f;
            var mousePos = Input.mousePosition;
            var guiY = Screen.height - mousePos.y;
            return new Rect(panelX, panelY, panelW, panelH).Contains(new Vector2(mousePos.x, guiY));
        }

        private string GetCostText(UnitData data)
        {
            if (data.trainingCost == null || data.trainingCost.Length == 0) return "";
            var s = "";
            foreach (var c in data.trainingCost)
            {
                if (s.Length > 0) s += " ";
                s += $"{c.amount}{c.type.GetLabel()}";
            }
            return s;
        }
    }
}
