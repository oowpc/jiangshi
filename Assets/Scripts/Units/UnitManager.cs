using System.Collections.Generic;
using UnityEngine;

namespace Jiangshi.Units
{
    public sealed class UnitManager : MonoBehaviour
    {
        private readonly List<Unit> units = new();

        public IReadOnlyList<Unit> Units => units;

        public Unit Spawn(UnitData data, Vector3 position, Quaternion rotation)
        {
            if (data == null || data.prefab == null)
            {
                return null;
            }

            var instance = Instantiate(data.prefab, position, rotation);
            var unit = instance.GetComponent<Unit>();
            if (unit != null)
            {
                unit.Initialize(data);
                unit.Died += OnUnitDied;
                units.Add(unit);
            }

            return unit;
        }

        private void OnUnitDied(Unit unit)
        {
            if (unit != null)
            {
                unit.Died -= OnUnitDied;
            }

            units.Remove(unit);
        }
    }
}
