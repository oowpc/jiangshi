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

        [Header("Idle Wander")]
        [SerializeField] private float detectRange = 5f;
        [SerializeField] private float wanderRadius = 2f;
        [SerializeField] private float wanderSpeed = 0.5f;

        private float nextAttackTime;
        private FlowField flowField;
        private bool aggressive;
        private Vector3 wanderTarget;
        private Vector3 spawnPosition;
        private float wanderTimer;

        private void Start()
        {
            flowField = FindObjectOfType<FlowField>();
            spawnPosition = transform.position;
            PickWanderTarget();
        }

        private void Update()
        {
            if (!aggressive)
            {
                // Check if player unit or building is nearby
                if (DetectPlayerNearby())
                {
                    aggressive = true;
                }
                else
                {
                    Wander();
                    return;
                }
            }

            if (TryAttackNearbyTarget())
                return;

            var dir = GetMovementDirection();
            if (dir.sqrMagnitude < 0.01f) return;
            transform.position += dir * GetMoveSpeed() * Time.deltaTime;
        }

        private Vector3 GetMovementDirection()
        {
            if (flowField != null)
            {
                var flowDirection = flowField.GetDirection(transform.position);
                if (flowDirection.sqrMagnitude > 0.01f)
                {
                    return flowDirection.normalized;
                }
            }

            if (target == null)
            {
                return Vector3.zero;
            }

            var directDirection = target.position - transform.position;
            directDirection.y = 0f;
            return directDirection.sqrMagnitude > 0.01f ? directDirection.normalized : Vector3.zero;
        }

        private void Wander()
        {
            var dir = wanderTarget - transform.position;
            if (dir.sqrMagnitude < 0.1f)
            {
                PickWanderTarget();
            }

            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
                PickWanderTarget();

            var move = dir.normalized * wanderSpeed * Time.deltaTime;
            transform.position += move;
        }

        private void PickWanderTarget()
        {
            var offset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = spawnPosition + new Vector3(offset.x, 0f, offset.y);
            wanderTimer = Random.Range(2f, 5f);
        }

        private bool DetectPlayerNearby()
        {
            var hits = Physics.OverlapSphere(transform.position, detectRange, attackMask, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var fm = hit.GetComponentInParent<FactionMember>();
                if (fm != null && fm.Faction == Faction.Player)
                    return true;
            }
            return false;
        }

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
        }

        public void SetAggressive()
        {
            aggressive = true;
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
