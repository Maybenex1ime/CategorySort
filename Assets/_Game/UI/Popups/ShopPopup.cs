using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using LogosGame.Features.Shop;
using LogosGame.Features.UI.Common;
using LogosMeta.Economy;
using LogosSDK.Core.Logging;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>
    /// Shop một trang cuộn, 3 mục: Special Offer (gói special_offer) → No Ads Offer (Remove Ads lẻ + các
    /// combo còn lại — mọi combo đều kèm No-Ads) → Coin Packs (lưới 3 cột). Nhúng trong Shop Panel của MainMenuScreen, tự dựng ở OnEnable — không còn là popup
    /// của UIManager (tên lớp giữ lại để không đứt tham chiếu script trong prefab).
    /// Mọi sản phẩm trả TIỀN THẬT qua IShopService, lấy qua [Inject] như MainMenuScreen.
    /// </summary>
    public sealed class ShopPopup : MonoBehaviour
    {
        private static readonly ILogger _logger = LogManager.GetLogger<ShopPopup>();

        private const float PunchScale = 0.15f, PunchDuration = 0.3f;

        [Header("Header (không cuộn)")]
        [SerializeField] private TextMeshProUGUI _coinCounterText;
        // Coin bay từ ô vừa mua về counter khi mua coin. Bỏ trống = chỉ nảy như cũ.
        [SerializeField] private CoinFly _coinFly;

        [Header("Special Offer — gói special_offer")]
        [SerializeField] private Transform _specialOfferRoot;

        [Header("No Ads Offer — ô Remove Ads lẻ (ẩn khi đã sở hữu) + các combo còn lại")]
        [SerializeField] private ShopRemoveAdsView _removeAdsView;
        [SerializeField] private Transform _comboListRoot;
        [SerializeField] private ShopComboCellView _comboCellPrefab;

        [Header("Coin Packs — gói coin thường")]
        [SerializeField] private Transform _coinGridRoot;
        [SerializeField] private ShopCoinCellView _coinCellPrefab;
        [Tooltip("Tiêu đề Coin Packs — nút + coin ở Home cuộn tới đây. Trống = cuộn tới lưới coin.")]
        [SerializeField] private RectTransform _coinPacksTitle;

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
        private CountUpText _coinCounter;
        private IDisposable _noAdsSubscription;
        private MotionHandle _counterPunch;
        private MotionHandle _cellPunch;
        private bool _built;
        private bool _isPurchasing;

        // Chạy lại mỗi lần panel bật — dựng ô một lần, subscribe một lần, còn giá thì hỏi lại mỗi lần
        // (giá store có thể về sau lần mở đầu). OnEnable đầu tiên chạy ngay trong Instantiate, trước
        // khi UIManager inject — bỏ qua lần đó.
        private void OnEnable()
        {
            if (_shopService == null) return;
            BindCoinCounter();
            BindNoAds();
            BuildOnce();
            RefreshPrices();
        }

        private void OnDestroy()
        {
            _coinCounterSubscription?.Dispose();
            _noAdsSubscription?.Dispose();
            _counterPunch.TryCancel();
            _cellPunch.TryCancel();
        }

        private void BindCoinCounter()
        {
            if (_coinCounterSubscription != null) return;
            if (_currencyService == null || _coinCounterText == null) return;

            _coinCounter = new CountUpText(_coinCounterText);
            _coinCounterSubscription = _currencyService.Coins
                .Subscribe(coins => _coinCounter.Set(coins));
        }

        /// Cuộn để tiêu đề Coin Packs chạm mép trên khung cuộn (nút + coin ở Home gọi).
        public void ScrollToCoinPacks()
        {
            if (isActiveAndEnabled) StartCoroutine(ScrollToCoinPacksAfterLayout());
        }

        private IEnumerator ScrollToCoinPacksAfterLayout()
        {
            yield return null;   // OnEnable vừa dựng ô — đợi một frame cho layout có kích thước thật
            var scroll = GetComponent<ScrollRect>();
            var target = _coinPacksTitle != null ? _coinPacksTitle : _coinGridRoot as RectTransform;
            if (scroll == null || scroll.content == null || target == null) yield break;

            Canvas.ForceUpdateCanvases();
            RectTransform content = scroll.content;
            RectTransform view = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float itemTop = corners[1].y;
            view.GetWorldCorners(corners);
            float viewTop = corners[1].y;

            scroll.StopMovement();
            float from = content.anchoredPosition.y;
            float to = ScrollYToShow(from, itemTop, viewTop, content.lossyScale.y, content.rect.height - view.rect.height);
            _scrollTween.TryCancel();
            _scrollTween = LMotion.Create(from, to, ScrollDuration).WithEase(Ease.OutQuad).WithCancelOnError()
                .Bind(content, (y, c) => c.anchoredPosition = new Vector2(c.anchoredPosition.x, y))
                .AddTo(content);
        }

        private const float ScrollDuration = 0.35f;
        private MotionHandle _scrollTween;

        // Content neo mép trên: anchoredPosition.y = quãng đã cuộn xuống. Kéo thêm đúng khoảng
        // mép trên mục còn cách mép trên khung (đổi world → đơn vị content), kẹp trong vùng cuộn được.
        public static float ScrollYToShow(float contentY, float itemTopWorld, float viewTopWorld, float worldPerUnit, float maxY)
        {
            return Mathf.Clamp(contentY + (viewTopWorld - itemTopWorld) / worldPerUnit, 0f, Mathf.Max(0f, maxY));
        }

        private void BindNoAds()
        {
            if (_noAdsSubscription != null) return;

            if (_removeAdsView == null) return;

            if (_noAdsService == null)
            {
                _removeAdsView.gameObject.SetActive(false);
                return;
            }

            // Đã sở hữu thì ẩn ô Remove Ads lẻ — kể cả ngay sau khi mua xong (Remove Ads hay combo).
            // Các combo vẫn hiện: còn coin và item để bán.
            _noAdsSubscription = _noAdsService.IsNoAds
                .Subscribe(owned => _removeAdsView.gameObject.SetActive(!owned));
        }

        private void BuildOnce()
        {
            if (_built) return;
            _built = true;

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
            IReadOnlyList<CoinBundleDefinition> specials = _shopService.SpecialBundles;
            IReadOnlyList<CoinBundleDefinition> bundles = _shopService.CoinBundles;
            if (specials.Count == 0 && bundles.Count == 0)
                _logger.Warn("[ShopPopup] SO_ShopCatalog chưa có gói nào — shop trống.");

            for (int i = 0; i < specials.Count; i++)
                SpawnCombo(specials[i], _specialOfferRoot);

            for (int i = 0; i < bundles.Count; i++)
            {
                CoinBundleDefinition bundle = bundles[i];
                if (bundle.HasItems)
                {
                    SpawnCombo(bundle, _comboListRoot);
                    continue;
                }

                if (_coinCellPrefab == null || _coinGridRoot == null) continue;
                ShopCoinCellView cell = Instantiate(_coinCellPrefab, _coinGridRoot);
                cell.Bind(bundle, _shopService.GetPriceLabel(bundle.ProductId), () => Buy(bundle.ProductId, cell.transform));
                _entries.Add(new Entry
                {
                    ProductId = bundle.ProductId, Root = cell.transform,
                    SetPrice = cell.SetPrice, SetInteractable = cell.SetInteractable,
                });
            }
        }

        // Ô combo dùng chung cho Special Offer lẫn No Ads Offer — chỉ khác chỗ đặt.
        private void SpawnCombo(CoinBundleDefinition bundle, Transform root)
        {
            if (_comboCellPrefab == null || root == null) return;
            ShopComboCellView cell = Instantiate(_comboCellPrefab, root);
            cell.Bind(bundle, _shopService.GetPriceLabel(bundle.ProductId), _shopService,
                () => Buy(bundle.ProductId, cell.transform));
            _entries.Add(new Entry
            {
                ProductId = bundle.ProductId, Root = cell.transform,
                SetPrice = cell.SetPrice, SetInteractable = cell.SetInteractable,
            });
        }

        private void RefreshPrices()
        {
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

        // Coin đã cộng qua subscribe (counter tự đếm dần). Có CoinFly: coin bay từ ô vừa mua về counter,
        // mỗi coin tới thì counter nảy. Không có: counter nảy một lần như cũ. Ô vừa mua nảy; banner Remove Ads tự ẩn.
        private void PlayPurchasedFeedback(Transform cell, ShopPurchaseResult result)
        {
            bool cellAlive = cell != null && cell.gameObject.activeInHierarchy;
            if (_coinCounterText != null && result.CoinsGranted > 0)
            {
                if (_coinFly != null && cellAlive)
                    _coinFly.Play(cell.position, _coinCounterText.transform.position, null, null,
                        () => _counterPunch = Punch(_coinCounterText.transform, _counterPunch));
                else
                    _counterPunch = Punch(_coinCounterText.transform, _counterPunch);
            }
            if (cellAlive) _cellPunch = Punch(cell, _cellPunch);
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
    }
}
