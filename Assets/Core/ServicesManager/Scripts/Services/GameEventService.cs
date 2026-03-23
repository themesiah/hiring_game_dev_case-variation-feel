namespace Game.Core
{
	public interface IGameEventService
	{
		event System.Action OnHeroRestarted;
		void TriggerHeroRestart();
	}

	public class GameEventService : IGameEventService
	{
		public event System.Action OnHeroRestarted;

		public void TriggerHeroRestart()
		{
			OnHeroRestarted?.Invoke();
		}
	}
}
