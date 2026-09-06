using System.Collections.Generic;
using LogosGame.Features.Shop;
using LogosGame.Features.Shop.Impl;
using LogosMeta.Economy;
using LogosSDK.Services;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Đường tiền — chỗ sai là user mất tiền thật hoặc được coin miễn phí. Fake
    /// thuần C# (không NSubstitute, asmdef test không tham chiếu) nên IShopCatalog
    /// tách khỏi ScriptableObject chính là để test được ở đây.
    /// </summary>
    public sealed class ShopServiceTests
    {
        private const string Bundle = "coins_1000";

        [Test]
        public void PurchaseCoinBundle_StoreChapNhan_CongDungSoCoin()
        {
            var currency = new FakeCurrency(120);
            var iap = new FakeIap { Accept = true };
            ShopService shop = Build(currency, iap);

            ShopPurchaseResult result = Await(shop.PurchaseCoinBundle(Bundle));

            Assert.IsTrue(result.IsSuccess, "store nhận đơn thì phải Success");
            Assert.AreEqual(1000, result.CoinsGranted);
            Assert.AreEqual(1120, currency.Coins.CurrentValue, "coin phải cộng đúng số của gói");
            Assert.AreEqual(Bundle, iap.LastProductId, "phải gọi store đúng product id");
        }

        [Test]
        public void PurchaseCoinBundle_StoreTuChoi_KhongCongCoin()
        {
            var currency = new FakeCurrency(120);
            var iap = new FakeIap { Accept = false };
            ShopService shop = Build(currency, iap);

            ShopPurchaseResult result = Await(shop.PurchaseCoinBundle(Bundle));

            Assert.AreEqual(ShopPurchaseCode.StoreDeclined, result.Code);
            Assert.AreEqual(120, currency.Coins.CurrentValue, "user huỷ đơn mà vẫn được coin là phát không");
        }

        [Test]
        public void PurchaseCoinBundle_GoiKhongCoTrongCatalog_KhongGoiStore()
        {
            var currency = new FakeCurrency(0);
            var iap = new FakeIap { Accept = true };
            ShopService shop = Build(currency, iap);

            ShopPurchaseResult result = Await(shop.PurchaseCoinBundle("coins_999999"));

            Assert.AreEqual(ShopPurchaseCode.UnknownProduct, result.Code);
            Assert.IsNull(iap.LastProductId, "id lạ thì không được chạm tới store");
            Assert.AreEqual(0, currency.Coins.CurrentValue);
        }

        [Test]
        public void PurchaseCoinBundle_ThieuCurrencyService_KhongChargeUser()
        {
            var iap = new FakeIap { Accept = true };
            var shop = new ShopService(new FakeCatalog(), iap, null, new FakePurchase());

            ShopPurchaseResult result = Await(shop.PurchaseCoinBundle(Bundle));

            Assert.AreEqual(ShopPurchaseCode.StoreUnavailable, result.Code);
            Assert.IsNull(iap.LastProductId, "không có ví để cộng thì tuyệt đối không được charge");
        }

        [Test]
        public void ItemOffers_BoQuaMaGiaoDichKhongCoTrongCatalogGiaoDich()
        {
            var purchase = new FakePurchase();
            purchase.Add("t_heart", 900);
            // "t_khong_ton_tai" cố tình không thêm.
            var catalog = new FakeCatalog(new[] { "t_heart", "t_khong_ton_tai" });
            var shop = new ShopService(catalog, new FakeIap(), new FakeCurrency(0), purchase);

            IReadOnlyList<TransactionDefinition> offers = shop.ItemOffers;

            Assert.AreEqual(1, offers.Count, "id không có entry phải bị loại, không dựng ô rỗng");
            Assert.AreEqual("t_heart", offers[0].TransactionId);
        }

        [Test]
        public void ItemOffers_ThieuPurchaseService_TraListRong()
        {
            var catalog = new FakeCatalog(new[] { "t_heart" });
            var shop = new ShopService(catalog, new FakeIap(), new FakeCurrency(0), null);

            Assert.AreEqual(0, shop.ItemOffers.Count);
        }

        // --- helpers ------------------------------------------------------------

        private static ShopService Build(FakeCurrency currency, FakeIap iap) =>
            new ShopService(new FakeCatalog(), iap, currency, new FakePurchase());

        // Mọi await bên trong đều đã hoàn tất sẵn (FakeIap trả Awaitable completed)
        // nên state machine chạy thẳng tới hết, đọc kết quả ngay được.
        private static T Await<T>(Awaitable<T> awaitable) => awaitable.GetAwaiter().GetResult();

        private sealed class FakeCatalog : IShopCatalog
        {
            private readonly string[] _itemIds;

            public FakeCatalog(string[] itemIds = null) => _itemIds = itemIds ?? new string[0];

            public IReadOnlyList<CoinBundleDefinition> CoinBundles { get; } = new[]
            {
                new CoinBundleDefinition { ProductId = Bundle, Coins = 1000, PriceLabelFallback = "1.99 $" },
            };

            public IReadOnlyList<string> ItemTransactionIds => _itemIds;
        }

        private sealed class FakeIap : IIAPService
        {
            public bool Accept = true;
            public string LastProductId;

            public Awaitable<bool> Purchase(string productId)
            {
                LastProductId = productId;
                var source = new AwaitableCompletionSource<bool>();
                source.SetResult(Accept);
                return source.Awaitable;
            }

            public Awaitable RestorePurchases()
            {
                var source = new AwaitableCompletionSource();
                source.SetResult();
                return source.Awaitable;
            }

            public bool IsOwned(string productId) => false;
        }

        private sealed class FakeCurrency : ICurrencyService
        {
            private readonly ReactiveProperty<int> _coins;

            public FakeCurrency(int initial) => _coins = new ReactiveProperty<int>(initial);

            public ReadOnlyReactiveProperty<int> Coins => _coins;

            public bool HasEnough(int amount) => _coins.Value >= amount;

            public void Add(int amount) => _coins.Value += amount;

            public bool TrySpend(int amount)
            {
                if (_coins.Value < amount) return false;
                _coins.Value -= amount;
                return true;
            }

            public void SetCoins(int amount) => _coins.Value = Mathf.Max(0, amount);
        }

        private sealed class FakePurchase : IPurchaseService
        {
            private readonly Dictionary<string, TransactionDefinition> _entries =
                new Dictionary<string, TransactionDefinition>();

            public void Add(string transactionId, int price) =>
                _entries[transactionId] = new TransactionDefinition
                {
                    TransactionId = transactionId,
                    Price = price,
                    Items = new[] { new TransactionItem { ItemId = "heart", Amount = 1 } },
                };

            public bool TryGetTransaction(string transactionId, out TransactionDefinition entry) =>
                _entries.TryGetValue(transactionId ?? string.Empty, out entry);

            public PurchaseResult TryPurchase(string transactionId) =>
                _entries.TryGetValue(transactionId ?? string.Empty, out TransactionDefinition entry)
                    ? new PurchaseResult(PurchaseResultCode.Success, transactionId, entry.Price)
                    : new PurchaseResult(PurchaseResultCode.UnknownTransaction, transactionId, 0);
        }
    }
}
