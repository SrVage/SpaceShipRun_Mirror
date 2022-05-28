using System;
using Character;
using UnityEngine;

namespace Mechanics
{
    public class Crystal:MonoBehaviour
    {
        public event Action<Crystal> OnDestroy;
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<GetCrystals>(out var ship))
            {
                ship.AddCrystal();
            }
            OnDestroy?.Invoke(this);
        }
    }
}