using System.Collections.Generic;
using Characters;
using UnityEngine;
using Mirror;

namespace Main
{
    public class SolarSystemNetworkManager : NetworkManager
    {
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
