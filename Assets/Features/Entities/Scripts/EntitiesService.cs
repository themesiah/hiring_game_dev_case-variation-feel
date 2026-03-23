using System;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Enemies;
using Game.GamePlay.Heroes;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;

namespace Game.GamePlay.Entities
{
	public class EntitiesService : IService
	{
		public Type[] GetDependencies() => new[] { typeof(JoystickInputService), typeof(WeaponsService) };

		public HeroController HeroController { get; private set; }
		public EnemiesController EnemiesController { get; private set; }

		public async UniTask<bool> Initialize()
		{
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			WeaponsService weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();

			HeroController = new HeroController();
			EnemiesController = new EnemiesController();

			await HeroController.Initialize(EnemiesController, joystickInputService, weaponsService);
			await EnemiesController.Initialize(HeroController, weaponsService);

			// Hook up weapon drops from enemies to weapons service
			EnemiesController.OnWeaponDropped += (position, weaponConfig) =>
			{
				weaponsService.WeaponPickupController.SpawnPickup(position, weaponConfig);
			};

			// Subscribe to hero restart to trigger game event and provide hero position
			HeroController.OnHeroRestarted += () =>
			{
				GameEventService gameEventService = ServicesLocator.Instance.GetService<GameEventService>();
				if (gameEventService != null)
				{
					gameEventService.TriggerHeroRestart();
				}
			};          // Provide hero position to weapon pickup controller each frame (in update pattern)
			HeroController.OnStateChanged += (heroState) =>
			{
				if (weaponsService.WeaponPickupController != null)
				{
					weaponsService.WeaponPickupController.SetHeroPosition(heroState.Position);
				}
			};

			return true;
		}
		public UniTask Reset() => default;
	}
}