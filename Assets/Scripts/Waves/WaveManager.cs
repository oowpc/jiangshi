using System.Collections;
using System.Linq;
using Jiangshi.Units;
using UnityEngine;

namespace Jiangshi.Waves
{
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private WaveData[] waves;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform defaultTarget;
        [SerializeField] private UnitManager unitManager;

        private float scheduleStartTime;
        private int activeWaveCount;
        private int completedWaveCount;
        private string activeWaveText;
        private WaveData[] orderedWaves;

        public string StatusText { get; private set; } = "无波次";

        private void Start()
        {
            if (waves == null)
            {
                return;
            }

            scheduleStartTime = Time.time;
            orderedWaves = waves
                .Where(wave => wave != null)
                .OrderBy(wave => wave.startTime)
                .ToArray();

            foreach (var wave in orderedWaves)
            {
                StartCoroutine(RunWave(wave));
            }

            RefreshStatusText();
        }

        private void Update()
        {
            RefreshStatusText();
        }

        private IEnumerator RunWave(WaveData wave)
        {
            yield return new WaitForSeconds(wave.startTime);

            activeWaveCount++;
            activeWaveText = string.IsNullOrWhiteSpace(wave.warningText) ? "波次进行中" : wave.warningText;
            RefreshStatusText();

            for (var i = 0; i < wave.count; i++)
            {
                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            activeWaveCount = Mathf.Max(0, activeWaveCount - 1);
            completedWaveCount++;
            RefreshStatusText();
        }

        private void SpawnEnemy(WaveData wave)
        {
            if (unitManager == null || wave.enemy == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            var unit = unitManager.Spawn(wave.enemy, spawnPoint.position, spawnPoint.rotation);

            if (unit is Zombie zombie)
            {
                zombie.SetTarget(defaultTarget);
                zombie.SetAggressive();
            }
        }

        public void SpawnPopulationZombies(Vector3 center, int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (unitManager == null)
            {
                unitManager = FindObjectOfType<UnitManager>();
            }

            var zombieData = GetZombieData();
            if (unitManager == null || zombieData == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var offset = Random.insideUnitCircle * 0.7f;
                var position = center + new Vector3(offset.x, 0f, offset.y);
                var unit = unitManager.Spawn(zombieData, position, Quaternion.identity);
                if (unit is Zombie zombie)
                {
                    zombie.SetTarget(defaultTarget);
                    zombie.SetAggressive();
                }
            }
        }

        private UnitData GetZombieData()
        {
            if (waves == null)
            {
                return null;
            }

            foreach (var wave in waves)
            {
                if (wave == null || wave.enemy == null || wave.enemy.prefab == null)
                {
                    continue;
                }

                if (wave.enemy.prefab.GetComponent<Zombie>() != null)
                {
                    return wave.enemy;
                }
            }

            return null;
        }

        private void RefreshStatusText()
        {
            if (orderedWaves == null || orderedWaves.Length == 0)
            {
                StatusText = "无波次";
                return;
            }

            if (activeWaveCount > 0)
            {
                StatusText = activeWaveText;
                return;
            }

            var elapsed = Time.time - scheduleStartTime;
            foreach (var wave in orderedWaves)
            {
                if (wave.startTime > elapsed)
                {
                    var label = string.IsNullOrWhiteSpace(wave.warningText) ? "下一波" : wave.warningText;
                    StatusText = $"{label} {Mathf.CeilToInt(wave.startTime - elapsed)}秒后";
                    return;
                }
            }

            StatusText = completedWaveCount > 0 ? "波次结束" : "无波次";
        }
    }
}
