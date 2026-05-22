using Jiangshi.Economy;
using UnityEngine;

namespace Jiangshi.Units
{
    [CreateAssetMenu(menuName = "Jiangshi/Unit Data")]
    public sealed class UnitData : ScriptableObject
    {
        public string displayName;
        public GameObject prefab;
        public int maxHealth = 50;
        public float moveSpeed = 3f;
        public int attackDamage = 8;
        public float attackRange = 3f;
        public float attackInterval = 1f;
        public ResourceAmount[] trainingCost;
        public float trainingTime = 5f;
    }
}

