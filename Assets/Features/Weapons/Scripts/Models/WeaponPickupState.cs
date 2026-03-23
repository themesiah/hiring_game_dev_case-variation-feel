using Game.Weapons;
using UnityEngine;

namespace Game.Weapons
{
    public struct WeaponPickupState
    {
        public int Id { get; }
        public Vector3 Position { get; }
        public WeaponConfig WeaponConfig { get; }
        public float SpawnTime { get; }

        public bool IsExpired(float currentTime, float lifetime)
            => currentTime - SpawnTime >= lifetime;

        public WeaponPickupState(int id, Vector3 position, WeaponConfig weaponConfig, float spawnTime)
        {
            Id = id;
            Position = position;
            WeaponConfig = weaponConfig;
            SpawnTime = spawnTime;
        }
    }
}
