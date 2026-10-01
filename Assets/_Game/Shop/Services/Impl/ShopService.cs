using System;
using System.Collections.Generic;
using LogosGame.Features.Currency;
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

        // Nơi trao Remove Ads. Vắng thì không bán Remove Ads (không bao giờ charge mà không trao được).
        private readonly INoAdsService _noAds;

        public ShopService(IShopCatalog catalog, IIAPService iap, ICurrencyService currency,
            ITransactionItemDispatcher items, IAnalyticsService analytics = null, INoAdsService noAds = null)
        {
            _catalog = catalog;
            _iap = iap;
            _currency = currency;
            _items = items;
            _analytics = analytics;
            _noAds = noAds;
        }

        public IReadOnlyList<CoinBundleDefinition> CoinBundles =>
            _catalog != null ? _catalog.CoinBundles : Array.Empty<CoinBundleDefinition>();

        public RemoveAdsDefinition RemoveAds => _catalog != null ? _catalog.RemoveAds : default;

        public bool TryGetRewardIcon(ResourceType type, out Sprite icon)
        {
            icon = null;
            return _catalog != null && _catalog.TryGetRewardIcon(type, out icon);
        }

        private bool IsRemoveAds(string productId) =>
            !string.IsNullOrEmpty(productId) && productId == RemoveAds.ProductId;

        public async Awaitable<bool> InitializeStore()
        {
            if (_iap == null) return false;

            IReadOnlyList<CoinBundleDefinition> bundles = CoinBundles;
            List<IapProduct> products = new List<IapProduct>(bundles.Count + 1);
            for (int i = 0; i < bundles.Count; i++)
            {
                if (!string.IsNullOrEmpty(bundles[i].ProductId))
                    products.Add(new IapProduct(bundles[i].ProductId, IapProductKind.Consumable));
            }

            string removeAdsId = RemoveAds.ProductId;
            if (!string.IsNullOrEmpty(removeAdsId))
                products.Add(new IapProduct(removeAdsId, IapProductKind.NonConsumable));

            bool ok = await _iap.Initialize(products, this);
            SyncRemoveAdsOwnership();
            return ok;
        }

        // Cài lại game: store còn biên nhận Remove Ads thì trả lại cờ (Android lúc khởi tạo,
        // iOS sau nút Restore). Grant tự bỏ qua khi cờ đã bật.
        private void SyncRemoveAdsOwnership()
        {
            string removeAdsId = RemoveAds.ProductId;
            if (_noAds == null || _iap == null || string.IsNullOrEmpty(removeAdsId)) return;
            if (_iap.IsOwned(removeAdsId)) _noAds.Grant();
        }

        public string GetPriceLabel(string productId)
        {
            string fallback;
            if (IsRemoveAds(productId)) fallback = RemoveAds.PriceLabelFallback;
            else if (TryGetBundle(productId, out CoinBundleDefinition bundle)) fallback = bundle.PriceLabelFallback;
            else return null;

            string storePrice = _iap != null ? _iap.GetLocalizedPrice(productId) : null;
            return string.IsNullOrEmpty(storePrice) ? fallback : storePrice;
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

            if (IsRemoveAds(productId)) return FulfillRemoveAds(productId, transactionId);

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

            // Không có coin thì AddOnce từ chối và đơn bị gửi lại mãi — chặn trước khi trao item,
            // không thì mỗi lần gửi lại user nhận thêm một bộ item.
            if (bundle.TotalCoins <= 0)
            {
                _logger.Warn($"[ShopService] Gói '{productId}' không có coin — không trao, sửa SO_ShopCatalog.");
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
                // Coin không đi qua dispatcher — đã cộng dồn vào TotalCoins, vào ví cùng AddOnce bên dưới.
                for (int i = 0; i < bundle.Items.Length; i++)
                {
                    string itemId = bundle.Items[i].Type.ToItemId();
                    if (itemId != null && bundle.Items[i].Amount > 0)
                        _items.Grant(itemId, bundle.Items[i].Amount);
                }
            }

            if (!_currency.AddOnce(bundle.TotalCoins, transactionId))
            {
                _logger.Warn($"[ShopService] Ví từ chối cộng '{productId}' ({transactionId}) — để Pending.");
                return false;
            }

            _logger.Info($"[ShopService] Trao '{productId}' ({transactionId}): +{bundle.TotalCoins} coin" +
                         (bundle.HasItems ? $" + {bundle.Items.Length} loại item." : "."));
            _analytics?.LogEvent("iap_purchase", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "coins", bundle.TotalCoins },
            });
            return true;
        }

        // Mua một lần: bật cờ thay vì cộng coin. Cờ đã bật (store gửi lại đơn) vẫn trả true để
        // store thôi gửi — Grant tự bỏ qua, không có gì bị trao hai lần.
        private bool FulfillRemoveAds(string productId, string transactionId)
        {
            if (_noAds == null)
            {
                _logger.Warn($"[ShopService] Thiếu INoAdsService — chưa trao '{productId}', để Pending.");
                return false;
            }

            bool alreadyOwned = _noAds.IsNoAds.CurrentValue;
            _noAds.Grant();
            if (alreadyOwned) return true;

            _logger.Info($"[ShopService] Trao '{productId}' ({transactionId}): bật No-Ads.");
            _analytics?.LogEvent("iap_purchase", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "coins", 0 },
            });
            return true;
        }

        public async Awaitable<ShopPurchaseResult> PurchaseProduct(string productId)
        {
            bool removeAds = IsRemoveAds(productId);
            CoinBundleDefinition bundle = default;
            if (!removeAds && !TryGetBundle(productId, out bundle))
            {
                _logger.Warn($"[ShopService] SO_ShopCatalog không có gói '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.UnknownProduct, productId, 0);
            }

            if (removeAds)
            {
                SyncRemoveAdsOwnership();
                if (_noAds != null && _noAds.IsNoAds.CurrentValue)
                    return new ShopPurchaseResult(ShopPurchaseCode.AlreadyOwned, productId, 0);
            }

            // Kiểm nơi trao TRƯỚC khi gọi store: thiếu ví (gói coin) hay thiếu No-Ads (Remove Ads)
            // mà vẫn charge là user mất tiền thật rồi không nhận được gì.
            bool canGrant = removeAds ? _noAds != null : _currency != null;
            if (_iap == null || !canGrant || !_iap.IsReady)
            {
                _logger.Warn($"[ShopService] Store chưa sẵn sàng hoặc thiếu nơi trao — không mua '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.StoreUnavailable, productId, 0);
            }

            bool accepted = await _iap.Purchase(productId);
            if (!accepted)
            {
                _logger.Info($"[ShopService] Store từ chối / user huỷ '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.StoreDeclined, productId, 0);
            }

            // KHÔNG trao ở đây: store đã gọi Fulfill (trao + ghi đĩa) TRƯỚC khi Purchase trả true.
            // Trao sau await thì app chết giữa hai dòng là user mất tiền thật.
            return new ShopPurchaseResult(ShopPurchaseCode.Success, productId, removeAds ? 0 : bundle.TotalCoins);
        }

        public async Awaitable RestorePurchases()
        {
            if (_iap == null) return;
            await _iap.RestorePurchases();
            SyncRemoveAdsOwnership();
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
