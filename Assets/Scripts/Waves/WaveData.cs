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
    }
}

