using UnityEngine;
using Jiangshi.Pools;

namespace Jiangshi.Combat
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;

        private Damageable target;
        private int damage;
        private ComponentPool<Projectile> pool;
        private Vector3 lastTargetPos;

        public void Init(Damageable target, int damage, ComponentPool<Projectile> pool)
        {
            this.target = target;
            this.damage = damage;
            this.pool = pool;
            lastTargetPos = target != null ? target.transform.position : transform.position + transform.forward;
        }

        private void Update()
        {
            if (target != null && !target.IsDead)
                lastTargetPos = target.transform.position;

            var step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, lastTargetPos, step);

            if (Vector3.SqrMagnitude(transform.position - lastTargetPos) < 0.01f)
            {
                if (target != null && !target.IsDead)
                    target.TakeDamage(damage);

                pool.Release(this);
            }
        }
    }
}
