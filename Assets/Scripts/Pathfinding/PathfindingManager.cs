using Jiangshi.Grid;
using UnityEngine;

namespace Jiangshi.Pathfinding
{
    public sealed class PathfindingManager : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;

        public Vector3 GetDirectionToward(Vector3 fromWorld, Vector3 toWorld)
        {
            var direction = toWorld - fromWorld;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
        }

        public void NotifyGridChanged()
        {
            // Flow field or A* invalidation will be added once buildings affect enemy routes.
        }
    }
}

