using UnityEngine;
using Jiangshi.Combat;

namespace Jiangshi.Units
{
    public sealed class Soldier : Unit, IMovableUnit
    {
        [SerializeField] private int maxOverlapHits = 64;

        private Vector3? moveTarget;
        private float nextAttackTime;
        private Collider[] overlapHits;
        private UnitVisualAnimator visualAnimator;
        private bool isDead;

        private void Awake()
        {
            overlapHits = new Collider[Mathf.Max(1, maxOverlapHits)];
            visualAnimator = GetComponent<UnitVisualAnimator>();
        }

        public void MoveTo(Vector3 position)
        {
            moveTarget = position;
        }

        private void Update()
        {
            if (isDead || Data == null) return;

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
                damageable.TakeDamage(Data.attackDamage);
                nextAttackTime = Time.time + Data.attackInterval;
                return true;
            }

            return false;
        }
    }
}
