using System.Collections;
using System.Linq;
using Jiangshi.Grid;
using Jiangshi.Units;
using UnityEngine;

namespace Jiangshi.Waves
{
    public sealed class WaveManager : MonoBehaviour
    {
        private const string DefaultWaveStartClipResource = "Audio/Prototype/ZombieHordeIncoming";
        private const string DefaultBaseMusicResource = "Audio/Prototype/DefenseTheme";
        private const string DefaultHordeMusicResource = "Audio/Prototype/HordeTheme";

        public static WaveManager Instance { get; private set; }

        [SerializeField] private WaveData[] waves;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform defaultTarget;
        [SerializeField] private UnitManager unitManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private int spawnSearchRadius = 8;
        [SerializeField] private bool enableCorridorMission;
        [SerializeField] private int corridorTriggerAfterWave = 2;
        [SerializeField] private GameObject corridorPortalPrefab;
        [SerializeField] private AudioClip waveStartClip;
        [SerializeField, Range(0f, 1f)] private float waveStartVolume = 0.9f;
        [SerializeField] private AudioClip baseMusicClip;
        [SerializeField] private AudioClip hordeMusicClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.55f;

        private float scheduleStartTime;
        private int activeWaveCount;
        private int completedWaveCount;
        private string activeWaveText;
        private WaveData[] orderedWaves;
        private bool corridorTriggered;
        private bool portalSpawned;
        private GameObject portalInstance;
        private int waveSpawnedAlive;
        private AudioSource waveStartAudioSource;
        private AudioSource musicAudioSource;

        public string StatusText { get; private set; } = "无波次";

        private void Awake()
        {
            Instance = this;
            EnsureWaveStartAudioSource();
            EnsureMusicAudioSource();
        }

        private void Start()
        {
            PlayBaseMusic();

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

            if (!corridorTriggered || portalSpawned)
                return;

            if (waveSpawnedAlive == 0)
            {
                SpawnCorridorPortal();
            }
        }

        private float fastForwardTo;

        public void ForceNextWave()
        {
            if (orderedWaves == null) return;
            var elapsed = Time.time - scheduleStartTime;
            foreach (var wave in orderedWaves)
            {
                if (wave.startTime > elapsed)
                {
                    fastForwardTo = wave.startTime;
                    break;
                }
            }
        }

        private IEnumerator RunWave(WaveData wave)
        {
            var startTime = wave.startTime;
            while (Time.time - scheduleStartTime < startTime)
            {
                if (fastForwardTo >= startTime)
                {
                    scheduleStartTime = Time.time - startTime;
                    break;
                }
                yield return null;
            }

            activeWaveCount++;
            activeWaveText = string.IsNullOrWhiteSpace(wave.warningText) ? "波次进行中" : wave.warningText;
            RefreshStatusText();
            PlayHordeMusic();
            PlayWaveStartClip();

            if (wave.enemyGroups != null && wave.enemyGroups.Length > 0)
            {
                var groupIndex = 0;
                var spawnedCount = 0;
                var spawnDirections = Mathf.Max(1, wave.spawnDirections);
                var remainingCounts = new int[wave.enemyGroups.Length];
                var remainingTotal = 0;
                for (var i = 0; i < wave.enemyGroups.Length; i++)
                {
                    remainingCounts[i] = Mathf.Max(0, wave.enemyGroups[i].count);
                    remainingTotal += remainingCounts[i];
                }

                while (spawnedCount < wave.count && remainingTotal > 0)
                {
                    if (remainingCounts[groupIndex] > 0)
                    {
                        SpawnEnemyFromGroup(wave.enemyGroups[groupIndex], spawnDirections);
                        remainingCounts[groupIndex]--;
                        remainingTotal--;
                        spawnedCount++;
                    }

                    groupIndex = (groupIndex + 1) % wave.enemyGroups.Length;
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }
            else
            {
                for (var i = 0; i < wave.count; i++)
                {
                    SpawnEnemy(wave);
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }

            activeWaveCount = Mathf.Max(0, activeWaveCount - 1);
            completedWaveCount++;
            RefreshStatusText();
            UpdateWaveMusicState();

            if (enableCorridorMission && completedWaveCount == corridorTriggerAfterWave)
            {
                corridorTriggered = true;
            }
        }

        private void SpawnEnemy(WaveData wave)
        {
            if (unitManager == null || wave.enemy == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            var unit = unitManager.Spawn(wave.enemy, GetSpawnPosition(spawnPoint.position), spawnPoint.rotation);

            if (unit is Zombie zombie)
            {
                zombie.SetTarget(defaultTarget);
                zombie.SetAggressive();
            }

            TrackWaveUnit(unit);
        }

        private void SpawnEnemyFromGroup(WaveData.EnemyGroup group, int directionCount)
        {
            if (unitManager == null || group.enemy == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            var spawnIndex = Random.Range(0, Mathf.Min(directionCount, spawnPoints.Length));
            var spawnPoint = spawnPoints[spawnIndex];
            var unit = unitManager.Spawn(group.enemy, GetSpawnPosition(spawnPoint.position), spawnPoint.rotation);

            if (unit is Zombie zombie)
            {
                zombie.SetTarget(defaultTarget);
                zombie.SetAggressive();
            }

            TrackWaveUnit(unit);
        }

        private void TrackWaveUnit(Unit unit)
        {
            if (unit == null) return;
            waveSpawnedAlive++;
            unit.Died += OnTrackedWaveUnitDied;
        }

        private void OnTrackedWaveUnitDied(Unit unit)
        {
            if (unit != null)
            {
                unit.Died -= OnTrackedWaveUnitDied;
            }

            waveSpawnedAlive = Mathf.Max(0, waveSpawnedAlive - 1);
            UpdateWaveMusicState();
        }

        private void EnsureWaveStartAudioSource()
        {
            if (waveStartClip == null)
            {
                waveStartClip = Resources.Load<AudioClip>(DefaultWaveStartClipResource);
            }

            if (waveStartAudioSource == null)
            {
                waveStartAudioSource = GetComponent<AudioSource>();
                if (waveStartAudioSource == null)
                {
                    waveStartAudioSource = gameObject.AddComponent<AudioSource>();
                }

                waveStartAudioSource.playOnAwake = false;
                waveStartAudioSource.loop = false;
                waveStartAudioSource.spatialBlend = 0f;
            }
        }

        private void PlayWaveStartClip()
        {
            EnsureWaveStartAudioSource();
            if (waveStartClip == null || waveStartAudioSource == null) return;

            waveStartAudioSource.PlayOneShot(waveStartClip, waveStartVolume);
        }

        public void StopWaveAudio()
        {
            if (musicAudioSource != null)
            {
                musicAudioSource.Stop();
            }

            if (waveStartAudioSource != null)
            {
                waveStartAudioSource.Stop();
            }
        }

        private void EnsureMusicAudioSource()
        {
            if (baseMusicClip == null)
            {
                baseMusicClip = Resources.Load<AudioClip>(DefaultBaseMusicResource);
            }

            if (hordeMusicClip == null)
            {
                hordeMusicClip = Resources.Load<AudioClip>(DefaultHordeMusicResource);
            }

            if (musicAudioSource == null)
            {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
                musicAudioSource.playOnAwake = false;
                musicAudioSource.loop = true;
                musicAudioSource.spatialBlend = 0f;
            }

            musicAudioSource.volume = musicVolume;
        }

        private void PlayBaseMusic()
        {
            PlayMusic(baseMusicClip);
        }

        private void PlayHordeMusic()
        {
            PlayMusic(hordeMusicClip);
        }

        private void PlayMusic(AudioClip clip)
        {
            EnsureMusicAudioSource();
            if (clip == null || musicAudioSource == null)
            {
                return;
            }

            if (musicAudioSource.clip == clip && musicAudioSource.isPlaying)
            {
                musicAudioSource.volume = musicVolume;
                return;
            }

            musicAudioSource.clip = clip;
            musicAudioSource.loop = true;
            musicAudioSource.volume = musicVolume;
            musicAudioSource.Play();
        }

        private void UpdateWaveMusicState()
        {
            if (activeWaveCount > 0 || waveSpawnedAlive > 0)
            {
                PlayHordeMusic();
            }
            else
            {
                PlayBaseMusic();
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

        private Vector3 GetSpawnPosition(Vector3 requestedPosition)
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }

            if (gridManager == null)
            {
                return requestedPosition;
            }

            var requestedGrid = gridManager.WorldToGrid(requestedPosition);
            var clampedGrid = new GridPosition(
                Mathf.Clamp(requestedGrid.X, 0, gridManager.Width - 1),
                Mathf.Clamp(requestedGrid.Y, 0, gridManager.Height - 1));

            if (TryFindWalkableCell(clampedGrid, out var spawnGrid))
            {
                return gridManager.GridToWorld(spawnGrid);
            }

            return gridManager.GridToWorld(clampedGrid);
        }

        private bool TryFindWalkableCell(GridPosition start, out GridPosition result)
        {
            var maxRadius = Mathf.Max(0, spawnSearchRadius);
            for (var radius = 0; radius <= maxRadius; radius++)
            {
                for (var x = start.X - radius; x <= start.X + radius; x++)
                {
                    for (var y = start.Y - radius; y <= start.Y + radius; y++)
                    {
                        if (radius > 0 && x != start.X - radius && x != start.X + radius && y != start.Y - radius && y != start.Y + radius)
                        {
                            continue;
                        }

                        var candidate = new GridPosition(x, y);
                        var cell = gridManager.GetCell(candidate);
                        if (cell != null && cell.IsWalkable)
                        {
                            result = candidate;
                            return true;
                        }
                    }
                }
            }

            result = start;
            return false;
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

        private void SpawnCorridorPortal()
        {
            if (corridorPortalPrefab == null || gridManager == null) return;

            var position = FindRandomWalkablePosition();
            portalInstance = Instantiate(corridorPortalPrefab, position, Quaternion.identity);
            portalSpawned = true;

            var hud = FindObjectOfType<Jiangshi.UI.PrototypeHud>();
            if (hud != null)
            {
                hud.ShowPortalAnnouncement("一个传送门出现");
            }
        }

        private Vector3 FindRandomWalkablePosition()
        {
            for (var attempt = 0; attempt < 200; attempt++)
            {
                var x = Random.Range(0, gridManager.Width);
                var y = Random.Range(0, gridManager.Height);
                var gp = new GridPosition(x, y);
                var cell = gridManager.GetCell(gp);
                if (cell != null && cell.IsWalkable && !cell.IsOccupied)
                {
                    return gridManager.GridToWorld(gp);
                }
            }
            return Vector3.zero;
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
