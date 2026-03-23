using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Weapons
{
	public class WeaponPickupController
	{
		private WeaponsService _weaponsService;
		private Vector3 _lastHeroPosition;

		// State
		private Dictionary<int, WeaponPickupState> _pickups;
		private int _nextPickupId;
		private CancellationTokenSource _cancellationTokenSource;

		// Events
		public event Action<int, Vector3, WeaponConfig> OnPickupSpawned;
		public event Action<int> OnPickupCollected;
		public event Action<int> OnPickupDespawned;

		public UniTask<bool> Initialize(WeaponsService weaponsService)
		{
			_weaponsService = weaponsService;

			_pickups = new Dictionary<int, WeaponPickupState>();
			_nextPickupId = 0;
			_lastHeroPosition = Vector3.zero;
			_cancellationTokenSource = new CancellationTokenSource();

			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		/// <summary>
		/// Called each frame by EntitiesService to provide the current hero position.
		/// </summary>
		public void SetHeroPosition(Vector3 heroPosition)
		{
			_lastHeroPosition = heroPosition;
		}

		public int SpawnPickup(Vector3 position, WeaponConfig weaponConfig)
		{
			int pickupId = _nextPickupId++;
			WeaponPickupState pickupState = new WeaponPickupState(pickupId, position, weaponConfig, Time.time);
			_pickups[pickupId] = pickupState;

			OnPickupSpawned?.Invoke(pickupId, position, weaponConfig);
			return pickupId;
		}

		public void RemovePickup(int pickupId)
		{
			if (_pickups.Remove(pickupId))
			{
				OnPickupDespawned?.Invoke(pickupId);
			}
		}

		public void ClearAllPickups()
		{
			List<int> pickupIds = new List<int>(_pickups.Keys);
			foreach (int pickupId in pickupIds)
			{
				RemovePickup(pickupId);
			}
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_pickups.Clear();

			return UniTask.CompletedTask;
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				// Check for expired pickups
				List<int> expiredPickups = new List<int>();
				foreach (var kvp in _pickups)
				{
					if (kvp.Value.IsExpired(Time.time, WeaponPickupConfig.Instance.PickupLifetime))
					{
						expiredPickups.Add(kvp.Key);
					}
				}

				foreach (int pickupId in expiredPickups)
				{
					RemovePickup(pickupId);
				}

				// Check for collection using last known hero position
				List<int> pickupIds = new List<int>(_pickups.Keys);
				foreach (int pickupId in pickupIds)
				{
					TryCollectPickupAtPosition(pickupId, _lastHeroPosition);
				}

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private bool TryCollectPickupAtPosition(int pickupId, Vector3 position)
		{
			if (!_pickups.TryGetValue(pickupId, out WeaponPickupState pickup))
				return false;

			float distance = Vector3.Distance(pickup.Position, position);
			if (distance > WeaponPickupConfig.Instance.PickupCollectionDistance)
				return false;

			_pickups.Remove(pickupId);
			OnPickupCollected?.Invoke(pickupId);

			// Switch weapon on collection
			if (pickup.WeaponConfig != null)
			{
				_weaponsService.EquipWeapon(pickup.WeaponConfig);
			}

			return true;
		}
	}
}
