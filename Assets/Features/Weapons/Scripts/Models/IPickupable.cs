namespace Game.Weapons
{
    public interface IPickupable
    {
        /// <summary>
        /// Called when the pickup is collected by the player.
        /// Allows different pickup types to handle collection differently.
        /// </summary>
        void OnPickedUp();
    }
}
