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
        [SerializeField] private int maxOverlapHits = 64;

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
        private Collider[] overlapHits;

        private void Start()
        {
            flowField = FlowField.Instance;
            overlapHits = new Collider[Mathf.Max(1, maxOverlapHits)];
            spawnPosition = transform.position;
            PickWanderTarget();
        }

        private void Update()
        {
            if (!aggressive)
            {
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

        public override void Initialize(UnitData unitData)
        {
            base.Initialize(unitData);
        }

        private Vector3 GetMovementDirection()
        {
            if (flowField == null)
            {
                flowField = FlowField.Instance;
            }

            if (flowField != null)
            {
                if (flowField.TryGetDirection(transform.position, out var flowDirection))
                {
                    return flowDirection.normalized;
                }
            }

            return Vector3.zero;
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
            var hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectRange, overlapHits, attackMask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = overlapHits[i];
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
            var hitCount = Physics.OverlapSphereNonAlloc(transform.position, GetAttackRange(), overlapHits, attackMask, QueryTriggerInteraction.Ignore);
            Damageable closest = null;
            var closestDistance = float.MaxValue;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = overlapHits[i];
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
