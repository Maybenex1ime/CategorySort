using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace BoosterModule
{
    public class BoosterSlotView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private BoosterId _id;
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private float _punchScale = 1.2f;

        [Header("References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _button;

        private BoosterSlotViewModel _viewModel;

        private void Start()
        {
            _viewModel = new BoosterSlotViewModel(_id);
            _viewModel.OnCountChanged += UpdateUI;
            
            if (_button != null)
            {
                _button.onClick.AddListener(OnClicked);
            }
            
            // Initial state
            UpdateUI(_viewModel.Count);
        }

        private void OnDestroy()
        {
            if (_viewModel != null)
            {
                _viewModel.OnCountChanged -= UpdateUI;
                _viewModel.Dispose();
            }
        }

        private void OnClicked()
        {
            // Visual feedback
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * (_punchScale - 1f), _animationDuration);
            
            _viewModel.RequestUse();
        }

        private void UpdateUI(int count)
        {
            if (_countText != null)
            {
                _countText.text = count.ToString();
            }
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = count > 0 ? 1f : 0.5f;
            }
        }
    }
}
