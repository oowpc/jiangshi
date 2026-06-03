using Jiangshi.Units;
using UnityEngine;

namespace Jiangshi.Waves
{
    [CreateAssetMenu(menuName = "Jiangshi/Wave Data")]
    public sealed class WaveData : ScriptableObject
    {
        public float startTime = 60f;
        public UnitData enemy;
        public int count = 20;
        public float spawnInterval = 0.25f;
        public string warningText;
        public EnemyGroup[] enemyGroups;
        public int spawnDirections = 1;

        [System.Serializable]
        public struct EnemyGroup
        {
            public UnitData enemy;
            public int count;
        }
    }
}
