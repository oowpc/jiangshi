using UnityEngine;

namespace Jiangshi.Combat
{
    public sealed class FactionMember : MonoBehaviour
    {
        [SerializeField] private Faction faction = Faction.Neutral;

        public Faction Faction => faction;

        public void SetFaction(Faction nextFaction)
        {
            faction = nextFaction;
        }
    }
}

