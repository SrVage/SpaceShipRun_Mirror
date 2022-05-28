using Mirror;
using Network;
using UnityEngine;

namespace Character
{
    public class GetCrystals:NetworkBehaviour
    {
        [SerializeField] [SyncVar] private int _crystals;
        public int Crystals => _crystals;

        public override void OnStartAuthority()
        {
            Object.FindObjectOfType<CrystalController>().RegisterPlayer(this);
        }

        public void AddCrystal()
        {
            if (hasAuthority)
            {
                CmdAddCrystals();
            }
        }

        [Command]
        private void CmdAddCrystals()
        {
            _crystals++;
        }
    }
}