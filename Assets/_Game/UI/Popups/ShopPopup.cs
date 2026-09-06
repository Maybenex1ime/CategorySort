using System;
using System.Collections.Generic;
using LogosGame.Features.Shop;
using LogosGame.Features.UI.Popups.Args;
using LogosMeta.Economy;
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
    /// <summary>
    /// Shop 2 tab: Coin (trả tiền thật qua IIAPService) và Item (trả coin qua
    /// IPurchaseService). Lấy service qua [Inject] như MainMenuScreen — UIManager
    /// đã InjectRecursive trước khi gọi SetArgs nên Initialize dùng được ngay.
    /// </summary>
    public sealed class ShopPopup : PopupBase<ShopPopupArgs>
    {
        private static readonly ILogger _logger = LogManager.GetLogger<ShopPopup>();

        [Header("Chung")]
        [SerializeField] private TextMeshProUGUI _coinCounterText;
        [SerializeField] private Button _closeButton;

        [Header("Tab")]
        [SerializeField] private Button _coinTabButton;
        [SerializeField] private Button _itemTabButton;
        [SerializeField] private GameObject _coinTabRoot;
        [SerializeField] private GameObject _itemTabRoot;

        [Header("Tab Coin")]
        [SerializeField] private Transform _coinGridRoot;
        [SerializeField] private ShopCoinCellView _coinCellPrefab;

        [Header("Tab Item")]
        [SerializeField] private Transform _itemGridRoot;
        [SerializeField] private ShopItemCellView _itemCellPrefab;

        [Inject] private IShopService _shopService;
        [Inject] private ICurrencyService _currencyService;

        private readonly List<ShopCoinCellView> _coinCells = new List<ShopCoinCellView>();

        private IDisposable _coinCounterSubscription;
        private bool _built;
        private bool _isPurchasing;

        protected override void Awake()
        {
            base.Awake();
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
            if (_coinTabButton != null) _coinTabButton.onClick.AddListener(ShowCoinTab);
            if (_itemTabButton != null) _itemTabButton.onClick.AddListener(ShowItemTab);
        }

        private void OnDestroy()
        {
            _coinCounterSubscription?.Dispose();
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_coinTabButton != null) _coinTabButton.onClick.RemoveListener(ShowCoinTab);
            if (_itemTabButton != null) _itemTabButton.onClick.RemoveListener(ShowItemTab);
        }

        // Chạy lại mỗi lần mở (UIManager cache instance và gọi SetArgs lại) — nên
        // dựng ô một lần, còn counter thì chỉ subscribe một lần.
        protected override void Initialize(ShopPopupArgs args)
        {
            BindCoinCounter();
            BuildOnce();
            ShowCoinTab();
        }

        private void BindCoinCounter()
        {
            if (_coinCounterSubscription != null) return;
            if (_currencyService == null || _coinCounterText == null) return;

            _coinCounterSubscription = _currencyService.Coins
                .Subscribe(coins => _coinCounterText.text = coins.ToString("N0"));
        }

        private void BuildOnce()
        {
            if (_built) return;
            _built = true;

            if (_shopService == null)
            {
                _logger.Warn("[ShopPopup] IShopService chưa bind — shop mở rỗng.");
                return;
            }

            BuildCoinTab();
            BuildItemTab();
        }

        private void BuildCoinTab()
        {
            if (_coinGridRoot == null || _coinCellPrefab == null) return;

            IReadOnlyList<CoinBundleDefinition> bundles = _shopService.CoinBundles;
            if (bundles.Count == 0)
                _logger.Warn("[ShopPopup] SO_ShopCatalog chưa có gói coin nào — tab Coin trống.");

            for (int i = 0; i < bundles.Count; i++)
            {
                CoinBundleDefinition bundle = bundles[i];
                ShopCoinCellView cell = Instantiate(_coinCellPrefab, _coinGridRoot);
                cell.Bind(bundle, () => BuyCoinBundle(bundle.ProductId));
                _coinCells.Add(cell);
            }
        }

        private void BuildItemTab()
        {
            if (_itemGridRoot == null || _itemCellPrefab == null) return;

            IReadOnlyList<TransactionDefinition> offers = _shopService.ItemOffers;
            for (int i = 0; i < offers.Count; i++)
            {
                TransactionDefinition entry = offers[i];
                ShopItemCellView cell = Instantiate(_itemCellPrefab, _itemGridRoot);
                cell.Bind(entry, _currencyService?.Coins, () => BuyItem(entry.TransactionId));
            }
        }

        private void ShowCoinTab() => SetTab(showCoin: true);

        private void ShowItemTab() => SetTab(showCoin: false);

        private void SetTab(bool showCoin)
        {
            if (_coinTabRoot != null) _coinTabRoot.SetActive(showCoin);
            if (_itemTabRoot != null) _itemTabRoot.SetActive(!showCoin);
            if (_coinTabButton != null) _coinTabButton.interactable = !showCoin;
            if (_itemTabButton != null) _itemTabButton.interactable = showCoin;
        }

        private void BuyCoinBundle(string productId)
        {
            // Tiền thật: bấm chồng là hai đơn. Khoá tới khi store trả lời.
            if (_isPurchasing) return;
            BuyCoinBundleInBackground(productId);
        }

        private async void BuyCoinBundleInBackground(string productId)
        {
            _isPurchasing = true;
            SetCoinCellsInteractable(false);

            try
            {
                ShopPurchaseResult result = await _shopService.PurchaseCoinBundle(productId);
                if (!result.IsSuccess)
                    _logger.Warn($"[ShopPopup] Mua '{productId}' không thành: {result.Code}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[ShopPopup] Lỗi khi mua '{productId}'.");
            }
            finally
            {
                // finally bắt buộc: thoát bằng exception mà không mở khoá là shop
                // chết cứng tới lần đóng-mở lại.
                _isPurchasing = false;
                SetCoinCellsInteractable(true);
            }
        }

        private void SetCoinCellsInteractable(bool interactable)
        {
            for (int i = 0; i < _coinCells.Count; i++)
            {
                if (_coinCells[i] != null) _coinCells[i].SetInteractable(interactable);
            }
        }

        private void BuyItem(string transactionId)
        {
            if (_shopService == null) return;

            PurchaseResult result = _shopService.PurchaseItem(transactionId);
            if (!result.IsSuccess)
                _logger.Warn($"[ShopPopup] Mua item '{transactionId}' không thành: {result.Code}.");
        }

        private void OnCloseClicked()
        {
            // Đang chờ store trả lời mà đóng là mất kết quả giao dịch — chặn.
            if (_isPurchasing) return;
            Dismiss();
        }
    }
}
