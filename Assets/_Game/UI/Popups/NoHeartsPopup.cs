using LogosGame.Features.Currency;
using LogosGame.Features.Currency.Events;
using LogosGame.Features.Currency.UI;
using LogosGame.Features.UI.Popups.Args;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using LitMotion;
using LogosGame.Features.UI.Common;
using LogosSDK.UI.Base;
using R3;
using Reflex.Attributes;
using Reflex.Core;
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
        [Inject] private ICurrencyService _currencyService;
        // IPurchaseService chi co khi CurrencyInstaller duoc gan catalog — [Inject] thang
        // ma thieu contract thi Reflex nem exception luc popup instantiate, nen resolve tay.
        [Inject] private Container _container;

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
            if (_buyButton == null) return;

            // Chi hien khi he mua co mat va catalog co gia tim.
            bool canShow = _container != null
                && _container.TryGetResolver<IPurchaseService>(out _)
                && _container.Resolve<IPurchaseService>().TryGetTransaction(TransactionIds.For(ResourceType.Heart), out TransactionDefinition entry)
                && SetupBuyButton(entry.Price);

            _buyButton.gameObject.SetActive(canShow);
        }

        private bool SetupBuyButton(int price)
        {
            if (_buyPriceText != null) _buyPriceText.text = price.ToString();

            // Thieu coin thi xam nut thay vi de BoosterPurchaseFlow mo NotEnoughGoldPopup:
            // popup do se THAY popup nay ma khong goi Args.OnClose (mat duong ve menu).
            if (_currencyService != null)
            {
                _currencyService.Coins
                    .Subscribe(coins => _buyButton.interactable = coins >= price)
                    .AddTo(ref _disposables);
            }

            return true;
        }

        private MotionHandle _closeDrop;

        // Nút X ẩn tới khi popup mở xong rồi mới rơi xuống nảy (UIDrop).
        public override async Awaitable Show()
        {
            Vector3 scale = UIDrop.Hide(_closeButton);
            await base.Show();
            _closeDrop = UIDrop.Play(_closeButton, scale, _closeDrop);
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
            // BoosterPurchaseFlow nghe va mua dong bo (tru coin → cong tim), nen doc lai
            // so tim ngay sau Fire la biet mua duoc chua.
            // Mở từ nút "+" ở menu thì tim có thể hồi đầy trong lúc popup đang mở: mua lúc đó là mất coin
            // không được gì (HeartService.Add bỏ qua khi đầy) — đóng luôn.
            if (_heartService != null && _heartService.IsFull.CurrentValue)
            {
                OnCloseClicked();
                return;
            }

            int before = _heartService != null ? _heartService.Current.CurrentValue : 0;
            Bus.Global.Fire(new PurchaseRequestedEvent(TransactionIds.For(ResourceType.Heart)));
            int after = _heartService != null ? _heartService.Current.CurrentValue : 0;
            if (after <= before) return;

            CloseWithHeartGranted();
        }

        // TEMP: ads not integrated yet — grant +1 heart on tap.
        private void OnAdClicked()
        {
            _logger.Info($"[NoHeartsPopup] OnAdClicked fired. heartService={(_heartService != null ? "OK" : "NULL")}");
            if (_heartService == null)
            {
                OnCloseClicked();
                return;
            }

            int before = _heartService.Current.CurrentValue;
            _heartService.Add(1);
            int after = _heartService.Current.CurrentValue;
            _logger.Info($"[NoHeartsPopup] Hearts {before} → {after}");

            if (after > before) CloseWithHeartGranted();
            else OnCloseClicked();
        }

        private void CloseWithHeartGranted()
        {
            Dismiss();
            if (Args == null) return;
            if (Args.OnHeartGranted != null) Args.OnHeartGranted();
            else Args.OnClose?.Invoke();
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
