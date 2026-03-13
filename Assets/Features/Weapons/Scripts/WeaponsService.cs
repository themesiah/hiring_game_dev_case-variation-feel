using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;

namespace Game.Weapons
{
	public class WeaponsService : IService
	{
		public Type[] GetDependencies() => null;

		public WeaponController CurrentWeapon { get; private set; }

		public event Action<WeaponController> OnWeaponChanged;

		public UniTask<bool> Initialize()
		{
			if (WeaponsConfig.Instance.Weapons.Count > 0)
			{
				CurrentWeapon = new WeaponController(WeaponsConfig.Instance.Weapons[0]);
			}

			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			CurrentWeapon = null;
			return UniTask.CompletedTask;
		}

		public bool SwitchWeapon(string weaponId)
		{
			WeaponConfig newWeapon = WeaponsConfig.Instance.GetWeaponById(weaponId);

			if (newWeapon == null) return false;

			CurrentWeapon = new WeaponController(newWeapon);
			OnWeaponChanged?.Invoke(CurrentWeapon);

			return true;
		}
	}
}