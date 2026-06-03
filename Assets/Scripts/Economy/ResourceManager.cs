using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jiangshi.Economy
{
    public sealed class ResourceManager : MonoBehaviour
    {
        [SerializeField] private ResourceAmount[] startingResources =
        {
            new ResourceAmount { type = ResourceType.Gold, amount = 200 },
            new ResourceAmount { type = ResourceType.Wood, amount = 50 },
            new ResourceAmount { type = ResourceType.Food, amount = 10 },
            new ResourceAmount { type = ResourceType.Power, amount = 30 }
        };

        private readonly Dictionary<ResourceType, int> resources = new();

        public event Action<ResourceType, int> ResourceChanged;

        private void Awake()
        {
            foreach (var resource in startingResources)
            {
                resources[resource.type] = resource.amount;
            }
        }

        public int Get(ResourceType type)
        {
            return resources.TryGetValue(type, out var amount) ? amount : 0;
        }

        public void Add(ResourceType type, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            resources[type] = Get(type) + amount;
            ResourceChanged?.Invoke(type, resources[type]);
        }

        public void Deduct(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            resources[type] = Get(type) - amount;
            ResourceChanged?.Invoke(type, resources[type]);
        }

        public bool CanAfford(IReadOnlyList<ResourceAmount> costs)
        {
            if (costs == null)
            {
                return true;
            }

            foreach (var cost in costs)
            {
                if (Get(cost.type) < cost.amount)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TrySpend(IReadOnlyList<ResourceAmount> costs)
        {
            if (costs == null)
            {
                return true;
            }

            if (!CanAfford(costs))
            {
                return false;
            }

            foreach (var cost in costs)
            {
                resources[cost.type] = Get(cost.type) - cost.amount;
                ResourceChanged?.Invoke(cost.type, resources[cost.type]);
            }

            return true;
        }
    }
}
