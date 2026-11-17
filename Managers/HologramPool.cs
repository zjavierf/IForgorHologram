using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace IForgorHologram.Managers
{
    public class HologramPool : IDisposable
    {
        private readonly HologramSpawner _spawner;
        private readonly Dictionary<SaberType, GameObject> _pool = new Dictionary<SaberType, GameObject>();

        [Inject]
        public HologramPool(HologramSpawner spawner)
        {
            _spawner = spawner;
        }

        public GameObject GetPooledHologram(SaberType saberType, Saber source)
        {
            if (_pool.TryGetValue(saberType, out GameObject hol) && hol != null)
            {
                _spawner.RefreshHologramAppearance(hol, saberType, source);
                hol.SetActive(true);
                return hol;
            }

            GameObject newHol = _spawner.SpawnHologram(source, saberType);
            if (newHol != null)
            {
                _pool[saberType] = newHol;
            }
            return newHol;
        }

        public void ReturnToPool(GameObject hol)
        {
            if (hol != null)
            {
                hol.SetActive(false);
            }
        }

        public void Dispose()
        {
            foreach (var kvp in _pool)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value);
                }
            }
            _pool.Clear();
        }
    }
}