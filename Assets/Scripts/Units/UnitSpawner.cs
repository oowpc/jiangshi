using System.Collections.Generic;
using Jiangshi.Economy;
using UnityEngine;

namespace Jiangshi.Units
{
    public sealed class UnitSpawner : MonoBehaviour
    {
        [SerializeField] private UnitData[] trainableUnits;

        private UnitManager unitManager;
        private ResourceManager resourceManager;
        private readonly Queue<UnitData> queue = new();
        private float trainEndTime;
        private UnitData currentTraining;

        public UnitData[] TrainableUnits => trainableUnits;
        public UnitData CurrentTraining => currentTraining;
        public int QueueCount => queue.Count;
        public float TrainProgress => currentTraining == null ? 0f :
            1f - Mathf.Clamp01((trainEndTime - Time.time) / currentTraining.trainingTime);

        public void SetTrainableUnits(UnitData[] units)
        {
            trainableUnits = units;
        }

        private void Start()
        {
            unitManager = FindObjectOfType<UnitManager>();
            resourceManager = FindObjectOfType<ResourceManager>();
        }

        private void Update()
        {
            if (currentTraining != null)
            {
                if (Time.time >= trainEndTime)
                {
                    SpawnUnit(currentTraining);
                    currentTraining = null;
                    TryStartNext();
                }
            }
        }

        public bool TryTrain(UnitData data)
        {
            if (data == null || resourceManager == null) return false;
            if (!resourceManager.TrySpend(data.trainingCost)) return false;

            queue.Enqueue(data);
            if (currentTraining == null)
                TryStartNext();
            return true;
        }

        private void TryStartNext()
        {
            if (queue.Count == 0) return;
            currentTraining = queue.Dequeue();
            trainEndTime = Time.time + currentTraining.trainingTime;
        }

        private void SpawnUnit(UnitData data)
        {
            if (unitManager == null) return;
            var pos = transform.position + Vector3.right * 1.5f;
            unitManager.Spawn(data, pos, Quaternion.identity);
        }
    }
}
