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
    public class HandBoosterButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private GameObject _countLabelGO;
        [SerializeField] private GameObject _cancelIconGO;
        [SerializeField] private Image _countBoxBg;
        [SerializeField] private Sprite _addBgSprite;
        [SerializeField] private Sprite _usesBgSprite;

        [Inject] private HandBoosterViewModel _viewModel;

        private DisposableBag _disposables;

        private void Start()
        {
            if (_viewModel == null) return;

            if (_button != null) _button.onClick.AddListener(OnButtonClicked);

            _viewModel.IsArmed.Subscribe(OnArmedChanged).AddTo(ref _disposables);
            _viewModel.Count.Subscribe(OnCountChanged).AddTo(ref _disposables);

            OnArmedChanged(_viewModel.IsArmed.CurrentValue);
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
            int count = _viewModel.Count.CurrentValue;
            bool armed = _viewModel.IsArmed.CurrentValue;
            if (count <= 0 && !armed)
            {
                Bus.Global.Fire(new PurchaseRequestedEvent(TransactionIds.ForBooster(BoosterId.Hand)));
                return;
            }
            _viewModel.OnButtonClicked();
        }

        private void OnArmedChanged(bool armed)
        {
            if (_countLabelGO != null) _countLabelGO.SetActive(!armed);
            if (_cancelIconGO != null) _cancelIconGO.SetActive(armed);
            UpdateInteractable();
        }

        private void OnCountChanged(int count)
        {
            if (_countLabel != null)
                _countLabel.text = count > 0 ? count.ToString() : "+";
            if (_countBoxBg != null && !_viewModel.IsArmed.CurrentValue)
                _countBoxBg.sprite = count > 0 ? _usesBgSprite : _addBgSprite;
            UpdateInteractable();
        }

        private void UpdateInteractable()
        {
            if (_button == null) return;
            _button.interactable = true;
        }
    }
}
