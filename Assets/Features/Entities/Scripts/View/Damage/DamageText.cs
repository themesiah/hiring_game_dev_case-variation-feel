using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Game.GamePlay
{
    public class DamageText : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _text;
        [SerializeField]
        private float _duration = 1f;
        [SerializeField]
        private float _yMovementDelta = 1f;

        private System.Action<DamageText> _onRelease;

        public void Initialize(string damageAmount, Color color, System.Action<DamageText> onRelease)
        {
            _text.text = damageAmount;
            _text.color = color;
            _text.alpha = 1f;
            _onRelease = onRelease;

            // Animate the damage text
            AnimateDamageText();
        }

        private void AnimateDamageText()
        {
            ((RectTransform)transform).DOAnchorPosY(((RectTransform)transform).anchoredPosition.y + _yMovementDelta, _duration);
            _text.DOFade(0, _duration).OnComplete(() => _onRelease?.Invoke(this));
        }
    }
}
