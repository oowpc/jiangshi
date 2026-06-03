using System;
using Jiangshi.Combat;
using UnityEngine;

namespace Jiangshi.Units
{
    [RequireComponent(typeof(Damageable))]
    public class Unit : MonoBehaviour
    {
        [SerializeField] private UnitData data;

        public UnitData Data => data;

        public event Action<Unit> Died;

        public virtual void Initialize(UnitData unitData)
        {
            data = unitData;

            var damageable = GetComponent<Damageable>();
            damageable.Died -= OnDied;
            damageable.SetMaxHealth(data.maxHealth, true);
            damageable.Died += OnDied;
        }

        protected virtual void OnDied(Damageable damageable)
        {
            Died?.Invoke(this);
            Destroy(gameObject, GetDeathDestroyDelay());
        }

        protected virtual float GetDeathDestroyDelay() => 0f;
    }
}
