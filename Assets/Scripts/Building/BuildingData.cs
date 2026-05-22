using Jiangshi.Economy;
using Jiangshi.Grid;
using Jiangshi.Units;
using UnityEngine;

namespace Jiangshi.Building
{
    [CreateAssetMenu(menuName = "Jiangshi/Building Data")]
    public sealed class BuildingData : ScriptableObject
    {
        public string displayName;
        public GameObject prefab;
        public Vector2Int size = Vector2Int.one;
        public int maxHealth = 100;
        public bool blocksMovement = true;
        public bool triggersDefeatOnDestroyed;
        public ResourceAmount[] buildCost;

        [Header("Upkeep")]
        public int powerCost;

        [Header("Production")]
        public ResourceType produceType;
        public int produceAmount;
        public float produceInterval;
        public CellContent scaleWithContent;

        [Header("Unit Training")]
        public UnitData[] trainableUnits;
    }
}
