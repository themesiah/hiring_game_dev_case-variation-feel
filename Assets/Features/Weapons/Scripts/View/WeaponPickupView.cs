using UnityEngine;

namespace Game.Weapons
{
	public class WeaponPickupView : MonoBehaviour
	{
		[SerializeField] private Transform _visualModel;
		[SerializeField] private float _rotationSpeed = 180f;

		private WeaponPickupState _currentState;

		public void Initialize(WeaponPickupState state)
		{
			_currentState = state;
			transform.position = state.Position + WeaponPickupConfig.Instance.PickupPositionOffset;
			transform.localScale = Vector3.one * WeaponPickupConfig.Instance.PickupScale;
			gameObject.SetActive(true);
		}
		public void OnPickedUp()
		{
			// Trigger collection effects here (will be added to prefab)
			// For now, this is just a placeholder for future visual feedback
		}

		public void Despawn()
		{
			gameObject.SetActive(false);
		}

		private void Update()
		{
			if (_visualModel != null)
			{
				_visualModel.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
			}
		}
	}
}
