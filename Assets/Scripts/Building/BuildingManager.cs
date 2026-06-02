using System.Collections.Generic;
using Jiangshi.Grid;
using UnityEngine;

namespace Jiangshi.Building
{
    public sealed class BuildingManager : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;

        private readonly List<Building> buildings = new();

        public IReadOnlyList<Building> Buildings => buildings;

        private void Start()
        {
            RegisterExistingBuildings();
        }

        public void Register(Building building)
        {
            if (building != null && !buildings.Contains(building))
            {
                buildings.Add(building);
                building.Destroyed -= OnBuildingDestroyed;
                building.Destroyed += OnBuildingDestroyed;
            }
        }

        public void Unregister(Building building)
        {
            if (building != null)
            {
                building.Destroyed -= OnBuildingDestroyed;
            }

            buildings.Remove(building);
        }

        private void RegisterExistingBuildings()
        {
            if (gridManager == null)
            {
                return;
            }

            foreach (var building in FindObjectsOfType<Building>())
            {
                if (building == null || building.Data == null)
                {
                    continue;
                }

                var origin = gridManager.WorldToGridOrigin(building.transform.position, building.Data.size);
                building.Initialize(building.Data, origin);
                Register(building);
                gridManager.SetOccupied(origin, building.Data.size, true, !building.Data.blocksMovement);
            }
        }

        private void OnBuildingDestroyed(Building building)
        {
            if (building != null && building.HasOrigin && building.Data != null && gridManager != null)
            {
                gridManager.SetOccupied(building.Origin, building.OccupiedSize, false, true);
            }

            Unregister(building);
        }
    }
}
