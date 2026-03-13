using Codice.Client.Common;
using Core.ServicesManager;
using DG.Tweening;
using UnityEngine;

namespace Game.Weapons
{
	public class WeaponView : MonoBehaviour
	{
		[SerializeField]
		private TrailRenderer _trailRenderer;
		[SerializeField]
		private float _trailDuration;

		WeaponsService _weaponsService;

		private void Start()
		{

			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
			_trailRenderer.enabled = false;
		}

		private void Destroy()
		{
			if (_weaponsService.CurrentWeapon != null)
			{
				_weaponsService.CurrentWeapon.OnAttack -= Attack;
			}
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
			if (_weaponsService.CurrentWeapon != null)
			{
				_weaponsService.CurrentWeapon.OnAttack += Attack;
			}
		}

		public void Attack()
		{
			_trailRenderer.enabled = true;

			DOTween.To(() => _trailRenderer.time, x => _trailRenderer.time = x, _trailDuration, _trailDuration).OnComplete(() => _trailRenderer.enabled = false);
		}
	}
}