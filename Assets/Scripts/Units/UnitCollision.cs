using UnityEngine;

namespace Jiangshi.Units
{
    public static class UnitCollision
    {
        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private const float SeparationRadius = 0.45f;
        private const float MinSeparation = 0.6f;

        public static bool TryGetSeparation(Vector3 position, Unit self, out Vector3 push)
        {
            push = Vector3.zero;
            var hitCount = Physics.OverlapSphereNonAlloc(position, SeparationRadius, OverlapBuffer);

            for (var i = 0; i < hitCount; i++)
            {
                var other = OverlapBuffer[i].GetComponentInParent<Unit>();
                if (other == null || other == self) continue;

                var toOther = position - other.transform.position;
                toOther.y = 0f;
                var dist = toOther.magnitude;

                if (dist < MinSeparation && dist > 0.001f)
                {
                    var strength = 1f - dist / MinSeparation;
                    push += toOther.normalized * strength;
                }
            }

            return push.sqrMagnitude > 0.001f;
        }
    }
}
