using System;
using System.Collections.Generic;
using LogosMeta.Economy;
using LogosSDK.Core.Logging;
using LogosSDK.Services;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Shop.Impl
{
    public sealed class ShopService : IShopService, IIapFulfillment
    {
        private static readonly ILogger _logger = LogManager.GetLogger<ShopService>();

        private readonly IShopCatalog _catalog;
        private readonly IIAPService _iap;
        private readonly ICurrencyService _currency;

        // Trao item của gói combo (booster, tim). Vắng thì gói coin vẫn bán được, gói combo
        // để Pending — không bao giờ nhận tiền mà thiếu item.
        private readonly ITransactionItemDispatcher _items;

        // Tuỳ chọn — vắng thì chỉ không ghi sự kiện, đường tiền không đổi.
        private readonly IAnalyticsService _analytics;

        public ShopService(IShopCatalog catalog, IIAPService iap, ICurrencyService currency,
            ITransactionItemDispatcher items, IAnalyticsService analytics = null)
        {
            _catalog = catalog;
            _iap = iap;
            _currency = currency;
            _items = items;
            _analytics = analytics;
        }

        public IReadOnlyList<CoinBundleDefinition> CoinBundles =>
            _catalog != null ? _catalog.CoinBundles : Array.Empty<CoinBundleDefinition>();

        public Awaitable<bool> InitializeStore()
        {
            if (_iap == null)
            {
                AwaitableCompletionSource<bool> source = new AwaitableCompletionSource<bool>();
                source.SetResult(false);
                return source.Awaitable;
            }

            IReadOnlyList<CoinBundleDefinition> bundles = CoinBundles;
            List<IapProduct> products = new List<IapProduct>(bundles.Count);
            for (int i = 0; i < bundles.Count; i++)
            {
                if (!string.IsNullOrEmpty(bundles[i].ProductId))
                    products.Add(new IapProduct(bundles[i].ProductId, IapProductKind.Consumable));
            }

            return _iap.Initialize(products, this);
        }

        public string GetPriceLabel(string productId)
        {
            if (!TryGetBundle(productId, out CoinBundleDefinition bundle)) return null;

            string storePrice = _iap != null ? _iap.GetLocalizedPrice(productId) : null;
            return string.IsNullOrEmpty(storePrice) ? bundle.PriceLabelFallback : storePrice;
        }

        /// <summary>
        /// Điểm DUY NHẤT cộng coin cho tiền thật. Store gọi vào đây cho mọi giao dịch — kể cả
        /// giao dịch dở được gửi lại lúc boot, khi không có popup nào mở — và chỉ xác nhận
        /// giao dịch sau khi hàm này trả true. AddOnce ghi đĩa ngay và tự chống trao trùng.
        /// </summary>
        public bool Fulfill(string productId, string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                _logger.Warn($"[ShopService] Giao dịch '{productId}' không có mã — không trao, để store gửi lại.");
                return false;
            }

            if (!TryGetBundle(productId, out CoinBundleDefinition bundle))
            {
                _logger.Warn($"[ShopService] Store báo giao dịch cho gói lạ '{productId}' — không trao, để Pending.");
                return false;
            }

            if (_currency == null)
            {
                _logger.Warn($"[ShopService] Thiếu ICurrencyService — chưa trao '{productId}', để Pending.");
                return false;
            }

            // Đơn đã trao (store gửi lại): báo true cho store thôi gửi, KHÔNG trao item lần nữa.
            if (_currency.HasGrant(transactionId)) return true;

            // Coins <= 0 thì AddOnce từ chối và đơn bị gửi lại mãi — chặn trước khi trao item,
            // không thì mỗi lần gửi lại user nhận thêm một bộ item.
            if (bundle.Coins <= 0)
            {
                _logger.Warn($"[ShopService] Gói '{productId}' có Coins <= 0 — không trao, sửa SO_ShopCatalog.");
                return false;
            }

            if (bundle.HasItems)
            {
                if (_items == null)
                {
                    _logger.Warn($"[ShopService] Thiếu ITransactionItemDispatcher — chưa trao gói combo '{productId}', để Pending.");
                    return false;
                }

                // ponytail: trao item TRƯỚC, AddOnce (ghi mã chống trùng) SAU. App chết đúng giữa hai
                // bước thì đơn được gửi lại và item trao lần hai — lệch về phía có lợi cho user.
                // Muốn tuyệt đối một lần thì phải lưu item + mã giao dịch trong cùng một lần ghi.
                for (int i = 0; i < bundle.Items.Length; i++)
                {
                    if (bundle.Items[i].Amount > 0)
                        _items.Grant(bundle.Items[i].ItemId, bundle.Items[i].Amount);
                }
            }

            if (!_currency.AddOnce(bundle.Coins, transactionId))
            {
                _logger.Warn($"[ShopService] Ví từ chối cộng '{productId}' ({transactionId}) — để Pending.");
                return false;
            }

            _logger.Info($"[ShopService] Trao '{productId}' ({transactionId}): +{bundle.Coins} coin" +
                         (bundle.HasItems ? $" + {bundle.Items.Length} loại item." : "."));
            _analytics?.LogEvent("iap_purchase", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "coins", bundle.Coins },
            });
            return true;
        }

        public async Awaitable<ShopPurchaseResult> PurchaseCoinBundle(string productId)
        {
            if (!TryGetBundle(productId, out CoinBundleDefinition bundle))
            {
                _logger.Warn($"[ShopService] SO_ShopCatalog không có gói '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.UnknownProduct, productId, 0);
            }

            // Kiểm tra ví TRƯỚC khi gọi store: thiếu ICurrencyService mà vẫn charge
            // là user mất tiền thật rồi không nhận được coin nào.
            if (_iap == null || _currency == null || !_iap.IsReady)
            {
                _logger.Warn($"[ShopService] Store chưa sẵn sàng hoặc thiếu ví — không mua '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.StoreUnavailable, productId, 0);
            }

            bool accepted = await _iap.Purchase(productId);
            if (!accepted)
            {
                _logger.Info($"[ShopService] Store từ chối / user huỷ '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.StoreDeclined, productId, 0);
            }

            // KHÔNG cộng coin ở đây: store đã gọi Fulfill (cộng + ghi đĩa) TRƯỚC khi Purchase
            // trả true. Cộng sau await thì app chết giữa hai dòng là user mất tiền thật.
            return new ShopPurchaseResult(ShopPurchaseCode.Success, productId, bundle.Coins);
        }

        public Awaitable RestorePurchases()
        {
            if (_iap != null) return _iap.RestorePurchases();

            AwaitableCompletionSource source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }


        private bool TryGetBundle(string productId, out CoinBundleDefinition bundle)
        {
            bundle = default;
            if (string.IsNullOrEmpty(productId) || _catalog == null) return false;

            IReadOnlyList<CoinBundleDefinition> bundles = _catalog.CoinBundles;
            if (bundles == null) return false;

            for (int i = 0; i < bundles.Count; i++)
            {
                if (bundles[i].ProductId == productId)
                {
                    bundle = bundles[i];
                    return true;
                }
            }

            return false;
        }
    }
}
