using System;
using System.Collections.Generic;
using LogosMeta.Economy;
using LogosSDK.Core.Logging;
using LogosSDK.Services;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Shop.Impl
{
    public sealed class ShopService : IShopService
    {
        private static readonly ILogger _logger = LogManager.GetLogger<ShopService>();

        private readonly IShopCatalog _catalog;
        private readonly IIAPService _iap;
        private readonly ICurrencyService _currency;

        // Vắng khi CurrencyInstaller chưa được gán SO_TransactionCatalog — tab Item
        // để trống, tab Coin vẫn chạy bình thường.
        private readonly IPurchaseService _purchase;

        private readonly List<TransactionDefinition> _itemOffers = new List<TransactionDefinition>();
        private bool _itemOffersBuilt;

        public ShopService(IShopCatalog catalog, IIAPService iap, ICurrencyService currency, IPurchaseService purchase)
        {
            _catalog = catalog;
            _iap = iap;
            _currency = currency;
            _purchase = purchase;
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

        public async Awaitable<ShopPurchaseResult> PurchaseCoinBundle(string productId)
        {
            if (!TryGetBundle(productId, out CoinBundleDefinition bundle))
            {
                _logger.Warn($"[ShopService] SO_ShopCatalog không có gói '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.UnknownProduct, productId, 0);
            }

            // Kiểm tra ví TRƯỚC khi gọi store: thiếu ICurrencyService mà vẫn charge
            // là user mất tiền thật rồi không nhận được coin nào.
            if (_iap == null || _currency == null)
            {
                _logger.Warn($"[ShopService] Thiếu IIAPService/ICurrencyService — không mua '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.StoreUnavailable, productId, 0);
            }

            bool accepted = await _iap.Purchase(productId);
            if (!accepted)
            {
                _logger.Info($"[ShopService] Store từ chối / user huỷ '{productId}'.");
                return new ShopPurchaseResult(ShopPurchaseCode.StoreDeclined, productId, 0);
            }

            // Cộng thẳng vào ví, KHÔNG đi qua ITransactionItemDispatcher — dispatcher
            // đó chỉ dịch item id (booster/heart), không có case coin.
            _currency.Add(bundle.Coins);
            _logger.Info($"[ShopService] Mua '{productId}' xong, +{bundle.Coins} coin.");
            return new ShopPurchaseResult(ShopPurchaseCode.Success, productId, bundle.Coins);
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
