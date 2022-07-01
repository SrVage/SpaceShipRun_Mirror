using System;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(menuName = "Cfg/PlanetCfg")]
    public class PlanetCfg:ScriptableObject
    {
        [Serializable]
        public class Planet
        {
            public float Radius;
            public GameObject Prefab;
            [Range(0,1)] public float Atmosphere;
            public Color AtmosphereColor;
        }

        public Planet[] Planets;
    }
}