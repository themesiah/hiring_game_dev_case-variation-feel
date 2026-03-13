using System;

namespace Game.Weapons
{
    public class WeaponController
    {
        private WeaponConfig _config;
        public WeaponConfig Config => _config;

        public Action OnAttack;

        public WeaponController(WeaponConfig config)
        {
            _config = config;
        }

        public void Attack()
        {
            OnAttack?.Invoke();
        }
    }
}