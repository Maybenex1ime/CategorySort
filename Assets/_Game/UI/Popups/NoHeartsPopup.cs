using LogosGame.Features.Currency.Events;
using LogosGame.Features.Currency.UI;
using LogosGame.Features.UI.Popups.Args;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using LogosSDK.UI.Base;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.UI.Popups
{
    public sealed class NoHeartsPopup : PopupBase<NoHeartsPopupArgs>
    {
        private static readonly ILogger _logger = LogManager.GetLogger<NoHeartsPopup>();

        [SerializeField] private Button _okButton;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _adButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private TextMeshProUGUI _buyPriceText;

        [Inject] private IHeartService _heartService;
        // WordStack chua co IPurchaseService — inject contract thieu la Reflex nem
        // exception ngay luc popup instantiate. Nut mua bi an trong UpdateBuyButton;
        // bat lai khi port he mua (cua hang).

        private DisposableBag _disposables;

        protected override void Awake()
        {
            base.Awake();

            if (_okButton != null)
            {
                _okButton.onClick.AddListener(OnOkClicked);
            }
            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyClicked);
            }

            if (_adButton != null)
            {
                _adButton.onClick.AddListener(OnAdClicked);
                _logger.Info("[NoHeartsPopup] Ad button listener registered.");
            }
            else
            {
                _logger.Warn("[NoHeartsPopup] _adButton is NULL — drag the button into the slot in the prefab.");
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        protected override void Initialize(NoHeartsPopupArgs args)
        {
            base.Initialize(args);

            // Re-show clears previous subscription so countdown bindings don't accumulate.
            _disposables.Dispose();
            _disposables = new DisposableBag();

            if (_heartService != null && _countdownText != null)
            {
                _heartService.TimeUntilNext
                    .Subscribe(t => _countdownText.text = $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}")
                    .AddTo(ref _disposables);
            }

            UpdateBuyButton();
        }

        private void UpdateBuyButton()
        {
            // Chua co he mua — an nut. Duong mua that quay lai khi port cua hang.
            const bool canShow = false;
            if (_buyButton != null) _buyButton.gameObject.SetActive(canShow);
        }

        private void OnDestroy()
        {
            if (_okButton != null)
            {
                _okButton.onClick.RemoveListener(OnOkClicked);
            }
            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyClicked);
            }

            if (_adButton != null)
            {
                _adButton.onClick.RemoveListener(OnAdClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            _disposables.Dispose();
        }

        private void OnOkClicked()
        {
            Dismiss();
            if (Args != null && Args.OnClose != null)
            {
                Args.OnClose();
            }
        }

        private void OnBuyClicked()
        {
            // Stub: chua ai nghe PurchaseRequestedEvent — nut nay dang an, giu lai
            // de he mua sau nay chi can viet ben nghe.
            Bus.Global.Fire(new PurchaseRequestedEvent(TransactionIds.Heart));
        }

        // TEMP: ads not integrated yet — grant +1 heart on tap.
        private void OnAdClicked()
        {
            _logger.Info($"[NoHeartsPopup] OnAdClicked fired. heartService={(_heartService != null ? "OK" : "NULL")}");
            if (_heartService != null)
            {
                int before = _heartService.Current.CurrentValue;
                _heartService.Add(1);
                int after = _heartService.Current.CurrentValue;
                _logger.Info($"[NoHeartsPopup] Hearts {before} → {after}");
            }

            Dismiss();
            if (Args != null && Args.OnClose != null)
            {
                Args.OnClose();
            }
        }

        private void OnCloseClicked()
        {
            Dismiss();
            if (Args != null && Args.OnClose != null)
            {
                Args.OnClose();
            }
        }
    }
}
