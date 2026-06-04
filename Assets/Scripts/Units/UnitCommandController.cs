using System.Collections.Generic;
using Jiangshi.Building;
using Jiangshi.Combat;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Jiangshi.Units
{
    public sealed class UnitCommandController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask groundMask = -1;
        [SerializeField] private float minDragDistance = 10f;

        private readonly List<Unit> selectedUnits = new();
        private PlacementSystem placementSystem;
        private UnitManager unitManager;
        private bool isDragging;
        private Vector2 dragStart;

        private void Start()
        {
            placementSystem = FindObjectOfType<PlacementSystem>();
            unitManager = FindObjectOfType<UnitManager>();
        }

        private void Update()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;

            var placingBuilding = placementSystem != null && placementSystem.SelectedBuilding != null;

            if (!placingBuilding && !EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0))
                {
                    dragStart = Input.mousePosition;
                    isDragging = false;
                }

                if (Input.GetMouseButton(0) && !isDragging)
                {
                    if (Vector2.Distance(Input.mousePosition, dragStart) > minDragDistance)
                        isDragging = true;
                }

                if (Input.GetMouseButtonUp(0))
                {
                    if (isDragging)
                        BoxSelect();
                    else
                        SingleSelect();
                    isDragging = false;
                }
            }

            if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
                TryCommand();
        }

        private void BoxSelect()
        {
            selectedUnits.Clear();
            var rect = GetScreenRect(dragStart, Input.mousePosition);

            foreach (var unit in unitManager.Units)
            {
                if (unit == null) continue;

                var factionMember = unit.GetComponentInParent<FactionMember>();
                if (factionMember == null || factionMember.Faction != Faction.Player) continue;

                var screenPos = worldCamera.WorldToScreenPoint(unit.transform.position);
                if (screenPos.z < 0) continue;

                var guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                if (rect.Contains(guiPos))
                    selectedUnits.Add(unit);
            }
        }

        private void SingleSelect()
        {
            selectedUnits.Clear();
            var ray = worldCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, 500f))
            {
                var unit = hit.collider.GetComponentInParent<Unit>();
                var factionMember = unit != null ? unit.GetComponentInParent<FactionMember>() : null;
                if (factionMember != null && factionMember.Faction == Faction.Player)
                    selectedUnits.Add(unit);
            }
        }

        private void TryCommand()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var ray = worldCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var targetHit, 500f))
            {
                var damageable = targetHit.collider.GetComponentInParent<Damageable>();
                var factionMember = damageable != null ? damageable.GetComponentInParent<FactionMember>() : null;
                if (damageable != null && !damageable.IsDead && factionMember != null && factionMember.Faction == Faction.Enemy)
                {
                    foreach (var unit in selectedUnits)
                    {
                        if (unit is IAttackCommandable attacker)
                            attacker.AttackTarget(damageable);
                    }
                    return;
                }
            }

            if (Physics.Raycast(ray, out var hit, 500f, groundMask))
            {
                var pos = hit.point;
                pos.y = 0f;

                for (var i = 0; i < selectedUnits.Count; i++)
                {
                    if (selectedUnits[i] is IMovableUnit movable)
                    {
                        var offset = selectedUnits.Count > 1
                            ? new Vector3((i % 4 - 1.5f) * 1.5f, 0f, (i / 4) * 1.5f)
                            : Vector3.zero;
                        movable.MoveTo(pos + offset);
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (isDragging)
            {
                var rect = GetScreenRect(dragStart, Input.mousePosition);
                DrawSelectionBox(rect);
            }

            if (worldCamera == null) return;

            foreach (var unit in selectedUnits)
            {
                if (unit == null) continue;

                var screenPos = worldCamera.WorldToScreenPoint(unit.transform.position);
                if (screenPos.z < 0) continue;

                var indicator = new Rect(screenPos.x - 15f, Screen.height - screenPos.y + 10f, 30f, 4f);
                GUI.DrawTexture(indicator, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.cyan, 0, 0);
            }
        }

        private static Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            var min = Vector2.Min(start, end);
            var max = Vector2.Max(start, end);
            return new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
        }

        private static void DrawSelectionBox(Rect rect)
        {
            var fill = new Color(0f, 1f, 0.5f, 0.15f);
            var border = new Color(0f, 1f, 0.5f, 0.5f);

            GUI.color = fill;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, border, 0, 0);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, border, 0, 0);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, border, 0, 0);
            GUI.DrawTexture(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, border, 0, 0);
        }
    }
}
