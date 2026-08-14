using BoosterModule;
using LogosGame.Features.Currency.Events;
using LogosGame.Features.Currency.UI;
using LogosGame.Features.Gameplay.Boosters.Services;
using LogosGame.Features.Gameplay.Boosters.ViewModels;
using LogosSDK.Core.Events;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// This view uses BoosterModule.BoosterId (via `using BoosterModule;`) for the
// purchase transaction. The tutorial gate keys off the gameplay domain enum, so
// alias it explicitly to avoid a CS1503 type mismatch.

namespace LogosGame.Features.Gameplay.Boosters.Views
{
    public class HammerBoosterButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private GameObject _countLabelGO;
        [SerializeField] private GameObject _cancelIconGO;
        [SerializeField] private Image _countBoxBg;
        [SerializeField] private Sprite _addBgSprite;
        [SerializeField] private Sprite _usesBgSprite;
        [SerializeField] private Transform _handAnchor;

        [Inject] private HammerBoosterViewModel _viewModel;
        [Inject] private IBoosterUiAnchors _anchors;

        private DisposableBag _disposables;
        private bool _anchorRegistered;
        private Transform _registeredAnchor;

        private void Start()
        {
            if (_viewModel == null) return;

            if (_button != null) _button.onClick.AddListener(OnButtonClicked);

            _viewModel.IsArmed.Subscribe(OnArmedChanged).AddTo(ref _disposables);
            _viewModel.Count.Subscribe(OnCountChanged).AddTo(ref _disposables);

            OnArmedChanged(_viewModel.IsArmed.CurrentValue);
            OnCountChanged(_viewModel.Count.CurrentValue);

            if (_anchors != null)
            {
                Transform anchor = _handAnchor != null ? _handAnchor : transform;
                if (anchor != null)
                {
                    _anchors.Register(BoosterAnchorKey.HammerButton, anchor);
                    _registeredAnchor = anchor;
                    _anchorRegistered = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveAllListeners();
            _disposables.Dispose();
            if (_anchorRegistered && _anchors != null)
            {
                _anchors.Unregister(BoosterAnchorKey.HammerButton, _registeredAnchor);
            }
        }

        private void OnButtonClicked()
        {
            if (_viewModel == null) return;

            // Tutorial gate: while a board step (or another booster's step) is
            // active, the hammer button is blocked — this also prevents disarming
            // during the "tap the row" step. Idle/Done allow everything.
            // Tutorial gate của aquapark đã cắt — WordStack chưa port hệ tutorial.

            int count = _viewModel.Count.CurrentValue;
            bool armed = _viewModel.IsArmed.CurrentValue;
            if (count <= 0 && !armed)
            {
                Bus.Global.Fire(new PurchaseRequestedEvent(TransactionIds.ForBooster(BoosterId.Hammer)));
                return;
            }
            _viewModel.OnButtonClicked();

            // If this click armed the hammer, the tutorial's "tap the booster"
            // step is complete — advance it to the "tap the row" step. Disarming
            // (toggle off) must not advance.
            if (!armed && _viewModel.IsArmed.CurrentValue)
            {
            }
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
