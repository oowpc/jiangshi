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

        private Unit selectedUnit;
        private PlacementSystem placementSystem;

        private void Start()
        {
            placementSystem = FindObjectOfType<PlacementSystem>();
        }

        private void Update()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;

            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                if (placementSystem == null || placementSystem.SelectedBuilding == null)
                    TrySelect();
            }

            if (Input.GetMouseButtonDown(1) && selectedUnit != null)
                TryCommand();
        }

        private void TrySelect()
        {
            var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f))
            {
                var unit = hit.collider.GetComponentInParent<Unit>();
                var factionMember = unit != null ? unit.GetComponentInParent<FactionMember>() : null;
                selectedUnit = factionMember != null && factionMember.Faction == Faction.Player
                    ? unit
                    : null;
            }
            else
            {
                selectedUnit = null;
            }
        }

        private void TryCommand()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var ray = worldCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var targetHit, 500f))
            {
                var damageable = targetHit.collider.GetComponentInParent<Damageable>();
                var factionMember = damageable != null ? damageable.GetComponentInParent<FactionMember>() : null;
                if (damageable != null && !damageable.IsDead && factionMember != null && factionMember.Faction == Faction.Enemy)
                {
                    if (selectedUnit is IAttackCommandable attacker)
                    {
                        attacker.AttackTarget(damageable);
                        return;
                    }
                }
            }

            if (Physics.Raycast(ray, out var hit, 500f, groundMask))
            {
                var pos = hit.point;
                pos.y = 0f;
                if (selectedUnit is IMovableUnit movable)
                {
                    movable.MoveTo(pos);
                }
            }
        }

        private void OnGUI()
        {
            if (selectedUnit == null || worldCamera == null) return;

            var screenPos = worldCamera.WorldToScreenPoint(selectedUnit.transform.position);
            if (screenPos.z < 0) return;

            var rect = new Rect(screenPos.x - 15f, Screen.height - screenPos.y + 10f, 30f, 4f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.cyan, 0, 0);
        }
    }
}
