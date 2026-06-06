using Jiangshi.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jiangshi.Units
{
    public sealed class CorridorPortal : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var unit = other.GetComponentInParent<Unit>();
            if (unit == null) return;

            var factionMember = unit.GetComponentInParent<FactionMember>();
            if (factionMember == null || factionMember.Faction != Faction.Player) return;

            Time.timeScale = 0f;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Additive);
        }
    }
}
