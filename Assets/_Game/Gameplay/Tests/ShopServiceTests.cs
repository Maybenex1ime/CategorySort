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
        public void PurchaseCoinBundle_ThanhCong_KhongCongCoinLan2()
        {
            var currency = new FakeCurrency(0);
            ShopService shop = Build(currency, new FakeIap());

            Await(shop.PurchaseCoinBundle(Bundle));

            Assert.AreEqual(1000, currency.Coins.CurrentValue, "coin chỉ được cộng MỘT lần — trong Fulfill, không thêm sau await");
            Assert.AreEqual(1, currency.Grants.Count);
        }

        [Test]
        public void PurchaseCoinBundle_StoreChuaSanSang_TraStoreUnavailable_KhongGoiStore()
        {
            var currency = new FakeCurrency(0);
            var iap = new FakeIap { Ready = false };
            ShopService shop = Build(currency, iap);

            ShopPurchaseResult result = Await(shop.PurchaseCoinBundle(Bundle));

            Assert.AreEqual(ShopPurchaseCode.StoreUnavailable, result.Code);
            Assert.IsNull(iap.LastProductId);
            Assert.AreEqual(0, currency.Coins.CurrentValue);
        }

        [Test]
        public void InitializeStore_DangKyDuGoiCoinLaConsumable()
        {
            var iap = new FakeIap();
            Build(new FakeCurrency(0), iap);

            Assert.AreEqual(1, iap.Products.Count);
            Assert.AreEqual(Bundle, iap.Products[0].Id);
            Assert.AreEqual(IapProductKind.Consumable, iap.Products[0].Kind);
        }

        [Test]
        public void Fulfill_GiaoDichMoi_CongCoinQuaAddOnce_TraTrue()
        {
            var currency = new FakeCurrency(10);
            var analytics = new FakeAnalytics();
            ShopService shop = Build(currency, new FakeIap(), analytics);

            bool ok = shop.Fulfill(Bundle, "gpa-123");

            Assert.IsTrue(ok);
            Assert.AreEqual(1010, currency.Coins.CurrentValue);
            CollectionAssert.Contains(currency.Grants, "gpa-123", "mã giao dịch của store là mã chống trùng của ví");
            Assert.AreEqual(1, analytics.Events.Count);
        }

        [Test]
        public void Fulfill_StoreGuiLaiGiaoDichDaTrao_KhongCongLan2_VanTraTrue()
        {
            var currency = new FakeCurrency(0);
            var analytics = new FakeAnalytics();
            ShopService shop = Build(currency, new FakeIap(), analytics);
            shop.Fulfill(Bundle, "gpa-123");

            bool again = shop.Fulfill(Bundle, "gpa-123");

            Assert.IsTrue(again, "đã trao rồi thì phải trả true để store thôi gửi lại");
            Assert.AreEqual(1000, currency.Coins.CurrentValue);
            Assert.AreEqual(1, analytics.Events.Count, "không ghi doanh thu hai lần cho một giao dịch");
        }

        [Test]
        public void Fulfill_GoiLa_ThieuVi_HoacMaRong_TraFalse()
        {
            var currency = new FakeCurrency(0);
            ShopService shop = Build(currency, new FakeIap());

            Assert.IsFalse(shop.Fulfill("coins_999999", "gpa-1"), "gói lạ: để Pending, đừng nuốt giao dịch");
            Assert.IsFalse(shop.Fulfill(Bundle, null));
            Assert.IsFalse(shop.Fulfill(Bundle, ""));
            Assert.AreEqual(0, currency.Coins.CurrentValue);

            var noWallet = new ShopService(new FakeCatalog(), new FakeIap(), null, new FakePurchase());
            Assert.IsFalse(noWallet.Fulfill(Bundle, "gpa-2"), "không có ví thì không được báo đã trao");
        }

        [Test]
        public void Fulfill_KhongCanPurchaseDangCho()
        {
            // Store gửi lại giao dịch dở ngay lúc boot: không popup, không PurchaseCoinBundle nào chạy.
            var currency = new FakeCurrency(0);
            var iap = new FakeIap();
            Build(currency, iap);

            bool ok = iap.Fulfillment.Fulfill(Bundle, "gpa-tu-hom-qua");

            Assert.IsTrue(ok);
            Assert.AreEqual(1000, currency.Coins.CurrentValue);
            Assert.AreEqual(0, iap.PurchaseCount);
        }

        [Test]
        public void GetPriceLabel_CoGiaStore_DungGiaStore()
        {
            ShopService shop = Build(new FakeCurrency(0), new FakeIap { Price = "49.000 VND" });

            Assert.AreEqual("49.000 VND", shop.GetPriceLabel(Bundle));
        }

        [Test]
        public void GetPriceLabel_KhongCoGiaStore_DungFallback()
        {
            ShopService shop = Build(new FakeCurrency(0), new FakeIap { Price = null });

            Assert.AreEqual("1.99 $", shop.GetPriceLabel(Bundle));
            Assert.IsNull(shop.GetPriceLabel("coins_999999"));
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

        private static ShopService Build(FakeCurrency currency, FakeIap iap, FakeAnalytics analytics = null)
        {
            var shop = new ShopService(new FakeCatalog(), iap, currency, new FakePurchase(), analytics);
            shop.InitializeStore();   // FakeIap hoàn tất ngay — như BootState gọi lúc khởi động
            return shop;
        }

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
            public bool Ready = true;
            public string LastProductId;
            public string Price;
            public int PurchaseCount;
            public IReadOnlyList<IapProduct> Products;
            public IIapFulfillment Fulfillment;

            public bool IsReady => Ready && Fulfillment != null;

            public Awaitable<bool> Initialize(IReadOnlyList<IapProduct> products, IIapFulfillment fulfillment)
            {
                Products = products;
                Fulfillment = fulfillment;
                return Done(Ready);
            }

            // Như store thật: nhận đơn thì giao dịch đi qua IIapFulfillment rồi mới báo về.
            public Awaitable<bool> Purchase(string productId)
            {
                LastProductId = productId;
                PurchaseCount++;
                bool ok = Accept && Fulfillment != null && Fulfillment.Fulfill(productId, "tx-" + PurchaseCount);
                return Done(ok);
            }

            public string GetLocalizedPrice(string productId) => Price;

            private static Awaitable<bool> Done(bool value)
            {
                var source = new AwaitableCompletionSource<bool>();
                source.SetResult(value);
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

            public readonly List<string> Grants = new List<string>();

            public bool AddOnce(int amount, string grantId)
            {
                if (amount <= 0 || string.IsNullOrEmpty(grantId) || Grants.Contains(grantId)) return false;
                Grants.Add(grantId);
                _coins.Value += amount;
                return true;
            }

            public bool HasGrant(string grantId) => Grants.Contains(grantId);

            public bool TrySpend(int amount)
            {
                if (_coins.Value < amount) return false;
                _coins.Value -= amount;
                return true;
            }

            public void SetCoins(int amount) => _coins.Value = Mathf.Max(0, amount);
        }

        private sealed class FakeAnalytics : IAnalyticsService
        {
            public readonly List<string> Events = new List<string>();

            public void LogEvent(string eventName, Dictionary<string, object> parameters = null) =>
                Events.Add(eventName);
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
