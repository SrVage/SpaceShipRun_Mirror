using System.Collections.Generic;
using System.Linq;
using Character;
using Mechanics;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Network
{
    public class CrystalController:NetworkBehaviour
    {
        [SerializeField] Crystal _prefab;
        private List<Crystal> _crystals = new List<Crystal>();
        [SerializeField] private int _generalCount;
        [SerializeField] private float _distance;
        private List<GetCrystals> _ships = new List<GetCrystals>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            for (int i = 0; i < _generalCount; i++)
            {
                GameObject instance = Instantiate(_prefab.gameObject, Random.insideUnitSphere*_distance, Quaternion.identity);
                _crystals.Add(instance.GetComponent<Crystal>());
                instance.GetComponent<Crystal>().OnDestroy += DestroyCrystal;
                NetworkServer.Spawn(instance, NetworkServer.localConnection);
            }
            
            //Destroy(gameObject);
        }

        public void RegisterPlayer(GetCrystals ship) => 
            _ships.Add(ship);

        private void DestroyCrystal(Crystal crystal)
        {
            if (_crystals.Contains(crystal))
            {
                crystal.OnDestroy -= DestroyCrystal;
                _crystals.Remove(crystal);
                NetworkServer.Destroy(crystal.gameObject);
            }
            if (_crystals.Count == 0)
            {
                var winner = _ships.OrderByDescending(e => e.Crystals).FirstOrDefault();
            }
        }
    }
}