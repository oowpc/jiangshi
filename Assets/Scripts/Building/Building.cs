using System;
using Jiangshi.Combat;
using Jiangshi.Core;
using Jiangshi.Economy;
using Jiangshi.Grid;
using Jiangshi.Units;
using Jiangshi.Waves;
using UnityEngine;

namespace Jiangshi.Building
{
    [RequireComponent(typeof(Damageable))]
    public sealed class Building : MonoBehaviour
    {
        [SerializeField] private BuildingData data;

        public BuildingData Data => data;
        public GridPosition Origin { get; private set; }
        public Vector2Int OccupiedSize { get; private set; }
        public bool HasOrigin { get; private set; }

        public event Action<Building> Destroyed;

        private Damageable damageable;

        public void Initialize(BuildingData buildingData, GridPosition origin)
        {
            Initialize(buildingData, origin, buildingData != null ? buildingData.size : Vector2Int.one);
        }

        public void Initialize(BuildingData buildingData, GridPosition origin, Vector2Int occupiedSize)
        {
            data = buildingData;
            Origin = origin;
            OccupiedSize = occupiedSize;
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

            if (data.populationCost > 0)
            {
                var rm = FindObjectOfType<ResourceManager>();
                if (rm != null) rm.Deduct(ResourceType.Population, data.populationCost);
            }

            ConfigureProduction();

            if (data.trainableUnits != null && data.trainableUnits.Length > 0)
            {
                var spawner = GetComponent<UnitSpawner>();
                if (spawner == null)
                    spawner = gameObject.AddComponent<UnitSpawner>();
                spawner.SetTrainableUnits(data.trainableUnits);
            }

            var groundShadow = GetComponent<BuildingGroundShadow>();
            if (groundShadow == null)
            {
                groundShadow = gameObject.AddComponent<BuildingGroundShadow>();
            }

            groundShadow.Configure(OccupiedSize);
        }

        public bool CanDemolish()
        {
            return data != null && !data.triggersDefeatOnDestroyed;
        }

        public void Demolish(ResourceManager resourceManager, float refundFraction = 0.5f)
        {
            if (!CanDemolish())
            {
                return;
            }

            RefundBuildCost(resourceManager, refundFraction);
            RefundUpkeep(resourceManager);
            Destroyed?.Invoke(this);
            Destroy(gameObject);
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

            SpawnPopulationZombies();

            if (data != null && data.triggersDefeatOnDestroyed)
            {
                GameManager.Instance?.Lose();
            }

            Destroy(gameObject);
        }

        private void RefundBuildCost(ResourceManager resourceManager, float refundFraction)
        {
            if (resourceManager == null || data == null || data.buildCost == null)
            {
                return;
            }

            foreach (var cost in data.buildCost)
            {
                var refund = Mathf.FloorToInt(cost.amount * Mathf.Clamp01(refundFraction));
                if (refund > 0)
                {
                    resourceManager.Add(cost.type, refund);
                }
            }
        }

        private void RefundUpkeep(ResourceManager resourceManager)
        {
            if (resourceManager == null || data == null)
            {
                return;
            }

            if (data.powerCost > 0)
            {
                resourceManager.Add(ResourceType.Power, data.powerCost);
            }

            if (data.populationCost > 0)
            {
                resourceManager.Add(ResourceType.Population, data.populationCost);
            }
        }

        private void SpawnPopulationZombies()
        {
            if (data == null || data.populationCost <= 0)
            {
                return;
            }

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.SpawnPopulationZombies(transform.position, data.populationCost);
            }
        }

        private void ConfigureProduction()
        {
            if (data.produceAmount > 0 || data.scaleWithContent != Grid.CellContent.None)
            {
                var producer = GetComponent<ResourceProducer>();
                if (producer == null)
                    producer = gameObject.AddComponent<ResourceProducer>();
                producer.Setup(data.produceType, data.produceAmount, data.produceInterval);
                if (data.scaleWithContent != Grid.CellContent.None)
                    producer.SetScaleWithCells(data.scaleWithContent, 3);
            }

            if (data.extraProduction == null)
                return;

            foreach (var production in data.extraProduction)
            {
                if (production.amount <= 0)
                    continue;

                var producer = gameObject.AddComponent<ResourceProducer>();
                producer.Setup(production.type, production.amount, data.produceInterval);
            }
        }
    }
}
