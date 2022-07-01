using Config;
using UnityEngine;
using Mirror;

namespace Main
{
    public class SolarSystemNetworkManager : NetworkManager
    {
        [SerializeField] private PlanetCfg _planetCfg;
        [SerializeField] private PlanetSpawner _planetSpawner;
        public override void Awake()
        {
            base.Awake();
            for (int i = 0; i < _planetCfg.Planets.Length; i++)
            {
                spawnPrefabs.Add(_planetCfg.Planets[i].Prefab);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _planetSpawner.Init(_planetCfg);
        }
        /*public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            var spawnTransform = GetStartPosition();
            
            var player = Instantiate(playerPrefab, spawnTransform.position, spawnTransform.rotation);
            //_players.Add(conn.connectionId, player.GetComponent<ShipController>());
            //_players[conn.connectionId].onPlayerCollided += OnPlayerCollided;

            NetworkServer.AddPlayerForConnection(conn, player);

        }*/
    }
}
