using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Heroes;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesController
	{
		private HeroController _heroController;
		private WeaponsService _weaponsService;

		// Events
		public event Action<EnemyState> OnEnemySpawned;
		public event Action<int, float> OnEnemyRemoved;
		public event Action<EnemyState> OnEnemyUpdated;
		public event Action<int, int> OnEnemyDamaged;
		public event Action<int, int> OnClosestEnemyChanged;

		// State
		private Dictionary<int, EnemyState> _enemies;
		private CancellationTokenSource _cancellationTokenSource;
		private int _nextEnemyId;

		public IReadOnlyDictionary<int, EnemyState> Enemies => _enemies;
		private int _closestEnemyId;

		public int ClosestEnemyId => _closestEnemyId;

		public UniTask<bool> Initialize(HeroController heroController, WeaponsService weaponsService)
		{
			_heroController = heroController;
			_weaponsService = weaponsService;

			_enemies = new Dictionary<int, EnemyState>();
			_nextEnemyId = 0;
			_closestEnemyId = -1;
			_cancellationTokenSource = new CancellationTokenSource();

			SpawnLoop(_cancellationTokenSource.Token).Forget();
			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_enemies.Clear();

			return UniTask.CompletedTask;
		}

		public void ClearAllEnemies()
		{
			List<int> enemyIds = new List<int>(_enemies.Keys);
			foreach (int enemyId in enemyIds)
			{
				RemoveEnemy(enemyId, 0f);
			}
		}

		public void RemoveEnemy(int enemyId, float timeToDisappear)
		{
			if (_enemies.Remove(enemyId))
			{
				OnEnemyRemoved?.Invoke(enemyId, timeToDisappear);
			}
		}

		public void AttackEnemy(EnemyState enemyState, int damage)
		{
			if (!_enemies.ContainsKey(enemyState.Id)) return;

			int newHealth = enemyState.Health - damage;

			Debug.Log($"Attacked enemy id°{enemyState.Id}. Health : {enemyState.Health} -> {newHealth}");

			EnemyState updatedEnemy = new EnemyState(enemyState.Id, enemyState.Position, newHealth, enemyState.Config);
			OnEnemyDamaged?.Invoke(enemyState.Id, damage);
			if (newHealth <= 0)
			{
				Debug.Log($"Enemy id°{enemyState.Id} is dead. Removing it.");
				OnEnemyUpdated?.Invoke(updatedEnemy);
				RemoveEnemy(enemyState.Id, updatedEnemy.Config.TimeToDisappear);
			}
			else
			{
				_enemies[enemyState.Id] = updatedEnemy;
				OnEnemyUpdated?.Invoke(updatedEnemy);
			}
		}

		private async UniTaskVoid SpawnLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(EnemiesConfig.Instance.SpawnInterval), cancellationToken: cancellationToken);
				if (!_heroController.CurrentState.IsDead && _enemies.Count < EnemiesConfig.Instance.MaxEnemies)
				{
					SpawnEnemy();
				}
			}
		}

		private void SpawnEnemy()
		{
			if (EnemiesConfig.Instance.Enemies.Count == 0) return;

			Vector3 playerPosition = _heroController.CurrentState.Position;
			Vector3 spawnPosition = GetRandomPositionAroundPlayer(playerPosition);

			int enemyId = _nextEnemyId++;
			EnemyConfig enemyConfig = EnemiesConfig.Instance.Enemies[0];
			EnemyState newEnemy = new EnemyState(enemyId, spawnPosition, enemyConfig.InitialHealth, enemyConfig);

			_enemies[enemyId] = newEnemy;
			OnEnemySpawned?.Invoke(newEnemy);
		}

		private Vector3 GetRandomPositionAroundPlayer(Vector3 playerPosition)
		{
			float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
			float x = playerPosition.x + EnemiesConfig.Instance.SpawnRadius * Mathf.Cos(angle);
			float z = playerPosition.z + EnemiesConfig.Instance.SpawnRadius * Mathf.Sin(angle);

			return new Vector3(x, playerPosition.y, z);
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (_heroController.CurrentState.IsDead)
				{
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
					continue;
				}

				var attackRange = _weaponsService.CurrentWeapon.Range;
				float closestEnemyDistance = attackRange;
				if (_closestEnemyId != -1 && _enemies.TryGetValue(_closestEnemyId, out EnemyState closestEnemy))
				{
					closestEnemyDistance = Mathf.Min(closestEnemyDistance, Vector3.Distance(closestEnemy.Position, _heroController.CurrentState.Position));
				}

				int lastClosestEnemy = _closestEnemyId;
				_closestEnemyId = -1;
				List<int> enemiesToUpdate = new List<int>(_enemies.Keys);
				for (int i = 0; i < enemiesToUpdate.Count; i++)
				{
					int enemyId = enemiesToUpdate[i];
					if (!_enemies.TryGetValue(enemyId, out EnemyState enemy)) continue;
					if (enemy.IsDead) continue; // Don't update dead enemies

					UpdateEnemy(enemy);
					// Updates which enemy is closest if in range
					var distance = Vector3.Distance(enemy.Position, _heroController.CurrentState.Position);
					if (!enemy.IsDead && distance < closestEnemyDistance)
					{
						_closestEnemyId = enemyId;
						closestEnemyDistance = distance;
					}
				}

				if (lastClosestEnemy != _closestEnemyId)
				{
					// Logic to handle the change in closest enemy
					OnClosestEnemyChanged?.Invoke(lastClosestEnemy, _closestEnemyId);
				}

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void UpdateEnemy(EnemyState enemy)
		{
			Vector3 heroPosition = _heroController.CurrentState.Position;
			float distanceToHero = Vector3.Distance(enemy.Position, heroPosition);

			if (distanceToHero > enemy.Config.AttackRange)
			{
				Vector3 direction = (heroPosition - enemy.Position).normalized;
				Vector3 newPosition = enemy.Position + direction * (enemy.Config.Speed * Time.deltaTime);

				EnemyState updatedEnemy = new EnemyState(enemy.Id, newPosition, enemy.Health, enemy.Config, enemy.LastAttackTime);
				_enemies[enemy.Id] = updatedEnemy;
				OnEnemyUpdated?.Invoke(updatedEnemy);
			}
			else
			{
				if (Time.time - enemy.LastAttackTime >= enemy.Config.AttackCooldown)
				{
					_heroController.TakeHit(enemy.Config.AttackDamage);

					EnemyState updatedEnemy = new EnemyState(enemy.Id, enemy.Position, enemy.Health, enemy.Config, Time.time);
					_enemies[enemy.Id] = updatedEnemy;
					OnEnemyUpdated?.Invoke(updatedEnemy);
				}
			}
		}
	}
}