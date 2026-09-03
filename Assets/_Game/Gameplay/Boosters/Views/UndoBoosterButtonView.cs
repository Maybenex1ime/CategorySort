using BoosterModule;
using LogosGame.Features.Currency.Events;
using LogosGame.Features.Currency.UI;
using LogosGame.Features.Gameplay.Boosters.ViewModels;
using LogosSDK.Core.Events;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.Gameplay.Boosters.Views
{
    /// <summary>Nút Undo. Xám khi không có nước nào để lùi, tránh bấm hụt mất lượt đã mua.</summary>
    public class UndoBoosterButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private Image _countBoxBg;
        [SerializeField] private Sprite _addBgSprite;
        [SerializeField] private Sprite _usesBgSprite;

        [Inject] private UndoBoosterViewModel _viewModel;

        private DisposableBag _disposables;

        private void Start()
        {
            if (_viewModel == null) return;

            if (_button != null) _button.onClick.AddListener(OnButtonClicked);

            _viewModel.Count.Subscribe(OnCountChanged).AddTo(ref _disposables);
            _viewModel.IsUsable.Subscribe(_ => UpdateInteractable()).AddTo(ref _disposables);

            OnCountChanged(_viewModel.Count.CurrentValue);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveAllListeners();
            _disposables.Dispose();
        }

        private void OnButtonClicked()
        {
            if (_viewModel == null) return;

            // Hết lượt thì nút đóng vai "mua thêm" — giống hệt các nút booster khác.
            if (_viewModel.Count.CurrentValue <= 0)
            {
                Bus.Global.Fire(new PurchaseRequestedEvent(TransactionIds.ForBooster(BoosterId.Undo)));
                return;
            }

            _viewModel.OnButtonClicked();
        }

        private void OnCountChanged(int count)
        {
            if (_countLabel != null)
                _countLabel.text = count > 0 ? count.ToString() : "+";
            if (_countBoxBg != null)
                _countBoxBg.sprite = count > 0 ? _usesBgSprite : _addBgSprite;
            UpdateInteractable();
        }

        private void UpdateInteractable()
        {
            if (_button == null || _viewModel == null) return;

            // Hết lượt vẫn bấm được — đó là đường vào popup mua. Chỉ xám khi CÒN lượt mà
            // không có nước nào để lùi, vì bấm lúc đó là mất lượt vô ích.
            bool outOfStock = _viewModel.Count.CurrentValue <= 0;
            _button.interactable = outOfStock || _viewModel.IsUsable.CurrentValue;
        }
    }
}
