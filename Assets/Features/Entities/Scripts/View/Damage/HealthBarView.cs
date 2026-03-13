using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using Core.ServicesManager;
using Game.GamePlay.Heroes;
using Game.GamePlay.Entities;
using UnityEngine.UI;
using TMPro;

namespace Game.GamePlay
{
    /**
    * @brief Health Bar UI for the Hero. It updates each time the hero receives damage, at the start of the game and each time
    * the hero gets resetted. It has a text inside with the actual health value.
    */
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField]
        private GameObject _heroHealthPanel;
        [SerializeField]
        private Slider _healthBarSlider;
        [SerializeField]
        private TMP_Text _healthText;

        private HeroController _heroController;

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;

            _heroController.OnHeroDamaged += OnHeroDamaged;
            _heroController.OnHeroRestarted += OnHeroRestarted;
            OnHeroRestarted();
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
            if (_heroController != null)
            {
                _heroController.OnHeroDamaged -= OnHeroDamaged;
                _heroController.OnHeroRestarted -= OnHeroRestarted;
            }
        }

        private void OnHeroDamaged(int damage)
        {
            // Update the health bar UI and text to reflect the new health value
            _healthBarSlider.value = (float)_heroController.CurrentState.Health / HeroConfig.Instance.InitialHealth;
            _healthText.text = $"{_heroController.CurrentState.Health} / {HeroConfig.Instance.InitialHealth}";

            if (_heroController.CurrentState.IsDead)
            {
                _heroHealthPanel.SetActive(false);
            }
        }

        private void OnHeroRestarted()
        {
            // Reset the health bar UI and text when the hero is restarted
            _healthBarSlider.value = 1f;
            _healthText.text = $"{HeroConfig.Instance.InitialHealth} / {HeroConfig.Instance.InitialHealth}";
            _heroHealthPanel.SetActive(true);
        }

    }
}
