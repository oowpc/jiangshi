using System.Collections.Generic;
using Jiangshi.Combat;
using Jiangshi.Grid;
using Jiangshi.Pathfinding;
using UnityEngine;

namespace Jiangshi.Units
{
    public sealed class Swordsman : Unit, IMovableUnit, IAttackCommandable
    {
        [SerializeField] private int maxOverlapHits = 64;

        private Vector3? moveTarget;
        private float nextAttackTime;
        private Collider[] overlapHits;
        private UnitVisualAnimator visualAnimator;
        private bool isDead;
        private Damageable lockedAttackTarget;

        private List<Vector3> currentPath;
        private int pathIndex;
        private float nextPathUpdate;
        private Vector3 lastTargetPos;

        private void Awake()
        {
            overlapHits = new Collider[Mathf.Max(1, maxOverlapHits)];
            visualAnimator = GetComponent<UnitVisualAnimator>();
        }

        public void MoveTo(Vector3 position)
        {
            lockedAttackTarget = null;
            moveTarget = position;
            InvalidatePath();
        }

        public void AttackTarget(Damageable target)
        {
            if (!IsValidAttackTarget(target))
            {
                return;
            }

            if (lockedAttackTarget != target)
            {
                lockedAttackTarget = target;
                moveTarget = null;
                nextAttackTime = Mathf.Min(nextAttackTime, Time.time);
                InvalidatePath();
            }
        }

        private void InvalidatePath()
        {
            currentPath = null;
            pathIndex = 0;
            nextPathUpdate = 0f;
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
                currentPath = null;
                visualAnimator?.PlayIdle();
                return;
            }

            var movement = dir.normalized;

            if (UnitCollision.TryGetSeparation(transform.position, this, out var push))
                movement = (movement + push * 2f).normalized;

            visualAnimator?.SetFacing(movement);
            visualAnimator?.PlayWalk();

            if (FollowPath())
            {
                return;
            }

            var nextPos = transform.position + movement * Data.moveSpeed * Time.deltaTime;
            if (TryMove(nextPos, movement)) return;

            moveTarget = null;
            currentPath = null;
            visualAnimator?.PlayIdle();
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
                damageable.TakeDamage(Data.attackDamage);
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

            var targetPos = lockedAttackTarget.transform.position;
            var direction = targetPos - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > Data.attackRange * Data.attackRange)
            {
                var attackMovement = direction.normalized;
                if (UnitCollision.TryGetSeparation(transform.position, this, out var sep))
                    attackMovement = (attackMovement + sep * 2f).normalized;

                visualAnimator?.SetFacing(attackMovement);
                visualAnimator?.PlayWalk();

                UpdatePathIfNeeded(targetPos);

                if (FollowPath())
                {
                    return true;
                }

                var attackNextPos = transform.position + attackMovement * Data.moveSpeed * Time.deltaTime;
                if (TryMove(attackNextPos, attackMovement)) return true;
            }

            visualAnimator?.SetFacing(direction);
            if (Time.time < nextAttackTime)
            {
                visualAnimator?.PlayReload();
                return true;
            }

            visualAnimator?.PlayAttack();
            lockedAttackTarget.TakeDamage(Data.attackDamage);
            nextAttackTime = Time.time + Data.attackInterval;
            return true;
        }

        private void UpdatePathIfNeeded(Vector3 targetPos)
        {
            if (currentPath != null && pathIndex < currentPath.Count && Time.time < nextPathUpdate)
            {
                if ((targetPos - lastTargetPos).sqrMagnitude < 1f)
                {
                    return;
                }
            }

            RecalculatePath(targetPos);
        }

        private void RecalculatePath(Vector3 targetPos)
        {
            lastTargetPos = targetPos;
            nextPathUpdate = Time.time + 2f;
            currentPath = PathPlanner.FindPath(GridManager.Instance, transform.position, targetPos);
            pathIndex = 0;
        }

        private bool FollowPath()
        {
            if (currentPath == null || pathIndex >= currentPath.Count)
            {
                return false;
            }

            var waypoint = currentPath[pathIndex];
            var toWaypoint = waypoint - transform.position;
            toWaypoint.y = 0f;

            if (toWaypoint.sqrMagnitude < 0.1f)
            {
                pathIndex++;
                if (pathIndex >= currentPath.Count)
                {
                    return false;
                }

                waypoint = currentPath[pathIndex];
                toWaypoint = waypoint - transform.position;
                toWaypoint.y = 0f;
            }

            var movement = toWaypoint.normalized;
            visualAnimator?.SetFacing(movement);

            var nextPos = transform.position + movement * Data.moveSpeed * Time.deltaTime;
            if (TryMove(nextPos, movement))
            {
                return true;
            }

            currentPath = null;
            return false;
        }

        private bool TryMove(Vector3 nextPos, Vector3 movement)
        {
            if (GridManager.Instance.IsWalkableAt(nextPos))
            {
                transform.position = nextPos;
                return true;
            }

            if (Mathf.Abs(movement.x) > 0.001f)
            {
                var slideX = new Vector3(movement.x, 0f, 0f).normalized;
                var nextSlide = transform.position + slideX * Data.moveSpeed * Time.deltaTime;
                if (GridManager.Instance.IsWalkableAt(nextSlide))
                {
                    transform.position = nextSlide;
                    return true;
                }
            }

            if (Mathf.Abs(movement.z) > 0.001f)
            {
                var slideZ = new Vector3(0f, 0f, movement.z).normalized;
                var nextSlide = transform.position + slideZ * Data.moveSpeed * Time.deltaTime;
                if (GridManager.Instance.IsWalkableAt(nextSlide))
                {
                    transform.position = nextSlide;
                    return true;
                }
            }

            return false;
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
    }
}
