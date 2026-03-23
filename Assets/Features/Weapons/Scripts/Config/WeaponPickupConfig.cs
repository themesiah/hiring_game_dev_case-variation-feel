using UnityEngine;
using Core.ScriptableObjectSingleton;

namespace Game.Weapons
{
	[CreateAssetMenu(fileName = "WeaponPickupConfig", menuName = "Game/Weapon Pickup Config")]
	public class WeaponPickupConfig : ScriptableObjectSingleton<WeaponPickupConfig>
	{
		[SerializeField]
		[Range(1f, 120f)]
		[Tooltip("Seconds before a pickup despawns")]
		private float _pickupLifetime = 30f;

		[SerializeField]
		[Range(0.5f, 10f)]
		[Tooltip("Distance required to collect a pickup")]
		private float _pickupCollectionDistance = 2f;

		[SerializeField]
		[Range(1, 20)]
		[Tooltip("Maximum objects per pickup prefab pool")]
		private int _poolSizePerPrefab = 5;

		[SerializeField]
		[Tooltip("Position offset applied to pickups (to adjust height/visibility)")]
		private Vector3 _pickupPositionOffset = Vector3.zero;

		[SerializeField]
		[Range(0.1f, 5f)]
		[Tooltip("Scale multiplier for pickup visuals")]
		private float _pickupScale = 1f;

		public float PickupLifetime => _pickupLifetime;
		public float PickupCollectionDistance => _pickupCollectionDistance;
		public int PoolSizePerPrefab => _poolSizePerPrefab;
		public Vector3 PickupPositionOffset => _pickupPositionOffset;
		public float PickupScale => _pickupScale;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (_pickupLifetime < 1f) _pickupLifetime = 1f;
			if (_pickupCollectionDistance < 0.5f) _pickupCollectionDistance = 0.5f;
			if (_poolSizePerPrefab < 1) _poolSizePerPrefab = 1;
			if (_pickupScale < 0.1f) _pickupScale = 0.1f;
		}
#endif
	}
}
