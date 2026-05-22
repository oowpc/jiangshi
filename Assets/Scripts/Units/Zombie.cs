using Jiangshi.Combat;
using Jiangshi.Pathfinding;
using UnityEngine;

namespace Jiangshi.Units
{
    public sealed class Zombie : Unit
    {
        [SerializeField] private Transform target;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private int attackDamage = 6;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float attackInterval = 1.1f;
        [SerializeField] private LayerMask attackMask = -1;

        private float nextAttackTime;
        private FlowField flowField;

        private void Start()
        {
            flowField = FindObjectOfType<FlowField>();
        }

        private void Update()
        {
            if (TryAttackNearbyTarget())
                return;

            if (flowField == null) return;

            var dir = flowField.GetDirection(transform.position);
            if (dir.sqrMagnitude < 0.01f) return;

            transform.position += dir * GetMoveSpeed() * Time.deltaTime;
        }

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
        }

        private bool TryAttackNearbyTarget()
        {
            var attackTarget = FindAttackTarget();
            if (attackTarget == null) return false;

            if (Time.time >= nextAttackTime)
            {
                attackTarget.TakeDamage(GetAttackDamage());
                nextAttackTime = Time.time + GetAttackInterval();
            }

            return true;
        }

        private Damageable FindAttackTarget()
        {
            var hits = Physics.OverlapSphere(transform.position, GetAttackRange(), attackMask, QueryTriggerInteraction.Ignore);
            Damageable closest = null;
            var closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<Damageable>();
                if (damageable == null || damageable.IsDead) continue;

                var factionMember = damageable.GetComponentInParent<FactionMember>();
                if (factionMember == null || factionMember.Faction != Faction.Player) continue;

                var distance = Vector3.SqrMagnitude(damageable.transform.position - transform.position);
                if (distance < closestDistance)
                {
                    closest = damageable;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private float GetMoveSpeed() => Data != null ? Data.moveSpeed : moveSpeed;
        private int GetAttackDamage() => Data != null ? Data.attackDamage : attackDamage;
        private float GetAttackRange() => Data != null ? Data.attackRange : attackRange;
        private float GetAttackInterval() => Data != null ? Data.attackInterval : attackInterval;
    }
}
