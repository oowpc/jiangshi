using Jiangshi.Grid;
using UnityEngine;

namespace Jiangshi.Economy
{
    public sealed class ResourceProducer : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private int amountPerTick = 5;
        [SerializeField] private float interval = 5f;
        [SerializeField] private bool scaleWithNearbyCells;
        [SerializeField] private CellContent scanContent;
        [SerializeField] private int scanRadius = 3;

        private ResourceManager resourceManager;
        private GridManager gridManager;
        private float nextTick;

        public void Setup(ResourceType type, int amount, float tickInterval)
        {
            resourceType = type;
            amountPerTick = amount;
            interval = tickInterval > 0f ? tickInterval : 5f;
            scaleWithNearbyCells = false;
            scanContent = CellContent.None;
        }

        public void SetScaleWithCells(CellContent content, int radius)
        {
            scaleWithNearbyCells = true;
            scanContent = content;
            scanRadius = radius;
        }

        private void Start()
        {
            resourceManager = FindObjectOfType<ResourceManager>();
            gridManager = FindObjectOfType<GridManager>();
            nextTick = Time.time + interval;
        }

        private void Update()
        {
            if (resourceManager == null || Time.time < nextTick)
                return;

            var amount = GetProduction();
            if (amount > 0)
                resourceManager.Add(resourceType, amount);
            nextTick = Time.time + interval;
        }

        private int GetProduction()
        {
            if (!scaleWithNearbyCells || gridManager == null)
                return amountPerTick;

            var center = gridManager.WorldToGrid(transform.position);
            var count = 0;
            for (var x = -scanRadius; x <= scanRadius; x++)
            {
                for (var y = -scanRadius; y <= scanRadius; y++)
                {
                    var cell = gridManager.GetCell(new GridPosition(center.X + x, center.Y + y));
                    if (cell != null && cell.Content == scanContent)
                        count++;
                }
            }

            return count * Mathf.Max(1, amountPerTick);
        }
    }
}
