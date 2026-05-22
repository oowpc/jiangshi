using System.Collections.Generic;
using UnityEngine;

namespace Jiangshi.Pools
{
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> inactive = new();

        public ComponentPool(T prefab, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            var instance = inactive.Count > 0 ? inactive.Dequeue() : Object.Instantiate(prefab, parent);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.gameObject.SetActive(false);
            inactive.Enqueue(instance);
        }
    }
}

