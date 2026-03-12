using System.Collections.Generic;
using Game.GamePlay.Entities;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesContainerView : MonoBehaviour
	{
		private EnemiesController _enemiesController;
		private Dictionary<int, EnemyView> _enemyViews;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_enemiesController = ServicesLocator.Instance.GetService<EntitiesService>().EnemiesController;
			_enemyViews = new Dictionary<int, EnemyView>();

			_enemiesController.OnEnemySpawned += OnEnemySpawned;
			_enemiesController.OnEnemyRemoved += OnEnemyRemoved;
			_enemiesController.OnEnemyUpdated += OnEnemyUpdated;
		}

		private void OnEnemySpawned(EnemyState enemyState)
		{
			EnemyView enemyView = Instantiate(enemyState.Config.Prefab, transform);
			enemyView.transform.position = enemyState.Position;
			_enemyViews[enemyState.Id] = enemyView;
		}

		private void OnEnemyRemoved(int enemyId, float timeToDestroy)
		{
			if (_enemyViews.Remove(enemyId, out EnemyView enemyView))
			{
				if (timeToDestroy > 0f)
				{
					// Destroy after 1 second.
					// I feel this is the safest way for fast development because it is managed by unity,
					// and won't suffer by async shenanigans.
					Destroy(enemyView.gameObject, timeToDestroy);
				}
				else
				{
					// If the enemy is removed due to a reset, we can destroy it immediately without waiting for the death animation.
					Destroy(enemyView.gameObject);
				}
			}
		}

		private void OnEnemyUpdated(EnemyState enemyState)
		{
			if (_enemyViews.TryGetValue(enemyState.Id, out EnemyView enemyView))
			{
				enemyView.OnEnemyStateChanged(enemyState);
			}
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemySpawned -= OnEnemySpawned;
				_enemiesController.OnEnemyRemoved -= OnEnemyRemoved;
				_enemiesController.OnEnemyUpdated -= OnEnemyUpdated;
			}
		}
	}
}