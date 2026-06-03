using UnityEngine;
using Jiangshi.Combat;
using Jiangshi.Pools;

namespace Jiangshi.Units
{
    public sealed class Soldier : Unit, IMovableUnit, IAttackCommandable
    {
        [SerializeField] private int maxOverlapHits = 64;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float projectileSpawnForwardOffset = 0.35f;

        private Vector3? moveTarget;
        private float nextAttackTime;
        private Collider[] overlapHits;
        private UnitVisualAnimator visualAnimator;
        private ComponentPool<Projectile> projectilePool;
        private bool isDead;
        private Damageable lockedAttackTarget;

        private void Awake()
        {
            overlapHits = new Collider[Mathf.Max(1, maxOverlapHits)];
            visualAnimator = GetComponent<UnitVisualAnimator>();
            if (projectilePrefab != null)
            {
                projectilePool = new ComponentPool<Projectile>(projectilePrefab);
            }
        }

        public void MoveTo(Vector3 position)
        {
            lockedAttackTarget = null;
            moveTarget = position;
        }

        public void AttackTarget(Damageable target)
        {
            if (!IsValidAttackTarget(target))
            {
                return;
            }

            lockedAttackTarget = target;
            moveTarget = null;
            nextAttackTime = Mathf.Min(nextAttackTime, Time.time);
        }

        private void Update()
        {
            if (isDead || Data == null) return;

            if (TryHandleLockedAttackTarget())
            {
                return;
            }

            if (TryAttack())
            {
                return;
            }

            if (Time.time < nextAttackTime && visualAnimator != null)
            {
                visualAnimator.PlayReload();
                return;
            }

            if (moveTarget == null)
            {
                visualAnimator?.PlayIdle();
                return;
            }

            var dir = moveTarget.Value - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f)
            {
                moveTarget = null;
                visualAnimator?.PlayIdle();
                return;
            }

            var movement = dir.normalized;
            visualAnimator?.SetFacing(movement);
            visualAnimator?.PlayWalk();
            transform.position += movement * Data.moveSpeed * Time.deltaTime;
        }

        protected override float GetDeathDestroyDelay()
        {
            isDead = true;
            return visualAnimator != null ? visualAnimator.PlayDeath() : 0f;
        }

        private bool TryAttack()
        {
            if (Time.time < nextAttackTime)
            {
                return false;
            }

            var hitCount = Physics.OverlapSphereNonAlloc(transform.position, Data.attackRange, overlapHits);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = overlapHits[i];
                var factionMember = hit.GetComponentInParent<FactionMember>();
                if (factionMember == null || factionMember.Faction != Faction.Enemy)
                {
                    continue;
                }

                var damageable = hit.GetComponentInParent<Damageable>();
                if (damageable == null || damageable.IsDead)
                {
                    continue;
                }

                visualAnimator?.SetFacing(damageable.transform.position - transform.position);
                visualAnimator?.PlayAttack();
                FireProjectile(damageable);
                nextAttackTime = Time.time + Data.attackInterval;
                return true;
            }

            return false;
        }

        private bool TryHandleLockedAttackTarget()
        {
            if (lockedAttackTarget == null)
            {
                return false;
            }

            if (!IsValidAttackTarget(lockedAttackTarget))
            {
                lockedAttackTarget = null;
                return false;
            }

            var direction = lockedAttackTarget.transform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > Data.attackRange * Data.attackRange)
            {
                var movement = direction.normalized;
                visualAnimator?.SetFacing(movement);
                visualAnimator?.PlayWalk();
                transform.position += movement * Data.moveSpeed * Time.deltaTime;
                return true;
            }

            visualAnimator?.SetFacing(direction);
            if (Time.time < nextAttackTime)
            {
                visualAnimator?.PlayReload();
                return true;
            }

            visualAnimator?.PlayAttack();
            FireProjectile(lockedAttackTarget);
            nextAttackTime = Time.time + Data.attackInterval;
            return true;
        }

        private static bool IsValidAttackTarget(Damageable target)
        {
            if (target == null || target.IsDead)
            {
                return false;
            }

            var factionMember = target.GetComponentInParent<FactionMember>();
            return factionMember != null && factionMember.Faction == Faction.Enemy;
        }

        private void FireProjectile(Damageable target)
        {
            if (target == null)
            {
                return;
            }

            if (projectilePool == null)
            {
                target.TakeDamage(Data.attackDamage);
                return;
            }

            var direction = target.transform.position - transform.position;
            direction.y = 0f;

            var spawnPosition = transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                spawnPosition += direction.normalized * projectileSpawnForwardOffset;
            }

            var projectile = projectilePool.Get(spawnPosition, Quaternion.identity);
            projectile.Init(target, Data.attackDamage, projectilePool);
        }
    }
}
