using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
	public class WeaponsService : IService
	{
		public Type[] GetDependencies() => null;

		public WeaponController CurrentWeapon { get; private set; }

		public event Action<WeaponController> OnWeaponChanged;

		public WeaponPickupController WeaponPickupController { get; private set; }

		public async UniTask<bool> Initialize()
		{
			// Initialize default weapon
			CurrentWeapon = new WeaponController(WeaponsConfig.Instance.Weapons[0]);

			// Initialize weapon pickup controller
			WeaponPickupController = new WeaponPickupController();
			await WeaponPickupController.Initialize(this);

			// Subscribe to hero restart event to clear pickups
			GameEventService gameEventService = ServicesLocator.Instance.GetService<GameEventService>();
			if (gameEventService != null)
			{
				gameEventService.OnHeroRestarted += () =>
				{
					WeaponPickupController.ClearAllPickups();
					// Reset to default weapon
					CurrentWeapon = new WeaponController(WeaponsConfig.Instance.Weapons[0]);
					OnWeaponChanged?.Invoke(CurrentWeapon);
				};
			}
			return true;
		}

		public async UniTask Reset()
		{
			if (WeaponPickupController != null)
			{
				await WeaponPickupController.Reset();
			}
			CurrentWeapon = null;
		}

		public bool SwitchWeapon(string weaponId)
		{
			WeaponConfig newWeapon = WeaponsConfig.Instance.GetWeaponById(weaponId);

			if (newWeapon == null) return false;

			CurrentWeapon = new WeaponController(newWeapon);
			OnWeaponChanged?.Invoke(CurrentWeapon);

			return true;
		}

		public void EquipWeapon(WeaponConfig weaponConfig)
		{
			if (weaponConfig == null)
			{
				Debug.LogWarning("Cannot equip null weapon config");
				return;
			}

			CurrentWeapon = new WeaponController(weaponConfig);
			OnWeaponChanged?.Invoke(CurrentWeapon);
		}

		public WeaponConfig[] GetAvailableWeaponsExcept(WeaponConfig currentWeapon)
		{
			if (WeaponsConfig.Instance.Weapons.Count == 0)
				return new WeaponConfig[0];

			List<WeaponConfig> available = new List<WeaponConfig>();
			foreach (WeaponConfig weapon in WeaponsConfig.Instance.Weapons)
			{
				if (weapon != currentWeapon)
				{
					available.Add(weapon);
				}
			}

			return available.ToArray();
		}
	}
}