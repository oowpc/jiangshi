using UnityEngine;
using Jiangshi.Combat;

namespace Jiangshi.Units
{
    public sealed class Archer : Unit
    {
        private Vector3? moveTarget;
        private float nextAttackTime;

        public void MoveTo(Vector3 position)
        {
            moveTarget = position;
        }

        private void Update()
        {
            if (Data == null) return;

            if (TryAttack()) return;

            if (moveTarget == null) return;

            var dir = moveTarget.Value - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f)
            {
                moveTarget = null;
                return;
            }

            transform.position += dir.normalized * Data.moveSpeed * Time.deltaTime;
        }

        private bool TryAttack()
        {
            if (Time.time < nextAttackTime) return false;

            var hits = Physics.OverlapSphere(transform.position, Data.attackRange);
            foreach (var hit in hits)
            {
                var fm = hit.GetComponentInParent<FactionMember>();
                if (fm == null || fm.Faction != Faction.Enemy) continue;

                var dmg = hit.GetComponentInParent<Damageable>();
                if (dmg == null || dmg.IsDead) continue;

                dmg.TakeDamage(Data.attackDamage);
                nextAttackTime = Time.time + Data.attackInterval;
                return true;
            }

            return false;
        }
    }
}
