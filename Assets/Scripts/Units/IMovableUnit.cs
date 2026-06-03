using UnityEngine;
using Jiangshi.Combat;

namespace Jiangshi.Units
{
    public interface IMovableUnit
    {
        void MoveTo(Vector3 position);
    }

    public interface IAttackCommandable
    {
        void AttackTarget(Damageable target);
    }
}
