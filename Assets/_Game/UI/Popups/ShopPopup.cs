using System;
using System.Collections.Generic;
using System.Globalization;
using LitMotion;
using LitMotion.Extensions;
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
    /// Shop một trang cuộn (spec 2026-10-01-shop-single-page): banner Remove Ads → ô combo → lưới
    /// 3 cột gói coin → nút Restore (chỉ iOS). Header (ô coin, tiêu đề, nút X) nằm ngoài vùng cuộn.
    /// Mọi sản phẩm trả TIỀN THẬT qua IShopService. Lấy service qua [Inject] như MainMenuScreen —
    /// UIManager đã InjectRecursive trước khi gọi SetArgs nên Initialize dùng được ngay.
    /// </summary>
    public sealed class ShopPopup : PopupBase<ShopPopupArgs>
    {
        private static readonly ILogger _logger = LogManager.GetLogger<ShopPopup>();

        private const float PunchScale = 0.15f, PunchDuration = 0.3f;

        [Header("Header (không cuộn)")]
        [SerializeField] private TextMeshProUGUI _coinCounterText;
        [SerializeField] private Button _closeButton;

        [Header("Trang cuộn — theo thứ tự từ trên xuống")]
        [SerializeField] private ShopRemoveAdsView _removeAdsView;
        [SerializeField] private Transform _comboListRoot;
        [SerializeField] private ShopComboCellView _comboCellPrefab;
        [SerializeField] private Transform _coinGridRoot;
        [SerializeField] private ShopCoinCellView _coinCellPrefab;

        [Header("Restore (chỉ hiện trên iOS — Apple bắt buộc; Android tự khôi phục)")]
        [SerializeField] private Button _restoreButton;

        [Inject] private IShopService _shopService;
        [Inject] private ICurrencyService _currencyService;
        [Inject] private INoAdsService _noAdsService;

        // Mọi ô bán được (banner, combo, coin) chung một danh sách: giá và khoá bấm làm một chỗ.
        private sealed class Entry
        {
            public string ProductId;
            public Transform Root;
            public Action<string> SetPrice;
            public Action<bool> SetInteractable;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        private IDisposable _coinCounterSubscription;
        private IDisposable _noAdsSubscription;
        private MotionHandle _counterPunch;
        private MotionHandle _cellPunch;
        private bool _built;
        private bool _isPurchasing;

        protected override void Awake()
        {
            base.Awake();
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);

            if (_restoreButton != null)
            {
                _restoreButton.gameObject.SetActive(Application.platform == RuntimePlatform.IPhonePlayer);
                _restoreButton.onClick.AddListener(OnRestoreClicked);
            }
        }

        private void OnDestroy()
        {
            _coinCounterSubscription?.Dispose();
            _noAdsSubscription?.Dispose();
            _counterPunch.TryCancel();
            _cellPunch.TryCancel();
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_restoreButton != null) _restoreButton.onClick.RemoveListener(OnRestoreClicked);
        }

        // Chạy lại mỗi lần mở (UIManager cache instance và gọi SetArgs lại) — dựng ô một lần,
        // subscribe một lần, còn giá thì hỏi lại mỗi lần (giá store có thể về sau lần mở đầu).
        protected override void Initialize(ShopPopupArgs args)
        {
            BindCoinCounter();
            BindNoAds();
            BuildOnce();
            RefreshPrices();
        }

        private void BindCoinCounter()
        {
            if (_coinCounterSubscription != null) return;
            if (_currencyService == null || _coinCounterText == null) return;

            _coinCounterSubscription = _currencyService.Coins
                .Subscribe(coins => _coinCounterText.text = coins.ToString("N0", CultureInfo.InvariantCulture));
        }

        private void BindNoAds()
        {
            if (_noAdsSubscription != null || _removeAdsView == null) return;

            if (_noAdsService == null)
            {
                _removeAdsView.gameObject.SetActive(false);
                return;
            }

            // Đã sở hữu thì ẩn banner — kể cả ngay sau khi mua xong hay sau Restore.
            _noAdsSubscription = _noAdsService.IsNoAds
                .Subscribe(owned => _removeAdsView.gameObject.SetActive(!owned));
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

            BuildRemoveAds();
            BuildBundles();
        }

        private void BuildRemoveAds()
        {
            if (_removeAdsView == null) return;

            RemoveAdsDefinition removeAds = _shopService.RemoveAds;
            if (string.IsNullOrEmpty(removeAds.ProductId))
            {
                _removeAdsView.gameObject.SetActive(false);
                return;
            }

            ShopRemoveAdsView view = _removeAdsView;
            view.Bind(removeAds, _shopService.GetPriceLabel(removeAds.ProductId),
                () => Buy(removeAds.ProductId, view.transform));
            _entries.Add(new Entry
            {
                ProductId = removeAds.ProductId, Root = view.transform,
                SetPrice = view.SetPrice, SetInteractable = view.SetInteractable,
            });
        }

        private void BuildBundles()
        {
            IReadOnlyList<CoinBundleDefinition> bundles = _shopService.CoinBundles;
            if (bundles.Count == 0)
                _logger.Warn("[ShopPopup] SO_ShopCatalog chưa có gói nào — shop trống.");

            for (int i = 0; i < bundles.Count; i++)
            {
                CoinBundleDefinition bundle = bundles[i];
                string price = _shopService.GetPriceLabel(bundle.ProductId);

                if (bundle.HasItems)
                {
                    if (_comboCellPrefab == null || _comboListRoot == null) continue;
                    ShopComboCellView cell = Instantiate(_comboCellPrefab, _comboListRoot);
                    cell.Bind(bundle, price, _shopService, () => Buy(bundle.ProductId, cell.transform));
                    _entries.Add(new Entry
                    {
                        ProductId = bundle.ProductId, Root = cell.transform,
                        SetPrice = cell.SetPrice, SetInteractable = cell.SetInteractable,
                    });
                }
                else
                {
                    if (_coinCellPrefab == null || _coinGridRoot == null) continue;
                    ShopCoinCellView cell = Instantiate(_coinCellPrefab, _coinGridRoot);
                    cell.Bind(bundle, price, () => Buy(bundle.ProductId, cell.transform));
                    _entries.Add(new Entry
                    {
                        ProductId = bundle.ProductId, Root = cell.transform,
                        SetPrice = cell.SetPrice, SetInteractable = cell.SetInteractable,
                    });
                }
            }
        }

        private void RefreshPrices()
        {
            if (_shopService == null) return;
            for (int i = 0; i < _entries.Count; i++)
                _entries[i].SetPrice(_shopService.GetPriceLabel(_entries[i].ProductId));
        }

        private void Buy(string productId, Transform cell)
        {
            // Tiền thật: bấm chồng là hai đơn. Khoá tới khi store trả lời.
            if (_isPurchasing) return;
            BuyInBackground(productId, cell);
        }

        private async void BuyInBackground(string productId, Transform cell)
        {
            _isPurchasing = true;
            SetInteractable(false);

            try
            {
                ShopPurchaseResult result = await _shopService.PurchaseProduct(productId);
                if (result.IsSuccess) PlayPurchasedFeedback(cell, result);
                else _logger.Warn($"[ShopPopup] Mua '{productId}' không thành: {result.Code}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[ShopPopup] Lỗi khi mua '{productId}'.");
            }
            finally
            {
                // finally bắt buộc: thoát bằng exception mà không mở khoá là shop chết cứng.
                _isPurchasing = false;
                SetInteractable(true);
            }
        }

        // Ô coin header nảy (chỉ khi có coin) + ô vừa mua nảy. Coin đã cộng qua subscribe; banner Remove Ads tự ẩn.
        private void PlayPurchasedFeedback(Transform cell, ShopPurchaseResult result)
        {
            if (_coinCounterText != null && result.CoinsGranted > 0) _counterPunch = Punch(_coinCounterText.transform, _counterPunch);
            if (cell != null && cell.gameObject.activeInHierarchy) _cellPunch = Punch(cell, _cellPunch);
        }

        private static MotionHandle Punch(Transform target, MotionHandle previous)
        {
            previous.TryComplete();   // trả scale về gốc trước khi nảy lần nữa
            Vector3 baseScale = target.localScale;
            return LMotion.Punch.Create(baseScale, baseScale * PunchScale, PunchDuration)
                .WithFrequency(6).WithDampingRatio(3.1f).WithCancelOnError()
                .BindToLocalScale(target).AddTo(target.gameObject);
        }

        private void SetInteractable(bool interactable)
        {
            for (int i = 0; i < _entries.Count; i++) _entries[i].SetInteractable(interactable);
        }

        private async void OnRestoreClicked()
        {
            if (_shopService == null || _isPurchasing) return;

            _isPurchasing = true;
            SetInteractable(false);
            try
            {
                await _shopService.RestorePurchases();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[ShopPopup] Lỗi khi khôi phục giao dịch.");
            }
            finally
            {
                _isPurchasing = false;
                SetInteractable(true);
            }
        }

        private void OnCloseClicked()
        {
            // Đang chờ store trả lời mà đóng là mất kết quả giao dịch — chặn.
            if (_isPurchasing) return;
            Dismiss();
        }
    }
}
