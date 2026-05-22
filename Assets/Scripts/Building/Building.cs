using System;
using Jiangshi.Combat;
using Jiangshi.Core;
using Jiangshi.Economy;
using Jiangshi.Grid;
using Jiangshi.Units;
using UnityEngine;

namespace Jiangshi.Building
{
    [RequireComponent(typeof(Damageable))]
    public sealed class Building : MonoBehaviour
    {
        [SerializeField] private BuildingData data;

        public BuildingData Data => data;
        public GridPosition Origin { get; private set; }
        public bool HasOrigin { get; private set; }

        public event Action<Building> Destroyed;

        private Damageable damageable;

        public void Initialize(BuildingData buildingData, GridPosition origin)
        {
            data = buildingData;
            Origin = origin;
            HasOrigin = true;

            damageable = GetComponent<Damageable>();
            damageable.Died -= OnDied;
            damageable.Died += OnDied;
            damageable.SetMaxHealth(data.maxHealth, true);

            if (data.powerCost > 0)
            {
                var rm = FindObjectOfType<ResourceManager>();
                if (rm != null) rm.Deduct(ResourceType.Power, data.powerCost);
            }

            if (data.produceAmount > 0 || data.scaleWithContent != Grid.CellContent.None)
            {
                var producer = GetComponent<ResourceProducer>();
                if (producer == null)
                    producer = gameObject.AddComponent<ResourceProducer>();
                producer.Setup(data.produceType, data.produceAmount, data.produceInterval);
                if (data.scaleWithContent != Grid.CellContent.None)
                    producer.SetScaleWithCells(data.scaleWithContent, 3);
            }

            if (data.trainableUnits != null && data.trainableUnits.Length > 0)
            {
                var spawner = GetComponent<UnitSpawner>();
                if (spawner == null)
                    spawner = gameObject.AddComponent<UnitSpawner>();
                spawner.SetTrainableUnits(data.trainableUnits);
            }
        }

        private void OnDestroy()
        {
            if (damageable != null)
            {
                damageable.Died -= OnDied;
            }
        }

        private void OnDied(Damageable _)
        {
            Destroyed?.Invoke(this);

            if (data != null && data.powerCost > 0)
            {
                var rm = FindObjectOfType<ResourceManager>();
                if (rm != null) rm.Add(ResourceType.Power, data.powerCost);
            }

            if (data != null && data.triggersDefeatOnDestroyed)
            {
                GameManager.Instance?.Lose();
            }

            Destroy(gameObject);
        }
    }
}
