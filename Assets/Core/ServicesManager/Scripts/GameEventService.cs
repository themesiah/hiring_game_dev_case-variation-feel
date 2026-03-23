namespace Core.ServicesManager
{
	public class GameEventService : IService
	{
		public event System.Action OnHeroRestarted;

		public System.Type[] GetDependencies() => null;

		public Cysharp.Threading.Tasks.UniTask<bool> Initialize()
		{
			return Cysharp.Threading.Tasks.UniTask.FromResult(true);
		}

		public Cysharp.Threading.Tasks.UniTask Reset()
		{
			return Cysharp.Threading.Tasks.UniTask.CompletedTask;
		}

		public void TriggerHeroRestart()
		{
			OnHeroRestarted?.Invoke();
		}
	}
}
