using System.Collections.Generic;
using LogosGame.Features.Currency;
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
        private const string Pack = "pack_starter";
        private const string RemoveAdsId = "remove_ads";

        [Test]
        public void PurchaseProduct_StoreChapNhan_CongDungSoCoin()
        {
            var currency = new FakeCurrency(120);
            var iap = new FakeIap { Accept = true };
            ShopService shop = Build(currency, iap);

            ShopPurchaseResult result = Await(shop.PurchaseProduct(Bundle));

            Assert.IsTrue(result.IsSuccess, "store nhận đơn thì phải Success");
            Assert.AreEqual(1000, result.CoinsGranted);
            Assert.AreEqual(1120, currency.Coins.CurrentValue, "coin phải cộng đúng số của gói");
            Assert.AreEqual(Bundle, iap.LastProductId, "phải gọi store đúng product id");
        }

        [Test]
        public void PurchaseProduct_StoreTuChoi_KhongCongCoin()
        {
            var currency = new FakeCurrency(120);
            var iap = new FakeIap { Accept = false };
            ShopService shop = Build(currency, iap);

            ShopPurchaseResult result = Await(shop.PurchaseProduct(Bundle));

            Assert.AreEqual(ShopPurchaseCode.StoreDeclined, result.Code);
            Assert.AreEqual(120, currency.Coins.CurrentValue, "user huỷ đơn mà vẫn được coin là phát không");
        }

        [Test]
        public void PurchaseProduct_GoiKhongCoTrongCatalog_KhongGoiStore()
        {
            var currency = new FakeCurrency(0);
            var iap = new FakeIap { Accept = true };
            ShopService shop = Build(currency, iap);

            ShopPurchaseResult result = Await(shop.PurchaseProduct("coins_999999"));

            Assert.AreEqual(ShopPurchaseCode.UnknownProduct, result.Code);
            Assert.IsNull(iap.LastProductId, "id lạ thì không được chạm tới store");
            Assert.AreEqual(0, currency.Coins.CurrentValue);
        }

        [Test]
        public void PurchaseProduct_ThieuCurrencyService_KhongChargeUser()
        {
            var iap = new FakeIap { Accept = true };
            var shop = new ShopService(new FakeCatalog(), iap, null, new FakeItems());

            ShopPurchaseResult result = Await(shop.PurchaseProduct(Bundle));

            Assert.AreEqual(ShopPurchaseCode.StoreUnavailable, result.Code);
            Assert.IsNull(iap.LastProductId, "không có ví để cộng thì tuyệt đối không được charge");
        }

        [Test]
        public void PurchaseProduct_ThanhCong_KhongCongCoinLan2()
        {
            var currency = new FakeCurrency(0);
            ShopService shop = Build(currency, new FakeIap());

            Await(shop.PurchaseProduct(Bundle));

            Assert.AreEqual(1000, currency.Coins.CurrentValue, "coin chỉ được cộng MỘT lần — trong Fulfill, không thêm sau await");
            Assert.AreEqual(1, currency.Grants.Count);
        }

        [Test]
        public void PurchaseProduct_StoreChuaSanSang_TraStoreUnavailable_KhongGoiStore()
        {
            var currency = new FakeCurrency(0);
            var iap = new FakeIap { Ready = false };
            ShopService shop = Build(currency, iap);

            ShopPurchaseResult result = Await(shop.PurchaseProduct(Bundle));

            Assert.AreEqual(ShopPurchaseCode.StoreUnavailable, result.Code);
            Assert.IsNull(iap.LastProductId);
            Assert.AreEqual(0, currency.Coins.CurrentValue);
        }

        [Test]
        public void InitializeStore_GoiCoinVaComboLaConsumable_RemoveAdsLaNonConsumable()
        {
            var iap = new FakeIap();
            Build(new FakeCurrency(0), iap);

            Assert.AreEqual(3, iap.Products.Count);
            Assert.AreEqual(Bundle, iap.Products[0].Id);
            Assert.AreEqual(Pack, iap.Products[1].Id);
            Assert.AreEqual(RemoveAdsId, iap.Products[2].Id);
            Assert.AreEqual(IapProductKind.Consumable, iap.Products[0].Kind);
            Assert.AreEqual(IapProductKind.Consumable, iap.Products[1].Kind);
            Assert.AreEqual(IapProductKind.NonConsumable, iap.Products[2].Kind, "mua một lần — store mới khôi phục được");
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

            var noWallet = new ShopService(new FakeCatalog(), new FakeIap(), null, new FakeItems());
            Assert.IsFalse(noWallet.Fulfill(Bundle, "gpa-2"), "không có ví thì không được báo đã trao");
        }

        [Test]
        public void Fulfill_KhongCanPurchaseDangCho()
        {
            // Store gửi lại giao dịch dở ngay lúc boot: không popup, không PurchaseProduct nào chạy.
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
        public void Fulfill_GoiCombo_TraoCoinVaMoiItem_DungMotLan()
        {
            var currency = new FakeCurrency(0);
            var items = new FakeItems();
            ShopService shop = Build(currency, new FakeIap(), items: items);

            Assert.IsTrue(shop.Fulfill(Pack, "gpa-pack"));
            Assert.IsTrue(shop.Fulfill(Pack, "gpa-pack"), "store gửi lại đơn đã trao: vẫn báo true để nó thôi gửi");

            Assert.AreEqual(2500, currency.Coins.CurrentValue, "reward Coin cộng dồn vào Coins của gói, cùng một lần AddOnce");
            CollectionAssert.AreEqual(new[] { "booster.shuffle x5", "heart x2" }, items.Granted,
                "item của gói chỉ được trao MỘT lần dù store gửi lại");
        }

        [Test]
        public void Fulfill_GoiCombo_ThieuBenTraoItem_TraFalse_KhongTraoGi()
        {
            var currency = new FakeCurrency(0);
            var shop = new ShopService(new FakeCatalog(), new FakeIap(), currency, null);

            Assert.IsFalse(shop.Fulfill(Pack, "gpa-pack"), "không trao được item thì để Pending, đừng nuốt tiền");
            Assert.AreEqual(0, currency.Coins.CurrentValue);
            Assert.IsTrue(shop.Fulfill(Bundle, "gpa-coin"), "gói chỉ có coin không cần bên trao item");
        }

        [Test]
        public void PurchaseProduct_RemoveAds_BatCo_KhongCongCoin()
        {
            var currency = new FakeCurrency(120);
            var iap = new FakeIap { Accept = true };
            var noAds = new FakeNoAds();
            ShopService shop = Build(currency, iap, noAds: noAds);

            ShopPurchaseResult result = Await(shop.PurchaseProduct(RemoveAdsId));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0, result.CoinsGranted);
            Assert.IsTrue(noAds.IsNoAds.CurrentValue, "mua xong phải bật cờ No-Ads");
            Assert.AreEqual(120, currency.Coins.CurrentValue, "Remove Ads không tặng coin");
            Assert.AreEqual(RemoveAdsId, iap.LastProductId);
        }

        [Test]
        public void Fulfill_RemoveAds_GoiLai_VanTraTrue_KhongCongCoin()
        {
            var currency = new FakeCurrency(0);
            var noAds = new FakeNoAds();
            ShopService shop = Build(currency, new FakeIap(), noAds: noAds);

            Assert.IsTrue(shop.Fulfill(RemoveAdsId, "tx-1"));
            Assert.IsTrue(shop.Fulfill(RemoveAdsId, "tx-1"), "store gửi lại đơn đã trao thì vẫn báo true cho nó thôi gửi");
            Assert.IsTrue(noAds.IsNoAds.CurrentValue);
            Assert.AreEqual(0, currency.Coins.CurrentValue);
            Assert.AreEqual(0, currency.Grants.Count, "Remove Ads không đi qua AddOnce");
        }

        [Test]
        public void PurchaseProduct_RemoveAdsDaSoHuu_TraAlreadyOwned_KhongGoiStore()
        {
            var iap = new FakeIap { Accept = true };
            var noAds = new FakeNoAds();
            noAds.Grant();
            ShopService shop = Build(new FakeCurrency(0), iap, noAds: noAds);

            ShopPurchaseResult result = Await(shop.PurchaseProduct(RemoveAdsId));

            Assert.AreEqual(ShopPurchaseCode.AlreadyOwned, result.Code);
            Assert.IsNull(iap.LastProductId, "đã mua rồi thì không được charge lần hai");
        }

        [Test]
        public void PurchaseProduct_RemoveAds_ThieuNoAdsService_StoreUnavailable_KhongCharge()
        {
            var iap = new FakeIap { Accept = true };
            ShopService shop = Build(new FakeCurrency(0), iap);

            ShopPurchaseResult result = Await(shop.PurchaseProduct(RemoveAdsId));

            Assert.AreEqual(ShopPurchaseCode.StoreUnavailable, result.Code);
            Assert.IsNull(iap.LastProductId, "không có nơi lưu cờ thì tuyệt đối không charge");
        }

        [Test]
        public void InitializeStore_StoreBaoDaSoHuuRemoveAds_BatCo()
        {
            var iap = new FakeIap();
            iap.Owned.Add(RemoveAdsId);
            var noAds = new FakeNoAds();

            Build(new FakeCurrency(0), iap, noAds: noAds);

            Assert.IsTrue(noAds.IsNoAds.CurrentValue, "cài lại game: store còn biên nhận thì phải trả lại quyền");
        }

        [Test]
        public void RestorePurchases_StoreBaoDaSoHuu_BatCo()
        {
            var iap = new FakeIap();
            var noAds = new FakeNoAds();
            ShopService shop = Build(new FakeCurrency(0), iap, noAds: noAds);
            iap.Owned.Add(RemoveAdsId);

            shop.RestorePurchases().GetAwaiter().GetResult();

            Assert.IsTrue(noAds.IsNoAds.CurrentValue);
        }

        [Test]
        public void Fulfill_RemoveAds_ThieuNoAdsService_TraFalse_DePending()
        {
            ShopService shop = Build(new FakeCurrency(0), new FakeIap());

            Assert.IsFalse(shop.Fulfill(RemoveAdsId, "tx-1"), "không có nơi lưu cờ thì không được báo đã trao");
        }

        // Adapter trao lại Remove Ads trong OnPurchasesFetched — có thể tới SAU khi init xong
        // (Restore iOS, mua trùng), không qua SyncRemoveAdsOwnership.
        [Test]
        public void Fulfill_RemoveAds_DenSauKhiKhoiTao_VanBatCo()
        {
            var currency = new FakeCurrency(0);
            var analytics = new FakeAnalytics();
            var noAds = new FakeNoAds();
            ShopService shop = Build(currency, new FakeIap(), analytics, noAds: noAds);
            Assert.IsFalse(noAds.IsNoAds.CurrentValue);

            Assert.IsTrue(shop.Fulfill(RemoveAdsId, "tx-restore"));
            Assert.IsTrue(shop.Fulfill(RemoveAdsId, "tx-restore"), "store báo lại lần nữa vẫn true");

            Assert.IsTrue(noAds.IsNoAds.CurrentValue);
            Assert.AreEqual(0, currency.Coins.CurrentValue, "Remove Ads không cộng coin");
            Assert.AreEqual(1, analytics.Events.Count, "trao lại không ghi doanh thu lần hai");
        }

        [Test]
        public void GetPriceLabel_RemoveAds_DungFallback()
        {
            ShopService shop = Build(new FakeCurrency(0), new FakeIap(), noAds: new FakeNoAds());

            Assert.AreEqual("4.99 $", shop.GetPriceLabel(RemoveAdsId));
        }

        // --- helpers ------------------------------------------------------------

        private static ShopService Build(FakeCurrency currency, FakeIap iap, FakeAnalytics analytics = null,
            FakeItems items = null, FakeNoAds noAds = null)
        {
            var shop = new ShopService(new FakeCatalog(), iap, currency, items ?? new FakeItems(), analytics, noAds);
            Await(shop.InitializeStore());   // FakeIap hoàn tất ngay — như BootState gọi lúc khởi động
            return shop;
        }

        // Mọi await bên trong đều đã hoàn tất sẵn (FakeIap trả Awaitable completed)
        // nên state machine chạy thẳng tới hết, đọc kết quả ngay được.
        private static T Await<T>(Awaitable<T> awaitable) => awaitable.GetAwaiter().GetResult();

        private sealed class FakeCatalog : IShopCatalog
        {
            public IReadOnlyList<CoinBundleDefinition> CoinBundles { get; } = new[]
            {
                new CoinBundleDefinition { ProductId = Bundle, Coins = 1000, PriceLabelFallback = "1.99 $" },
                new CoinBundleDefinition
                {
                    ProductId = Pack, Title = "Starter Pack", Coins = 2000, PriceLabelFallback = "4.99 $",
                    Items = new[]
                    {
                        new ShopReward { Type = ResourceType.BoosterShuffle, Amount = 5 },
                        new ShopReward { Type = ResourceType.Heart, Amount = 2 },
                        new ShopReward { Type = ResourceType.Coin, Amount = 500 },
                    },
                },
            };

            public RemoveAdsDefinition RemoveAds { get; } =
                new RemoveAdsDefinition { ProductId = RemoveAdsId, PriceLabelFallback = "4.99 $" };

            public bool TryGetRewardIcon(ResourceType type, out Sprite icon)
            {
                icon = null;
                return false;
            }
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

            public readonly HashSet<string> Owned = new HashSet<string>();

            public bool IsOwned(string productId) => Owned.Contains(productId);
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

        private sealed class FakeNoAds : INoAdsService
        {
            private readonly ReactiveProperty<bool> _isNoAds = new ReactiveProperty<bool>(false);

            public ReadOnlyReactiveProperty<bool> IsNoAds => _isNoAds;

            public void Grant() => _isNoAds.Value = true;
        }

        private sealed class FakeItems : ITransactionItemDispatcher
        {
            public readonly List<string> Granted = new List<string>();

            public void Grant(string itemId, int amount) => Granted.Add($"{itemId} x{amount}");
        }
    }
}
