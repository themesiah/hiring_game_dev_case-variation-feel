using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using TMPro;

using Core.ServicesManager;
using Game.GamePlay.Heroes;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;

namespace Game.GamePlay
{
    /**
    * @brief This class is responsible for displaying damage text above characters (heroes/enemies) when they take damage.
    * It manages the pooling of damage text objects.
    * The damage text objects manage their own release when finishing animating.
    */
    public class DamageTextsView : MonoBehaviour
    {
        [SerializeField]
        private Transform _damageTextParent;
        [SerializeField]
        private DamageText _damageTextPrefab;
        [SerializeField]
        private Color _heroDamageColor = Color.red;
        [SerializeField]
        private Color _enemyDamageColor = Color.white;

        private ObjectPool<DamageText> _damageTextPool;
        private HeroController _heroController;
        private EnemiesController _enemiesController;

        private HeroState? _lastHeroState = null;

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _damageTextPool = new ObjectPool<DamageText>(
                createFunc: () =>
                {
                    DamageText damageText = Instantiate(_damageTextPrefab, _damageTextParent);
                    return damageText;
                },
                actionOnGet: (text) => text.gameObject.SetActive(true),
                actionOnRelease: (text) => text.gameObject.SetActive(false),
                actionOnDestroy: (text) => Destroy(text.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 20
            );
            _heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
            _enemiesController = ServicesLocator.Instance.GetService<EntitiesService>().EnemiesController;

            _heroController.OnHeroDamaged += OnHeroDamaged;
            _enemiesController.OnEnemyDamaged += OnEnemyDamaged;

            _lastHeroState = _heroController.CurrentState;
        }

        private void OnHeroDamaged(int damage)
        {
            // Show damage text above the hero
            ShowDamageText(_heroController.CurrentState.Position, damage, _heroDamageColor);
        }

        private void OnEnemyDamaged(int enemyId, int damage)
        {
            // Show damage text above the enemy
            if (_enemiesController.Enemies.TryGetValue(enemyId, out EnemyState enemyState))
            {
                ShowDamageText(enemyState.Position, damage, _enemyDamageColor);
            }
        }

        private void ShowDamageText(Vector3 position, int damage, Color color)
        {
            DamageText text = _damageTextPool.Get();
            Camera c = Camera.main;
            Vector3 screenPosition = c.WorldToScreenPoint(position);
            ((RectTransform)text.transform).anchoredPosition = screenPosition;
            text.Initialize(damage.ToString(), color, _damageTextPool.Release);
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
            if (_heroController != null)
            {
                _heroController.OnHeroDamaged -= OnHeroDamaged;
            }
            if (_enemiesController != null)
            {
                _enemiesController.OnEnemyDamaged -= OnEnemyDamaged;
            }
        }
    }
}
