using Game.GamePlay.Entities;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public class HeroView : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int DeadHash = Animator.StringToHash("Dead");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int DamageHash = Animator.StringToHash("Damage");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField] private Transform weaponSlot;
		[SerializeField] private ParticleSystem damageEffect;

		private JoystickInputService _joystickInputService;
		private HeroController _heroController;
		private WeaponsService _weaponsService;
		private Vector2 _currentMovementInput;
		private WeaponView _currentWeaponView;

		private HeroState? _lastHeroState = null;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();

			_joystickInputService.OnStateChanged += OnJoystickStateChanged;
			_heroController.OnStateChanged += OnHeroStateChanged;
			_weaponsService.OnWeaponChanged += OnWeaponChanged;

			OnJoystickStateChanged(_joystickInputService.CurrentState);
			OnHeroStateChanged(_heroController.CurrentState);
			SpawnCurrentWeapon();

			_lastHeroState = _heroController.CurrentState;
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_joystickInputService != null)
			{
				_joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			}
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
			}
			if (_weaponsService != null)
			{
				_weaponsService.OnWeaponChanged -= OnWeaponChanged;
			}
			if (_currentWeaponView != null)
			{
				Destroy(_currentWeaponView.gameObject);
			}
		}

		private void OnJoystickStateChanged(JoystickState state)
		{
			_currentMovementInput = state.IsActive ? state.MovementVector : Vector2.zero;
			UpdateAnimator();
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			transform.position = heroState.Position;

			// Using the last hero state to determine animation triggers
			// Setting them here, because even if UpdateAnimator is called like that,
			// it actually updates the animator only for the movement. Might need some refactor.
			if (_lastHeroState.HasValue)
			{
				HeroState previousState = _lastHeroState.Value;

				if (heroState.LastAttackTime > previousState.LastAttackTime)
				{
					animator.SetTrigger(AttackHash);
					// Set rotation to where is attacking
					Vector3 attackDirection = -(heroState.AttackToPosition - heroState.Position).normalized;
					Quaternion targetRotation = Quaternion.LookRotation(attackDirection);
					transform.rotation = targetRotation;
				}
				else if (heroState.Health < previousState.Health)
				{
					animator.SetTrigger(DamageHash);
					if (heroState.IsDead)
					{
						// If the hero is dead, we can reset the triggers immediately to avoid any unwanted animation states.
						ResetAnimatorTriggers();
					}
					damageEffect.Play();
				}


				animator.SetBool("Dead", heroState.IsDead);
			}

			_lastHeroState = heroState;
		}

		private void Update()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;
			if (_currentMovementInput.sqrMagnitude <= 0.01f) return;

			Vector3 movement = new Vector3(-_currentMovementInput.x, 0f, -_currentMovementInput.y);
			Quaternion targetRotation = Quaternion.LookRotation(-movement);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
		}

		private void UpdateAnimator()
		{
			if (animator == null) return;

			// Commenting this because death state will be exclusive with other states.
			// Speed parameter will be irrelevant.
			// if (_heroController is { CurrentState: { IsDead: true } })
			// {
			// 	animator.SetFloat(SpeedHash, 0f);
			// 	return;
			// }

			float speed = _currentMovementInput.magnitude;
			animator.SetFloat(SpeedHash, speed);
		}

		private void OnWeaponChanged(WeaponController newWeapon)
		{
			if (_currentWeaponView != null)
			{
				Destroy(_currentWeaponView.gameObject);
				_currentWeaponView = null;
			}

			SpawnCurrentWeapon();
		}

		private void SpawnCurrentWeapon()
		{
			if (_weaponsService.CurrentWeapon == null) return;

			Transform parent = weaponSlot != null ? weaponSlot : transform;
			_currentWeaponView = Instantiate(_weaponsService.CurrentWeapon.Config.Prefab, parent);
			_currentWeaponView.transform.localPosition = Vector3.zero;
			_currentWeaponView.transform.localRotation = Quaternion.identity;
		}


		private void ResetAnimatorTriggers()
		{
			animator.ResetTrigger(AttackHash);
			animator.ResetTrigger(DamageHash);
		}
	}
}