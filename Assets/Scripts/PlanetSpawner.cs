using Config;
using UnityEngine;
using Mirror;

public class PlanetSpawner : NetworkBehaviour
{
    private PlanetCfg _planetCfg;
    public void Init(PlanetCfg planetCfg)
    {
        _planetCfg = planetCfg;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        for (int i = 0; i < _planetCfg.Planets.Length; i++)
        {
            GameObject planetInstance = Instantiate(_planetCfg.Planets[i].Prefab);
            planetInstance.GetComponent<PlanetOrbit>().Init(_planetCfg.Planets[i].Radius);
            var material = planetInstance.GetComponent<MeshRenderer>().material;
            material.SetColor("_AtmosphereColor", _planetCfg.Planets[i].AtmosphereColor);
            material.SetFloat("_Atmosphere", _planetCfg.Planets[i].Atmosphere);
            planetInstance.GetComponent<MeshRenderer>().material = material;
            NetworkServer.Spawn(planetInstance, NetworkServer.localConnection);
        }
        Destroy(gameObject);
    }
}
