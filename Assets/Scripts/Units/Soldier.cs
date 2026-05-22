using UnityEngine;

namespace Jiangshi.Units
{
    public sealed class Soldier : Unit
    {
        private Vector3? moveTarget;

        public void MoveTo(Vector3 position)
        {
            moveTarget = position;
        }

        private void Update()
        {
            if (moveTarget == null || Data == null) return;

            var dir = moveTarget.Value - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f)
            {
                moveTarget = null;
                return;
            }

            transform.position += dir.normalized * Data.moveSpeed * Time.deltaTime;
        }
    }
}

