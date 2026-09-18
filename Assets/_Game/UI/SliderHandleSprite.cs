using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI
{
    /// Swaps the slider handle's sprite by value: sprites[0] at min, sprites[last] at max.
    [RequireComponent(typeof(Slider))]
    public sealed class SliderHandleSprite : MonoBehaviour
    {
        [SerializeField] private Image _handle;
        [SerializeField] private Sprite[] _sprites;

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            if (_handle == null && _slider.handleRect != null)
                _handle = _slider.handleRect.GetComponent<Image>();
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(Apply);
            Apply(_slider.value);
        }

        private void OnDisable() => _slider.onValueChanged.RemoveListener(Apply);

        private void Apply(float _)
        {
            if (_handle == null || _sprites == null || _sprites.Length == 0) return;
            _handle.sprite = _sprites[Mathf.RoundToInt(_slider.normalizedValue * (_sprites.Length - 1))];
        }
    }
}
