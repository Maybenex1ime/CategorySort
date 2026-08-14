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
// purchase transaction + anchor key. The tutorial gate keys off the gameplay
// domain enum, so alias it explicitly to avoid the CS1503 type mismatch.

namespace LogosGame.Features.Gameplay.Boosters.Views
{
    public class AddBeltBoosterButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private Image _countBoxBg;
        [SerializeField] private Sprite _addBgSprite;
        [SerializeField] private Sprite _usesBgSprite;
        [SerializeField] private Transform _flyingNumberAnchor;

        [Inject] private AddBeltBoosterViewModel _viewModel;
        [Inject] private IBoosterUiAnchors _anchors;

        private DisposableBag _disposables;
        private bool _anchorRegistered;
        private Transform _registeredAnchor;

        private void Start()
        {
            if (_viewModel == null) return;

            if (_button != null) _button.onClick.AddListener(OnButtonClicked);

            _viewModel.Count.Subscribe(OnCountChanged).AddTo(ref _disposables);
            OnCountChanged(_viewModel.Count.CurrentValue);

            if (_anchors != null)
            {
                Transform anchor = _flyingNumberAnchor != null ? _flyingNumberAnchor : transform;
                if (anchor != null)
                {
                    _anchors.Register(BoosterAnchorKey.AddBeltButton, anchor);
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
                _anchors.Unregister(BoosterAnchorKey.AddBeltButton, _registeredAnchor);
            }
        }

        private void OnButtonClicked()
        {
            if (_viewModel == null) return;

            // Tutorial gate: while a board step (or another booster's step) is
            // active, this booster is blocked. Idle/Done allow everything.
            // Tutorial gate cua aquapark da cat - WordStack chua port he tutorial.

            int count = _viewModel.Count.CurrentValue;
            if (count <= 0)
            {
                // Zero-count opens the purchase flow — NOT an actual use, so the
                // tutorial must not advance here.
                Bus.Global.Fire(new PurchaseRequestedEvent(TransactionIds.ForBooster(BoosterId.AddBelt)));
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
            if (_button == null) return;
            _button.interactable = true;
        }
    }
}
