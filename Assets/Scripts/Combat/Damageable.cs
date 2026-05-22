using System;
using UnityEngine;

namespace Jiangshi.Combat
{
    public sealed class Damageable : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public event Action<Damageable> Died;
        public event Action<Damageable, int> HealthChanged;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void SetMaxHealth(int value, bool refill)
        {
            maxHealth = Mathf.Max(1, value);
            if (refill)
            {
                CurrentHealth = maxHealth;
                HealthChanged?.Invoke(this, CurrentHealth);
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(this, CurrentHealth);

            if (CurrentHealth == 0)
            {
                Died?.Invoke(this);
            }
        }
    }
}

