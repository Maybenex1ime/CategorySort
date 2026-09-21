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

        // Vắng khi CurrencyInstaller chưa được gán SO_TransactionCatalog — tab Item
        // để trống, tab Coin vẫn chạy bình thường.
        private readonly IPurchaseService _purchase;

        // Tuỳ chọn — vắng thì chỉ không ghi sự kiện, đường tiền không đổi.
        private readonly IAnalyticsService _analytics;

        private readonly List<TransactionDefinition> _itemOffers = new List<TransactionDefinition>();
        private bool _itemOffersBuilt;

        public ShopService(IShopCatalog catalog, IIAPService iap, ICurrencyService currency,
            IPurchaseService purchase, IAnalyticsService analytics = null)
        {
            _catalog = catalog;
            _iap = iap;
            _currency = currency;
            _purchase = purchase;
            _analytics = analytics;
        }

        public IReadOnlyList<CoinBundleDefinition> CoinBundles =>
            _catalog != null ? _catalog.CoinBundles : Array.Empty<CoinBundleDefinition>();

        public IReadOnlyList<TransactionDefinition> ItemOffers
        {
            get
            {
                if (!_itemOffersBuilt) BuildItemOffers();
                return _itemOffers;
            }
        }

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

            if (_currency.AddOnce(bundle.Coins, transactionId))
            {
                _logger.Info($"[ShopService] Trao '{productId}' ({transactionId}): +{bundle.Coins} coin.");
                _analytics?.LogEvent("iap_purchase", new Dictionary<string, object>
                {
                    { "product_id", productId },
                    { "coins", bundle.Coins },
                });
                return true;
            }

            // AddOnce từ chối: hoặc store gửi lại giao dịch đã trao (báo true cho nó thôi gửi),
            // hoặc dữ liệu gói sai (Coins <= 0) — khi đó để Pending, đừng nuốt tiền của user.
            bool alreadyGranted = _currency.HasGrant(transactionId);
            if (!alreadyGranted)
                _logger.Warn($"[ShopService] Không trao được '{productId}' ({transactionId}) — kiểm tra Coins của gói.");
            return alreadyGranted;
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

        public PurchaseResult PurchaseItem(string transactionId)
        {
            if (_purchase == null)
            {
                _logger.Warn($"[ShopService] Hệ mua chưa bind — bỏ qua '{transactionId}'.");
                return new PurchaseResult(PurchaseResultCode.UnknownTransaction, transactionId, 0);
            }

            return _purchase.TryPurchase(transactionId);
        }

        private void BuildItemOffers()
        {
            _itemOffersBuilt = true;

            if (_catalog == null) return;
            IReadOnlyList<string> ids = _catalog.ItemTransactionIds;
            if (ids == null || ids.Count == 0) return;

            if (_purchase == null)
            {
                _logger.Warn("[ShopService] IPurchaseService vắng mặt (chưa gán SO_TransactionCatalog) — tab Item để trống.");
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrEmpty(id)) continue;

                if (_purchase.TryGetTransaction(id, out TransactionDefinition entry))
                    _itemOffers.Add(entry);
                else
                    _logger.Warn($"[ShopService] SO_ShopCatalog trỏ tới '{id}' nhưng SO_TransactionCatalog không có entry — bỏ qua.");
            }
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
