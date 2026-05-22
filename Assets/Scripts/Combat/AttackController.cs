using UnityEngine;
using Jiangshi.Pools;

namespace Jiangshi.Combat
{
    public sealed class AttackController : MonoBehaviour
    {
        [SerializeField] private float range = 3f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private Faction targetFaction = Faction.Enemy;
        [SerializeField] private LayerMask targetMask = -1;
        [SerializeField] private Projectile projectilePrefab;

        private float nextAttackTime;
        private Damageable self;
        private ComponentPool<Projectile> projectilePool;

        private void Awake()
        {
            self = GetComponentInParent<Damageable>();
            if (projectilePrefab != null)
                projectilePool = new ComponentPool<Projectile>(projectilePrefab);
        }

        private void Update()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            var target = FindTarget();
            if (target == null)
            {
                return;
            }

            if (projectilePool != null)
            {
                var proj = projectilePool.Get(transform.position, Quaternion.identity);
                proj.Init(target, damage, projectilePool);
            }
            else
            {
                target.TakeDamage(damage);
            }

            nextAttackTime = Time.time + attackInterval;
        }

        private Damageable FindTarget()
        {
            var hits = Physics.OverlapSphere(transform.position, range, targetMask);
            Damageable closest = null;
            var closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<Damageable>();
                if (damageable == null || damageable == self || damageable.IsDead)
                {
                    continue;
                }

                var factionMember = damageable.GetComponentInParent<FactionMember>();
                if (factionMember == null || factionMember.Faction != targetFaction)
                {
                    continue;
                }

                var distance = Vector3.SqrMagnitude(damageable.transform.position - transform.position);
                if (distance < closestDistance)
                {
                    closest = damageable;
                    closestDistance = distance;
                }
            }

            return closest;
        }
    }
}
