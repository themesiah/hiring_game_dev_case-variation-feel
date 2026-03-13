using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemyView : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int DeadHash = Animator.StringToHash("Dead");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int DamageHash = Animator.StringToHash("Damage");


		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField] private Animator animator;
		[SerializeField] private ParticleSystem damageEffect;
		[SerializeField] private Outline outline;

		private HeroController _heroController;
		private EnemyState? _lastEnemyState = null;

		public Outline Outline => outline;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
		}

		public void OnEnemyStateChanged(EnemyState enemyState)
		{
			// Always update position from the runtime state
			transform.position = enemyState.Position;

			if (_lastEnemyState.HasValue)
			{
				EnemyState previousState = _lastEnemyState.Value;

				animator.SetFloat(SpeedHash, (enemyState.Position - previousState.Position).magnitude / Time.deltaTime);

				if (enemyState.LastAttackTime > previousState.LastAttackTime)
				{
					animator.SetTrigger(AttackHash);
				}
				else if (enemyState.Health < previousState.Health)
				{
					animator.SetTrigger(DamageHash);
					damageEffect.Play();
					outline.enabled = false;
				}

				animator.SetBool(DeadHash, enemyState.IsDead);
			}

			_lastEnemyState = enemyState;
		}

		private void Update()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;

			Vector3 heroPosition = _heroController.CurrentState.Position;
			Vector3 direction = (heroPosition - transform.position).normalized;

			if (direction.sqrMagnitude > 0.01f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}
	}
}