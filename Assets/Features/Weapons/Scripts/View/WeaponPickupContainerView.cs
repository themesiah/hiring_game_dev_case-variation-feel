using System.Collections.Generic;
using Core.ServicesManager;
using Game.Weapons;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Weapons
{
    public class WeaponPickupContainerView : MonoBehaviour
    {
        [SerializeField] private List<WeaponPickupView> _pickupPrefabs = new List<WeaponPickupView>();

        private WeaponPickupController _pickupController;
        private Dictionary<WeaponPickupView, ObjectPool<WeaponPickupView>> _pickupPools;
        private Dictionary<int, WeaponPickupView> _activePickups;

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            WeaponsService weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
            _pickupController = weaponsService.WeaponPickupController;

            _pickupPools = new Dictionary<WeaponPickupView, ObjectPool<WeaponPickupView>>();
            _activePickups = new Dictionary<int, WeaponPickupView>();

            // Initialize pools for each prefab
            foreach (WeaponPickupView prefab in _pickupPrefabs)
            {
                var pool = new ObjectPool<WeaponPickupView>(
                    createFunc: () => Instantiate(prefab, transform),
                    actionOnGet: view => view.gameObject.SetActive(true),
                    actionOnRelease: view => view.gameObject.SetActive(false),
                    actionOnDestroy: view => Destroy(view.gameObject),
                    collectionCheck: false,
                    defaultCapacity: 1,
                    maxSize: WeaponPickupConfig.Instance.PoolSizePerPrefab
                );
                _pickupPools[prefab] = pool;
            }

            // Subscribe to pickup events
            _pickupController.OnPickupSpawned += OnPickupSpawned;
            _pickupController.OnPickupDespawned += OnPickupDespawned;
            _pickupController.OnPickupCollected += OnPickupCollected;
        }

        private void OnPickupSpawned(int pickupId, Vector3 position, WeaponConfig weaponConfig)
        {
            if (_pickupPrefabs.Count == 0)
            {
                Debug.LogWarning("No pickup prefabs assigned to WeaponPickupContainerView");
                return;
            }

            // Use first prefab for now (could be extended to select prefab by weapon type)
            WeaponPickupView prefab = _pickupPrefabs[0];

            if (!_pickupPools.TryGetValue(prefab, out ObjectPool<WeaponPickupView> pool))
            {
                Debug.LogError("Pool not found for prefab: " + prefab.name);
                return;
            }

            WeaponPickupView pickupView = pool.Get();
            WeaponPickupState state = new WeaponPickupState(pickupId, position, weaponConfig, Time.time);
            pickupView.Initialize(state);

            _activePickups[pickupId] = pickupView;
        }

        private void OnPickupDespawned(int pickupId)
        {
            if (_activePickups.TryGetValue(pickupId, out WeaponPickupView pickupView))
            {
                pickupView.Despawn();
                _activePickups.Remove(pickupId);

                // Return to pool
                WeaponPickupView prefab = _pickupPrefabs[0]; // Match the prefab used in OnPickupSpawned
                if (_pickupPools.TryGetValue(prefab, out ObjectPool<WeaponPickupView> pool))
                {
                    pool.Release(pickupView);
                }
            }
        }

        private void OnPickupCollected(int pickupId)
        {
            if (_activePickups.TryGetValue(pickupId, out WeaponPickupView pickupView))
            {
                pickupView.OnPickedUp();
                _activePickups.Remove(pickupId);

                // Return to pool
                WeaponPickupView prefab = _pickupPrefabs[0]; // Match the prefab used in OnPickupSpawned
                if (_pickupPools.TryGetValue(prefab, out ObjectPool<WeaponPickupView> pool))
                {
                    pool.Release(pickupView);
                }
            }
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;

            if (_pickupController != null)
            {
                _pickupController.OnPickupSpawned -= OnPickupSpawned;
                _pickupController.OnPickupDespawned -= OnPickupDespawned;
                _pickupController.OnPickupCollected -= OnPickupCollected;
            }
        }
    }
}
