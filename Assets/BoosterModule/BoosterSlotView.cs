using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private MotionHandle _punch;

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
            // Visual feedback — kết thúc cú trước (DOKill(complete) cũ) rồi punch lại.
            // DOPunchScale mặc định vibrato 10, elasticity 1; công thức LitMotion khác. DampingRatio
            // chọn để biên độ tắt còn ~5% lúc hết giờ — điểm khởi đầu cho Task 8 so mắt, chưa chốt.
            _punch.TryComplete();
            _punch = LMotion.Punch.Create(transform.localScale, Vector3.one * (_punchScale - 1f), _animationDuration)
                .WithFrequency(10)
                .WithDampingRatio(1.9f)
                .WithCancelOnError()
                .BindToLocalScale(transform)
                .AddTo(gameObject);

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
