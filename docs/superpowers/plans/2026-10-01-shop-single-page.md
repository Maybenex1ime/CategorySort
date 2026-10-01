# Shop một trang + Remove Ads — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đổi `ShopPopup` thành một trang cuộn (banner Remove Ads → ô combo → lưới 3 cột gói coin → Restore) và thêm sản phẩm mua một lần `remove_ads` bật cờ No-Ads lưu bền.

**Architecture:** Remove Ads là một mục riêng trong `ShopCatalog`, đi chung `ShopService` (đăng ký `NonConsumable`, `Fulfill` gọi `INoAdsService.Grant()` thay vì cộng coin, đối chiếu `IIAPService.IsOwned` sau khởi tạo store và sau Restore). Cờ sống ở `NoAdsService` + save domain `noads`. UI: ba view mới (banner, ô combo, ô quà) và `ShopPopup` viết lại; prefab dựng bằng menu `WordStack/Setup/Build Shop`.

**Tech Stack:** Unity 6000.3.8f1, Reflex DI, R3, LitMotion 2.0.2, TextMeshPro, uGUI, Unity IAP 5.4.3 (`com.unity.purchasing`), NUnit EditMode (`WordStack.Meta.Tests`).

**Spec:** `docs/superpowers/specs/2026-10-01-shop-single-page-design.md`.

## Global Constraints

- Product id không đổi sau lần build store đầu tiên: `remove_ads`, `special_offer`, `beginner`, `medium`, `high`, `coins_1000` … `coins_100000`.
- Chỉ `Assets/_Game/Shop/Services/Impl/UnityIAPService.cs` được `using UnityEngine.Purchasing`.
- Remove Ads: `IapProductKind.NonConsumable`, giá dự phòng `"4.99 $"`, **không** cộng coin; mọi gói trong `CoinBundles` vẫn `Consumable`.
- Không bao giờ charge user khi không có nơi trao: gói coin cần `ICurrencyService`, Remove Ads cần `INoAdsService` — thiếu thì `StoreUnavailable`, không gọi store.
- No-Ads chỉ là cờ + service; **không** code chặn ads.
- Thứ tự trang: banner Remove Ads → ô combo → lưới 3 cột gói coin → nút Restore (chỉ `RuntimePlatform.IPhonePlayer`). Header (ô coin, tiêu đề, nút X) không cuộn.
- Hàng quà: coin `N0` ("2,000"), item `"x5"`, tim vô hạn (phút) `"1h"` nếu chia hết 60, ngược lại `"30m"`.
- UIManager load popup bằng `Addressables.InstantiateAsync(type.Name)` → address prefab phải là `ShopPopup`.
- **Không sửa `.prefab` / `.asset` trên đĩa** — prefab và catalog chỉ do menu Build Shop ghi trong Unity (Task 5).
- Không stage file của user: `Assets/_Game/Art/Fonts/*.asset`, `Assets/_Game/Content/SO_LevelCatalog.asset`, `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset`, `Assets/Scenes/Main.unity`, `Assets/Prefabs/Box.prefab`. Luôn `git add <đường dẫn cụ thể>`; không `git stash`.
- Commit kết thúc bằng dòng: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- File `.cs` mới cần `.meta` do Unity sinh; chưa có thì commit file `.cs` trước, `.meta` commit ở Task 5.
- Lệnh chạy từ gốc repo `D:\CategorySort` bằng Git Bash. File nguồn dùng CRLF.

**Lệnh kiểm dùng chung:**
- Compile: `./compilecheck.sh` → `game.dll OK`, `editor.dll OK`, `meta.dll OK` (`meta.dll` gồm cả `Assets/_Game/Editor`).
- Compile test: `bash .git/sdd/testcheck.sh` (sau compilecheck) → `board-tests.dll OK`, `meta-tests.dll OK`.
- Test EditMode chỉ chạy được trong Unity: Window ▸ General ▸ Test Runner ▸ EditMode ▸ `WordStack.Meta.Tests`. Ngoài Unity, "RED" = test assembly không compile (thiếu API), "GREEN" = compile được.

## File Structure

| File | Trách nhiệm |
|---|---|
| `Assets/_Game/Shop/CoinBundleDefinition.cs` (sửa) | Thêm struct `RemoveAdsDefinition`, `RewardIcon` |
| `Assets/_Game/Shop/IShopCatalog.cs` (sửa) | `RemoveAds`, `TryGetRewardIcon` |
| `Assets/_Game/Shop/SO_ShopCatalog.cs` (sửa) | Field `_removeAds`, `_rewardIcons` |
| `Assets/_Game/Shop/ShopProductIds.cs` (sửa) | Hằng `RemoveAds` + 4 combo |
| `Assets/_Game/Shop/Services/INoAdsService.cs` (mới) | `INoAdsService` + `NoAdsData` |
| `Assets/_Game/Shop/Services/Impl/NoAdsService.cs` (mới) | Cờ No-Ads lưu bền |
| `Assets/_Game/GameSaveInstaller.cs` (sửa) | Domain `"noads"` |
| `Assets/_Game/Shop/ShopInstaller.cs` (sửa) | Bind `INoAdsService`, truyền vào `ShopService` |
| `Assets/_Game/Shop/Services/IShopService.cs` (sửa) | `AlreadyOwned`, `RemoveAds`, `TryGetRewardIcon`, `PurchaseProduct` |
| `Assets/_Game/Shop/Services/Impl/ShopService.cs` (sửa) | Nhánh Remove Ads, đồng bộ sở hữu |
| `Assets/_Game/UI/Popups/ShopRewardItemView.cs` (mới) | Ô quà icon + số, `FormatAmount` |
| `Assets/_Game/UI/Popups/ShopComboCellView.cs` (mới) | Ô combo ngang |
| `Assets/_Game/UI/Popups/ShopRemoveAdsView.cs` (mới) | Banner Remove Ads |
| `Assets/_Game/UI/Popups/ShopCoinCellView.cs` (sửa) | Bỏ phần combo |
| `Assets/_Game/UI/Popups/ShopPopup.cs` (viết lại) | Trang cuộn, mua, phản hồi |
| `Assets/_Game/Editor/ShopSetup.cs` (sửa) | Dựng bố cục mới, điền catalog |
| `Assets/_Game/Gameplay/Tests/NoAdsServiceTests.cs` (mới) | Test cờ No-Ads |
| `Assets/_Game/Gameplay/Tests/ShopServiceTests.cs` (sửa) | Test Remove Ads, đổi tên API |
| `Assets/_Game/Gameplay/Tests/ShopRewardFormatTests.cs` (mới) | Test định dạng hàng quà |
| `Assets/_Game/Gameplay/Tests/ShopCatalogAssetTests.cs` (sửa) | Kiểm `remove_ads`, icon quà |

---

### Task 1: Dữ liệu catalog + cờ No-Ads

**Files:**
- Modify: `Assets/_Game/Shop/CoinBundleDefinition.cs`, `Assets/_Game/Shop/IShopCatalog.cs`, `Assets/_Game/Shop/SO_ShopCatalog.cs`, `Assets/_Game/Shop/ShopProductIds.cs`, `Assets/_Game/GameSaveInstaller.cs`, `Assets/_Game/Shop/ShopInstaller.cs`, `Assets/_Game/Gameplay/Tests/ShopServiceTests.cs` (chỉ `FakeCatalog`)
- Create: `Assets/_Game/Shop/Services/INoAdsService.cs`, `Assets/_Game/Shop/Services/Impl/NoAdsService.cs`, `Assets/_Game/Gameplay/Tests/NoAdsServiceTests.cs`

**Interfaces:**
- Produces: `struct RemoveAdsDefinition { string ProductId; string PriceLabelFallback; Sprite Icon; string Title; string Subtitle; }`; `struct RewardIcon { ResourceType Type; Sprite Icon; }`; `IShopCatalog.RemoveAds`, `IShopCatalog.TryGetRewardIcon(ResourceType, out Sprite)`; `ShopProductIds.RemoveAds = "remove_ads"`, `SpecialOffer`, `Beginner`, `Medium`, `High`; `interface INoAdsService { ReadOnlyReactiveProperty<bool> IsNoAds { get; } void Grant(); }`; `class NoAdsData { int SchemaVersion; bool Owned; }`; `class NoAdsService(ISaveManager)`. Serialized field names trên `ShopCatalog`: `_removeAds`, `_rewardIcons`.

- [ ] **Step 1: Viết test trước (RED)**

`Assets/_Game/Gameplay/Tests/NoAdsServiceTests.cs`:

```csharp
using LogosGame.Features.Shop;
using LogosGame.Features.Shop.Impl;
using LogosSDK.Save;
using NUnit.Framework;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Cờ Remove Ads là quyền đã trả tiền thật: bật là ghi đĩa NGAY (store chỉ được xác nhận
    /// đơn sau khi Fulfill trả về), và đọc lại đúng sau khi khởi động lại.
    /// </summary>
    public sealed class NoAdsServiceTests
    {
        [Test]
        public void Grant_BatCo_GhiDiaNgay()
        {
            var save = new FakeSave(new NoAdsData());
            var noAds = new NoAdsService(save);

            noAds.Grant();

            Assert.IsTrue(noAds.IsNoAds.CurrentValue);
            Assert.IsTrue(save.Data.Owned, "cờ phải nằm trong data đã ghi");
            Assert.AreEqual(1, save.ImmediateCalls, "tiền thật: phải SaveImmediate, không chờ lần ghi trễ");
        }

        [Test]
        public void Grant_LanHai_KhongGhiLai()
        {
            var save = new FakeSave(new NoAdsData());
            var noAds = new NoAdsService(save);

            noAds.Grant();
            noAds.Grant();

            Assert.AreEqual(1, save.ImmediateCalls, "đã bật rồi thì không ghi thêm");
        }

        [Test]
        public void KhoiDongLai_DocCoTuSave()
        {
            var save = new FakeSave(new NoAdsData { SchemaVersion = 1, Owned = true });

            var noAds = new NoAdsService(save);

            Assert.IsTrue(noAds.IsNoAds.CurrentValue, "cài lại / mở lại game phải giữ quyền đã mua");
            Assert.AreEqual(0, save.ImmediateCalls);
        }

        private sealed class FakeSave : ISaveManager
        {
            public readonly NoAdsData Data;
            public int ImmediateCalls;

            public FakeSave(NoAdsData data) => Data = data;

            public void Register<T>(IStorageProvider provider, string key) where T : class, new() { }
            public T Load<T>() where T : class, new() => Data as T;
            public void Save<T>(T data) where T : class, new() { }
            public void SaveImmediate<T>(T data) where T : class, new() => ImmediateCalls++;
            public void SaveAll() { }
            public void DeleteDomain<T>() where T : class, new() { }
            public bool HasDomain<T>() where T : class, new() => true;
        }
    }
}
```

- [ ] **Step 2: Compile test — phải đỏ**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `meta-tests` FAIL, `CS0246` — `NoAdsData` / `NoAdsService` không tồn tại.

- [ ] **Step 3: `CoinBundleDefinition.cs` — thêm 2 struct**

Cuối namespace `LogosGame.Features.Shop` (sau `struct CoinBundleDefinition`), thêm:

```csharp

    /// Sản phẩm mua một lần — bật cờ No-Ads, không tặng coin.
    [Serializable]
    public struct RemoveAdsDefinition
    {
        public string ProductId;

        // Như CoinBundleDefinition.PriceLabelFallback: chỗ đỡ lúc chưa có giá store.
        public string PriceLabelFallback;

        public Sprite Icon;
        public string Title;
        public string Subtitle;
    }

    /// Icon từng loại quà trong hàng quà của ô combo.
    [Serializable]
    public struct RewardIcon
    {
        public ResourceType Type;
        public Sprite Icon;
    }
```

- [ ] **Step 4: `IShopCatalog.cs`**

Thay toàn file:

```csharp
using System.Collections.Generic;
using LogosGame.Features.Currency;
using UnityEngine;

namespace LogosGame.Features.Shop
{
    /// <summary>
    /// Tách interface khỏi ScriptableObject để ShopService test được bằng fake
    /// thuần C# — cùng lý do ITransactionCatalog tồn tại bên _Modules/Economy.
    /// </summary>
    public interface IShopCatalog
    {
        /// Gói coin thường + gói combo (có Items), đều bán bằng tiền thật.
        IReadOnlyList<CoinBundleDefinition> CoinBundles { get; }

        /// Sản phẩm mua một lần. ProductId rỗng = shop không bán Remove Ads.
        RemoveAdsDefinition RemoveAds { get; }

        /// Icon của một loại quà; thiếu thì ô quà chỉ hiện chữ.
        bool TryGetRewardIcon(ResourceType type, out Sprite icon);
    }
}
```

- [ ] **Step 5: `SO_ShopCatalog.cs`**

Thay toàn file:

```csharp
using System;
using System.Collections.Generic;
using LogosGame.Features.Currency;
using UnityEngine;

namespace LogosGame.Features.Shop
{
    [CreateAssetMenu(fileName = "SO_ShopCatalog", menuName = "WordStack/Config/Shop Catalog")]
    public sealed class ShopCatalog : ScriptableObject, IShopCatalog
    {
        [Header("Gói bán bằng tiền thật — để trống Items là gói coin, có Items là gói combo")]
        [SerializeField] private CoinBundleDefinition[] _coinBundles = Array.Empty<CoinBundleDefinition>();

        [Header("Mua một lần — bật cờ No-Ads")]
        [SerializeField] private RemoveAdsDefinition _removeAds;

        [Header("Icon từng loại quà (hàng quà trong ô combo)")]
        [SerializeField] private List<RewardIcon> _rewardIcons = new List<RewardIcon>();

        public IReadOnlyList<CoinBundleDefinition> CoinBundles =>
            _coinBundles ?? (IReadOnlyList<CoinBundleDefinition>)Array.Empty<CoinBundleDefinition>();

        public RemoveAdsDefinition RemoveAds => _removeAds;

        public bool TryGetRewardIcon(ResourceType type, out Sprite icon)
        {
            icon = null;
            if (_rewardIcons == null) return false;
            for (int i = 0; i < _rewardIcons.Count; i++)
            {
                if (_rewardIcons[i].Type == type && _rewardIcons[i].Icon != null)
                {
                    icon = _rewardIcons[i].Icon;
                    return true;
                }
            }
            return false;
        }
    }
}
```

- [ ] **Step 6: `ShopProductIds.cs`**

Sau dòng `public const string Coins100000 = "coins_100000";` thêm:

```csharp

        // Gói combo (coin + item) — đã có trong SO_ShopCatalog.asset.
        public const string SpecialOffer = "special_offer";
        public const string Beginner = "beginner";
        public const string Medium = "medium";
        public const string High = "high";

        // Mua một lần (NonConsumable) — KHÔNG nằm trong CoinBundles, sống ở ShopCatalog.RemoveAds.
        public const string RemoveAds = "remove_ads";
```

- [ ] **Step 7: `INoAdsService.cs`**

`Assets/_Game/Shop/Services/INoAdsService.cs`:

```csharp
using System;
using R3;

namespace LogosGame.Features.Shop
{
    /// <summary>
    /// Quyền "không quảng cáo" đã mua (sản phẩm remove_ads). Chỉ là cờ: code ads tích hợp sau
    /// đọc IsNoAds để bỏ interstitial / banner; rewarded ad do người chơi tự bấm nên không chặn.
    /// </summary>
    public interface INoAdsService
    {
        ReadOnlyReactiveProperty<bool> IsNoAds { get; }

        /// Bật cờ và ghi đĩa ngay. Đã bật thì không làm gì.
        void Grant();
    }

    [Serializable]
    public class NoAdsData
    {
        public int SchemaVersion = 1;
        public bool Owned;
    }
}
```

- [ ] **Step 8: `NoAdsService.cs`**

`Assets/_Game/Shop/Services/Impl/NoAdsService.cs`:

```csharp
using System;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using R3;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Shop.Impl
{
    public sealed class NoAdsService : INoAdsService, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<NoAdsService>();

        private readonly ISaveManager _save;
        private readonly NoAdsData _data;
        private readonly ReactiveProperty<bool> _isNoAds;

        public NoAdsService(ISaveManager save)
        {
            _save = save;
            _data = _save.Load<NoAdsData>() ?? new NoAdsData();
            _isNoAds = new ReactiveProperty<bool>(_data.Owned);
        }

        public ReadOnlyReactiveProperty<bool> IsNoAds => _isNoAds;

        public void Grant()
        {
            if (_data.Owned) return;

            _data.Owned = true;
            // Ghi NGAY: ShopService.Fulfill chỉ trả true (store xác nhận đơn) sau dòng này.
            _save.SaveImmediate(_data);
            _isNoAds.Value = true;
            _logger.Info("[NoAdsService] Đã bật No-Ads.");
        }

        public void Dispose() => _isNoAds.Dispose();
    }
}
```

- [ ] **Step 9: Đăng ký save domain + bind service**

`GameSaveInstaller.cs`: thêm `using LogosGame.Features.Shop;` vào nhóm using, và ngay sau dòng `save.Register<LevelProgressData>(json, "progress");` thêm:

```csharp
                save.Register<NoAdsData>(json, "noads");
```

`ShopInstaller.cs`: thêm `using LogosSDK.Save;`. Trong `InstallBindings`, ngay TRƯỚC `builder.RegisterFactory<IShopService>(` thêm:

```csharp
            // Lazy như mọi service đọc save: domain "noads" chỉ đăng ký xong ở OnContainerBuilt
            // của GameSaveInstaller.
            builder.RegisterFactory<INoAdsService>(
                c => new NoAdsService(c.Resolve<ISaveManager>()),
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

```

(Việc truyền `INoAdsService` vào `ShopService` làm ở Task 2.)

- [ ] **Step 10: `FakeCatalog` trong `ShopServiceTests.cs` implement interface mới**

Trong `private sealed class FakeCatalog : IShopCatalog`, sau khai báo `CoinBundles { get; } = new[] { ... };` thêm:

```csharp

            public RemoveAdsDefinition RemoveAds { get; } =
                new RemoveAdsDefinition { ProductId = RemoveAdsId, PriceLabelFallback = "4.99 $" };

            public bool TryGetRewardIcon(ResourceType type, out Sprite icon)
            {
                icon = null;
                return false;
            }
```

và trong class `ShopServiceTests`, sau `private const string Pack = "pack_starter";` thêm:

```csharp
        private const string RemoveAdsId = "remove_ads";
```

- [ ] **Step 11: Compile — phải xanh**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → 3 dll OK + `board-tests.dll OK`, `meta-tests.dll OK`.
Test EditMode `NoAdsServiceTests` chạy trong Unity ở Task 5.

- [ ] **Step 12: Commit**

```bash
git add Assets/_Game/Shop/CoinBundleDefinition.cs Assets/_Game/Shop/IShopCatalog.cs Assets/_Game/Shop/SO_ShopCatalog.cs Assets/_Game/Shop/ShopProductIds.cs Assets/_Game/Shop/Services/INoAdsService.cs Assets/_Game/Shop/Services/Impl/NoAdsService.cs Assets/_Game/GameSaveInstaller.cs Assets/_Game/Shop/ShopInstaller.cs Assets/_Game/Gameplay/Tests/ShopServiceTests.cs Assets/_Game/Gameplay/Tests/NoAdsServiceTests.cs
git commit -m "Shop: catalog carries Remove Ads and reward icons; No-Ads flag service

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

(Kèm `.meta` của 3 file mới nếu Unity đã sinh; chưa có thì để Task 5.)

---

### Task 2: `ShopService` bán Remove Ads

**Files:**
- Modify: `Assets/_Game/Shop/Services/IShopService.cs`, `Assets/_Game/Shop/Services/Impl/ShopService.cs`, `Assets/_Game/Shop/ShopInstaller.cs`, `Assets/_Game/UI/Popups/ShopPopup.cs` (một dòng gọi), `Assets/_Game/Gameplay/Tests/ShopServiceTests.cs`

**Interfaces:**
- Consumes: `INoAdsService`, `RemoveAdsDefinition`, `IShopCatalog.RemoveAds`, `IShopCatalog.TryGetRewardIcon` (Task 1).
- Produces: `ShopPurchaseCode.AlreadyOwned = 4`; `IShopService.RemoveAds` (`RemoveAdsDefinition`), `IShopService.TryGetRewardIcon(ResourceType, out Sprite)`, `IShopService.PurchaseProduct(string) : Awaitable<ShopPurchaseResult>` (thay `PurchaseCoinBundle`); constructor `ShopService(IShopCatalog, IIAPService, ICurrencyService, ITransactionItemDispatcher, IAnalyticsService analytics = null, INoAdsService noAds = null)`.

- [ ] **Step 1: Đổi tên API trong test + viết test mới (RED)**

Trong `ShopServiceTests.cs`: thay mọi `PurchaseCoinBundle(` thành `PurchaseProduct(` (gồm cả tên method test `PurchaseCoinBundle_…` → `PurchaseProduct_…`).

Sửa test `InitializeStore_DangKyDuGoiCoinVaGoiComboLaConsumable` thành:

```csharp
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
```

Thêm các test (ngay trước dòng `private static ShopService Build(`):

```csharp
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
        public void GetPriceLabel_RemoveAds_DungFallback()
        {
            ShopService shop = Build(new FakeCurrency(0), new FakeIap(), noAds: new FakeNoAds());

            Assert.AreEqual("4.99 $", shop.GetPriceLabel(RemoveAdsId));
        }

```

Sửa helper `Build` thêm tham số `noAds`:

```csharp
        private static ShopService Build(FakeCurrency currency, FakeIap iap, FakeAnalytics analytics = null,
            FakeItems items = null, FakeNoAds noAds = null)
        {
            var shop = new ShopService(new FakeCatalog(), iap, currency, items ?? new FakeItems(), analytics, noAds);
            shop.InitializeStore();   // FakeIap hoàn tất ngay — như BootState gọi lúc khởi động
            return shop;
        }
```

Trong `FakeIap`: thay `public bool IsOwned(string productId) => false;` bằng:

```csharp
            public readonly HashSet<string> Owned = new HashSet<string>();

            public bool IsOwned(string productId) => Owned.Contains(productId);
```

Thêm fake mới (cạnh `FakeItems`):

```csharp
        private sealed class FakeNoAds : INoAdsService
        {
            private readonly ReactiveProperty<bool> _isNoAds = new ReactiveProperty<bool>(false);

            public ReadOnlyReactiveProperty<bool> IsNoAds => _isNoAds;

            public void Grant() => _isNoAds.Value = true;
        }
```

- [ ] **Step 2: Compile test — phải đỏ**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `meta-tests` FAIL — `PurchaseProduct`, `ShopPurchaseCode.AlreadyOwned` không tồn tại; constructor `ShopService` không có tham số thứ 6.

- [ ] **Step 3: `IShopService.cs`**

Enum:

```csharp
    public enum ShopPurchaseCode
    {
        Success = 0,
        UnknownProduct = 1,
        StoreDeclined = 2,
        StoreUnavailable = 3,
        AlreadyOwned = 4,
    }
```

Interface `IShopService` (thay toàn bộ phần thân interface; giữ doc-comment phía trên):

```csharp
    public interface IShopService
    {
        IReadOnlyList<CoinBundleDefinition> CoinBundles { get; }

        /// Sản phẩm mua một lần; ProductId rỗng = không bán.
        RemoveAdsDefinition RemoveAds { get; }

        bool TryGetRewardIcon(ResourceType type, out Sprite icon);

        /// Khởi tạo store với mọi gói trong catalog + Remove Ads. Gọi một lần lúc boot (không đợi mở
        /// Shop): giao dịch đã trả tiền mà chưa trao chỉ được store gửi lại sau bước này. Xong thì
        /// đối chiếu quyền Remove Ads với store.
        Awaitable<bool> InitializeStore();

        /// Giá đã bản địa hoá từ store; chưa có thì giá dự phòng của catalog; id lạ → null.
        string GetPriceLabel(string productId);

        /// Mua gói coin, gói combo hoặc Remove Ads.
        Awaitable<ShopPurchaseResult> PurchaseProduct(string productId);

        /// Khôi phục giao dịch non-consumable rồi đối chiếu quyền Remove Ads. iOS bắt buộc có nút này;
        /// Android tự khôi phục lúc khởi tạo.
        Awaitable RestorePurchases();
    }
```

Thêm `using LogosGame.Features.Currency;` ở đầu file.

- [ ] **Step 4: `ShopService.cs`**

Field + constructor:

```csharp
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
```

Sau property `CoinBundles` thêm:

```csharp
        public RemoveAdsDefinition RemoveAds => _catalog != null ? _catalog.RemoveAds : default;

        public bool TryGetRewardIcon(ResourceType type, out Sprite icon)
        {
            icon = null;
            return _catalog != null && _catalog.TryGetRewardIcon(type, out icon);
        }

        private bool IsRemoveAds(string productId) =>
            !string.IsNullOrEmpty(productId) && productId == RemoveAds.ProductId;
```

Thay `InitializeStore` bằng:

```csharp
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
```

Thay `GetPriceLabel` bằng:

```csharp
        public string GetPriceLabel(string productId)
        {
            string fallback;
            if (IsRemoveAds(productId)) fallback = RemoveAds.PriceLabelFallback;
            else if (TryGetBundle(productId, out CoinBundleDefinition bundle)) fallback = bundle.PriceLabelFallback;
            else return null;

            string storePrice = _iap != null ? _iap.GetLocalizedPrice(productId) : null;
            return string.IsNullOrEmpty(storePrice) ? fallback : storePrice;
        }
```

Trong `Fulfill`, ngay sau khối kiểm `string.IsNullOrEmpty(transactionId)` (trước `if (!TryGetBundle(...`) thêm:

```csharp
            if (IsRemoveAds(productId)) return FulfillRemoveAds(productId, transactionId);
```

và thêm method (sau `Fulfill`):

```csharp
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
```

Thay `PurchaseCoinBundle` bằng:

```csharp
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
```

Thay `RestorePurchases` bằng:

```csharp
        public async Awaitable RestorePurchases()
        {
            if (_iap == null) return;
            await _iap.RestorePurchases();
            SyncRemoveAdsOwnership();
        }
```

- [ ] **Step 5: `ShopInstaller.cs` truyền `INoAdsService`**

Trong factory `IShopService`, sau đối số analytics (`... ? c.Resolve<IAnalyticsService>() : null`) thêm đối số thứ 6 — đổi đoạn cuối thành:

```csharp
                    c.TryGetResolver<IAnalyticsService>(out _)
                        ? c.Resolve<IAnalyticsService>()
                        : null,
                    c.Resolve<INoAdsService>()),
```

- [ ] **Step 6: `ShopPopup.cs` gọi tên mới**

Dòng `ShopPurchaseResult result = await _shopService.PurchaseCoinBundle(productId);` → `ShopPurchaseResult result = await _shopService.PurchaseProduct(productId);` (popup viết lại toàn bộ ở Task 3).

- [ ] **Step 7: Compile — phải xanh**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.
Run: `grep -rn "PurchaseCoinBundle" Assets --include=*.cs` → không còn kết quả.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Game/Shop/Services/IShopService.cs Assets/_Game/Shop/Services/Impl/ShopService.cs Assets/_Game/Shop/ShopInstaller.cs Assets/_Game/UI/Popups/ShopPopup.cs Assets/_Game/Gameplay/Tests/ShopServiceTests.cs
git commit -m "Shop: sell Remove Ads as a non-consumable that sets the No-Ads flag

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Script giao diện — trang cuộn, ô combo, ô quà, banner

**Files:**
- Create: `Assets/_Game/UI/Popups/ShopRewardItemView.cs`, `Assets/_Game/UI/Popups/ShopComboCellView.cs`, `Assets/_Game/UI/Popups/ShopRemoveAdsView.cs`, `Assets/_Game/Gameplay/Tests/ShopRewardFormatTests.cs`
- Modify: `Assets/_Game/UI/Popups/ShopCoinCellView.cs`, `Assets/_Game/UI/Popups/ShopPopup.cs` (viết lại)

**Interfaces:**
- Consumes: `IShopService.RemoveAds`, `TryGetRewardIcon`, `PurchaseProduct`, `GetPriceLabel`, `RestorePurchases`; `INoAdsService.IsNoAds` (Task 1–2).
- Produces (Task 4 dựng prefab theo đúng tên field):
  - `ShopRewardItemView`: `_icon` (Image), `_amountText` (TMP); `Bind(Sprite, string)`; `static string FormatAmount(ResourceType, int)`.
  - `ShopComboCellView`: `_icon`, `_titleText`, `_rewardRoot` (Transform), `_rewardItemPrefab` (ShopRewardItemView), `_priceText`, `_buyButton`; `Bind(CoinBundleDefinition, string, IShopService, Action)`, `SetPrice(string)`, `SetInteractable(bool)`.
  - `ShopRemoveAdsView`: `_icon`, `_titleText`, `_subtitleText`, `_priceText`, `_buyButton`; `Bind(RemoveAdsDefinition, string, Action)`, `SetPrice`, `SetInteractable`.
  - `ShopCoinCellView`: `_icon`, `_coinsText`, `_priceText`, `_popularBadge`, `_bestValueBadge`, `_buyButton`.
  - `ShopPopup`: `_coinCounterText`, `_closeButton`, `_removeAdsView`, `_comboListRoot`, `_comboCellPrefab`, `_coinGridRoot`, `_coinCellPrefab`, `_restoreButton`.

- [ ] **Step 1: Viết test định dạng trước (RED)**

`Assets/_Game/Gameplay/Tests/ShopRewardFormatTests.cs`:

```csharp
using LogosGame.Features.Currency;
using LogosGame.Features.UI.Popups;
using NUnit.Framework;

namespace WordStack.Meta.Tests
{
    public sealed class ShopRewardFormatTests
    {
        [Test]
        public void Coin_DinhDangNhom()
        {
            Assert.AreEqual(2000.ToString("N0"), ShopRewardItemView.FormatAmount(ResourceType.Coin, 2000));
        }

        [Test]
        public void Item_Dang_x()
        {
            Assert.AreEqual("x5", ShopRewardItemView.FormatAmount(ResourceType.BoosterShuffle, 5));
            Assert.AreEqual("x5", ShopRewardItemView.FormatAmount(ResourceType.Heart, 5));
        }

        [Test]
        public void TimVoHan_TheoGioHoacPhut()
        {
            Assert.AreEqual("1h", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 60));
            Assert.AreEqual("2h", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 120));
            Assert.AreEqual("30m", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 30));
            Assert.AreEqual("90m", ShopRewardItemView.FormatAmount(ResourceType.UnlimitedHeart, 90));
        }
    }
}
```

- [ ] **Step 2: Compile test — phải đỏ**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → `meta-tests` FAIL, `ShopRewardItemView` không tồn tại.

- [ ] **Step 3: `ShopRewardItemView.cs`**

```csharp
using LogosGame.Features.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>Một món quà trong hàng quà của ô combo: icon + số lượng.</summary>
    public sealed class ShopRewardItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _amountText;

        public void Bind(Sprite icon, string amount)
        {
            if (_icon != null)
            {
                _icon.gameObject.SetActive(icon != null);
                _icon.sprite = icon;
            }
            if (_amountText != null) _amountText.text = amount ?? string.Empty;
        }

        /// Coin: "2,000". Tim vô hạn (Amount = phút): "1h" nếu chia hết 60, ngược lại "30m". Còn lại: "x5".
        public static string FormatAmount(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Coin:
                    return amount.ToString("N0");
                case ResourceType.UnlimitedHeart:
                    return amount % 60 == 0 ? (amount / 60) + "h" : amount + "m";
                default:
                    return "x" + amount;
            }
        }
    }
}
```

- [ ] **Step 4: `ShopComboCellView.cs`**

```csharp
using System;
using System.Collections.Generic;
using LogosGame.Features.Currency;
using LogosGame.Features.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>
    /// Ô gói combo (gói có Items): icon gói, tên, hàng quà (coin đứng đầu), nút giá. Trả tiền thật
    /// nên luôn bấm được, không gate theo ví.
    /// </summary>
    public sealed class ShopComboCellView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Transform _rewardRoot;
        [SerializeField] private ShopRewardItemView _rewardItemPrefab;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _buyButton;

        private readonly List<ShopRewardItemView> _rewards = new List<ShopRewardItemView>();
        private Action _onClick;

        private void Awake()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_buyButton != null) _buyButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(CoinBundleDefinition bundle, string priceLabel, IShopService shop, Action onClick)
        {
            _onClick = onClick;

            if (_icon != null && bundle.Icon != null) _icon.sprite = bundle.Icon;
            if (_titleText != null) _titleText.text = bundle.Title ?? string.Empty;
            SetPrice(priceLabel);

            ClearRewards();
            // Coin của gói (cộng cả reward loại Coin) gộp thành món đầu tiên.
            AddReward(shop, ResourceType.Coin, bundle.TotalCoins);
            if (bundle.Items != null)
            {
                for (int i = 0; i < bundle.Items.Length; i++)
                {
                    if (bundle.Items[i].Type == ResourceType.Coin) continue;
                    AddReward(shop, bundle.Items[i].Type, bundle.Items[i].Amount);
                }
            }
        }

        private void AddReward(IShopService shop, ResourceType type, int amount)
        {
            if (_rewardItemPrefab == null || _rewardRoot == null || amount <= 0) return;

            Sprite icon = null;
            shop?.TryGetRewardIcon(type, out icon);
            ShopRewardItemView item = Instantiate(_rewardItemPrefab, _rewardRoot);
            // Thiếu icon thì hiện tên loại để không mất thông tin.
            string amountText = ShopRewardItemView.FormatAmount(type, amount);
            item.Bind(icon, icon != null ? amountText : type + " " + amountText);
            _rewards.Add(item);
        }

        private void ClearRewards()
        {
            for (int i = 0; i < _rewards.Count; i++)
                if (_rewards[i] != null) Destroy(_rewards[i].gameObject);
            _rewards.Clear();
        }

        public void SetPrice(string priceLabel)
        {
            if (_priceText == null) return;
            // Chưa có giá thì gạch ngang — "0" hay rỗng dễ bị đọc thành miễn phí.
            _priceText.text = string.IsNullOrEmpty(priceLabel) ? "—" : priceLabel;
        }

        /// Khoá khi đang có giao dịch chạy — chặn bấm chồng thành 2 đơn.
        public void SetInteractable(bool interactable)
        {
            if (_buyButton != null) _buyButton.interactable = interactable;
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
```

- [ ] **Step 5: `ShopRemoveAdsView.cs`**

```csharp
using System;
using LogosGame.Features.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>Banner Remove Ads đầu trang shop. ShopPopup ẩn cả banner khi đã sở hữu.</summary>
    public sealed class ShopRemoveAdsView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _buyButton;

        private Action _onClick;

        private void Awake()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_buyButton != null) _buyButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(RemoveAdsDefinition definition, string priceLabel, Action onClick)
        {
            _onClick = onClick;
            if (_icon != null && definition.Icon != null) _icon.sprite = definition.Icon;
            if (_titleText != null) _titleText.text = definition.Title ?? string.Empty;
            if (_subtitleText != null) _subtitleText.text = definition.Subtitle ?? string.Empty;
            SetPrice(priceLabel);
        }

        public void SetPrice(string priceLabel)
        {
            if (_priceText == null) return;
            _priceText.text = string.IsNullOrEmpty(priceLabel) ? "—" : priceLabel;
        }

        public void SetInteractable(bool interactable)
        {
            if (_buyButton != null) _buyButton.interactable = interactable;
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
```

- [ ] **Step 6: `ShopCoinCellView.cs` — bỏ phần combo**

- Doc-comment class đổi thành: `/// Ô gói coin (gói không có Items) trong lưới 3 cột. Trả tiền thật nên luôn bấm được, không gate theo ví.`
- Xoá khối `[Header("Gói combo (tuỳ chọn) …")]` cùng 2 field `_titleText`, `_itemsText`.
- Trong `Bind`: xoá 2 dòng `SetOptionalText(_titleText, …)` và `SetOptionalText(_itemsText, …)`.
- Xoá method `SetOptionalText` và `DescribeItems` (cả comment `ponytail` phía trên).
- Xoá `using System.Text;` và `using LogosGame.Features.Currency;`.

- [ ] **Step 7: `ShopPopup.cs` — viết lại toàn file**

```csharp
using System;
using System.Collections.Generic;
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
                .Subscribe(coins => _coinCounterText.text = coins.ToString("N0"));
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
                if (result.IsSuccess) PlayPurchasedFeedback(cell);
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

        // Ô coin header nảy + ô vừa mua nảy. Coin đã cộng qua subscribe; banner Remove Ads tự ẩn.
        private void PlayPurchasedFeedback(Transform cell)
        {
            if (_coinCounterText != null) _counterPunch = Punch(_coinCounterText.transform, _counterPunch);
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
```

- [ ] **Step 8: Compile — phải xanh**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK. (Menu Build Shop vẫn compile vì `SetRef` dùng tên field dạng chuỗi — nó sẽ được viết lại ở Task 4.)

- [ ] **Step 9: Commit**

```bash
git add Assets/_Game/UI/Popups/ShopRewardItemView.cs Assets/_Game/UI/Popups/ShopComboCellView.cs Assets/_Game/UI/Popups/ShopRemoveAdsView.cs Assets/_Game/UI/Popups/ShopCoinCellView.cs Assets/_Game/UI/Popups/ShopPopup.cs Assets/_Game/Gameplay/Tests/ShopRewardFormatTests.cs
git commit -m "Shop UI: single scrolling page with Remove Ads banner and combo cells

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Menu Build Shop dựng bố cục mới + test catalog

**Files:**
- Modify: `Assets/_Game/Editor/ShopSetup.cs`, `Assets/_Game/Gameplay/Tests/ShopCatalogAssetTests.cs`

**Interfaces:**
- Consumes: field names ở Task 3 (Produces), `ShopCatalog._removeAds`, `_rewardIcons` (Task 1), `ShopProductIds.RemoveAds`.
- Produces: prefab `Assets/_Shared/Prefab/Popup/ShopPopup.prefab`, `ShopCoinCell.prefab`, `ShopComboCell.prefab`, `ShopRewardItem.prefab` (do user chạy menu ở Task 5).

- [ ] **Step 1: Cập nhật test catalog (RED về dữ liệu — chạy trong Unity ở Task 5)**

Trong `ShopCatalogAssetTests.cs`, thay test `MoiProductId_CoGoiHopLeTrongCatalog` bằng:

```csharp
        [Test]
        public void MoiProductId_CoGoiHopLeTrongCatalog()
        {
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            Assert.IsNotNull(catalog, "Chưa có " + ShopCatalogPath + " — chạy WordStack ▸ Setup ▸ Build Shop.");

            // remove_ads không phải gói coin — kiểm riêng ở RemoveAds_HopLe.
            string[] expected = ConstValues(typeof(ShopProductIds)).Where(id => id != ShopProductIds.RemoveAds).ToArray();
            List<string> inCatalog = catalog.CoinBundles.Select(b => b.ProductId).ToList();

            foreach (string id in expected)
                Assert.Contains(id, inCatalog, $"Catalog thiếu gói '{id}'.");
            foreach (string id in inCatalog)
                Assert.Contains(id, expected, $"Gói '{id}' chưa có trong ShopProductIds — thêm hằng số, và tạo sản phẩm trên console.");

            foreach (CoinBundleDefinition b in catalog.CoinBundles)
            {
                Assert.Greater(b.Coins, 0, $"'{b.ProductId}' có Coins <= 0 (gói combo cũng phải có coin).");
                Assert.IsFalse(string.IsNullOrEmpty(b.PriceLabelFallback), $"'{b.ProductId}' chưa có PriceLabelFallback.");
                Assert.IsNotNull(b.Icon, $"'{b.ProductId}' chưa gán Icon.");
            }
        }

        [Test]
        public void RemoveAds_HopLe()
        {
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            Assert.IsNotNull(catalog, "Chưa có " + ShopCatalogPath);

            RemoveAdsDefinition removeAds = catalog.RemoveAds;
            Assert.AreEqual(ShopProductIds.RemoveAds, removeAds.ProductId, "Catalog chưa điền Remove Ads — chạy Build Shop.");
            Assert.IsFalse(string.IsNullOrEmpty(removeAds.PriceLabelFallback), "Remove Ads chưa có PriceLabelFallback.");
            Assert.IsNotNull(removeAds.Icon, "Remove Ads chưa gán Icon.");
        }

        [Test]
        public void GoiCombo_MoiLoaiQua_CoIcon()
        {
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            Assert.IsNotNull(catalog, "Chưa có " + ShopCatalogPath);

            // Coin luôn là món đầu của hàng quà nên cũng phải có icon.
            Assert.IsTrue(catalog.TryGetRewardIcon(ResourceType.Coin, out _), "Thiếu icon quà cho Coin.");
            foreach (CoinBundleDefinition b in catalog.CoinBundles)
            {
                if (b.Items == null) continue;
                foreach (ShopReward reward in b.Items)
                    Assert.IsTrue(catalog.TryGetRewardIcon(reward.Type, out _),
                        $"'{b.ProductId}' có quà {reward.Type} nhưng catalog chưa có icon cho loại này.");
            }
        }
```

- [ ] **Step 2: `ShopSetup.cs` — hằng + doc**

Doc-comment class đổi câu đầu thành: `/// Dựng trọn Shop một trang (spec 2026-10-01-shop-single-page) bằng một menu: SO_ShopCatalog (chỉ điền phần còn rỗng — không đè số GD đã chỉnh), ShopInstaller trên ProjectScope, 4 prefab (ShopPopup, ShopCoinCell, ShopComboCell, ShopRewardItem), address "ShopPopup". Prefab bị dựng lại từ đầu — chạy lại là MẤT mọi chỉnh tay.` (giữ câu về template `BoosterPurchasePopup`).

Thay khối hằng (từ `private const string CoinCellPath` tới hết `CellSize`) bằng:

```csharp
        private const string CoinCellPath = "Assets/_Shared/Prefab/Popup/ShopCoinCell.prefab";
        private const string ComboCellPath = "Assets/_Shared/Prefab/Popup/ShopComboCell.prefab";
        private const string RewardItemPath = "Assets/_Shared/Prefab/Popup/ShopRewardItem.prefab";
        private const string ShopArt = "Assets/_Game/Art/UI_New/Shop/";
        private const string CellBgPath = "Assets/_Game/Art/UI_New/Pop-Up/Popup In.png";
        private const string ComboBgPath = ShopArt + "Bundle Pack 1.png";
        private const string BannerBgPath = ShopArt + "Bundle Pack 2.png";
        private const string NoAdsIconPath = "Assets/_Game/Art/UI_New/UI Icon/Icon No Ads (big).png";
        private const string PopupAddress = "ShopPopup"; // UIManager load theo typeof(TPopup).Name

        private static readonly Vector2 CellSize = new Vector2(240f, 300f);
        private const float BannerHeight = 170f, ComboHeight = 210f, RestoreHeight = 70f;
```

- [ ] **Step 3: `EnsureCatalog` điền Remove Ads + icon quà**

Thêm `using LogosGame.Features.Currency;` ở đầu file. Trong `EnsureCatalog`, ngay TRƯỚC `so.ApplyModifiedPropertiesWithoutUndo();` thêm:

```csharp
            SerializedProperty removeAds = so.FindProperty("_removeAds");
            if (string.IsNullOrEmpty(removeAds.FindPropertyRelative("ProductId").stringValue))
            {
                removeAds.FindPropertyRelative("ProductId").stringValue = ShopProductIds.RemoveAds;
                removeAds.FindPropertyRelative("PriceLabelFallback").stringValue = "4.99 $";
                removeAds.FindPropertyRelative("Icon").objectReferenceValue = LoadSprite(NoAdsIconPath);
                removeAds.FindPropertyRelative("Title").stringValue = "Remove ads";
                removeAds.FindPropertyRelative("Subtitle").stringValue = "Mua một lần, giữ mãi";
                Debug.Log("SHOP: điền Remove Ads vào catalog.");
            }

            (ResourceType type, string path)[] icons =
            {
                (ResourceType.Coin, $"{ShopArt}Icon Shop Coin 1.png"),
                (ResourceType.Heart, "Assets/_Game/Art/UI/more lives/heart.png"),
                (ResourceType.UnlimitedHeart, "Assets/_Game/Art/UI/more lives/heart.png"),
                (ResourceType.BoosterShuffle, "Assets/_Game/Art/Sprites/Shuffle.png"),
                (ResourceType.BoosterMagnet, "Assets/_Game/Art/UI/Booster/magnet.png"),
                (ResourceType.BoosterUndo, "Assets/_Game/Art/Sprites/Undo.png"),
            };
            SerializedProperty rewardIcons = so.FindProperty("_rewardIcons");
            if (rewardIcons.arraySize == 0)
            {
                rewardIcons.arraySize = icons.Length;
                for (int i = 0; i < icons.Length; i++)
                {
                    SerializedProperty e = rewardIcons.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("Type").enumValueIndex = (int)icons[i].type;
                    e.FindPropertyRelative("Icon").objectReferenceValue = LoadSprite(icons[i].path);
                }
                Debug.Log("SHOP: điền icon quà vào catalog.");
            }
```

(`ResourceType` bắt đầu từ 0 và liên tục — `enumValueIndex` = giá trị enum.)

- [ ] **Step 4: `BuildPrefabs` dựng trang cuộn**

Thay toàn bộ method `BuildPrefabs` và `BuildCoinCell`, và method `BuildScrollGrid`, bằng:

```csharp
        private static bool BuildPrefabs()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TemplatePath);
            try
            {
                Transform box = root.transform.Find("Box ");
                Transform closeButton = box != null ? box.Find("X Button") : null;
                Transform buttonTpl = box != null ? box.Find("Coin Button") : null;
                Transform titleTpl = box != null ? box.Find("Image/Title Text (TMP)") : null;
                Transform coinText = root.transform.Find("CoinArea/CoinBox/CoinTxt");
                if (box == null || closeButton == null || buttonTpl == null || titleTpl == null || coinText == null)
                {
                    Debug.LogError($"SHOP FAIL: {TemplatePath} đổi cấu trúc — cần 'Box '/X Button, Coin Button, Image/Title Text (TMP), CoinArea/CoinBox/CoinTxt.");
                    return false;
                }

                var textTpl = titleTpl.GetComponent<TextMeshProUGUI>();

                ShopCoinCellView coinCell = BuildCoinCell(root.transform, buttonTpl, textTpl);
                ShopRewardItemView rewardItem = BuildRewardItem(root.transform, textTpl);
                ShopComboCellView comboCell = BuildComboCell(root.transform, buttonTpl, textTpl, rewardItem);

                // Khung popup: bỏ nội dung booster, giữ nền / nút X / tiêu đề / ô coin.
                Object.DestroyImmediate(root.GetComponent<BoosterPurchasePopup>());
                foreach (string child in new[] { "Revive Image", "Ad Button" })
                {
                    Transform t = box.Find(child);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
                root.name = "ShopPopup";
                textTpl.text = "SHOP";

                RectTransform content = BuildScrollPage(box);
                ShopRemoveAdsView banner = BuildRemoveAdsBanner(content, buttonTpl, textTpl);
                Transform comboList = BuildVerticalList(content, "Combo List", 16f);
                Transform coinGrid = BuildCoinGrid(content);
                Button restore = CloneButton(buttonTpl, content, "Restore Button", "RESTORE", Vector2.zero, new Vector2(220f, RestoreHeight));
                AddLayoutHeight(restore.gameObject, RestoreHeight);
                Object.DestroyImmediate(buttonTpl.gameObject);

                var popup = root.AddComponent<ShopPopup>();
                SetRef(popup, "_coinCounterText", coinText.GetComponent<TextMeshProUGUI>());
                SetRef(popup, "_closeButton", closeButton.GetComponent<Button>());
                SetRef(popup, "_removeAdsView", banner);
                SetRef(popup, "_comboListRoot", comboList);
                SetRef(popup, "_comboCellPrefab", comboCell);
                SetRef(popup, "_coinGridRoot", coinGrid);
                SetRef(popup, "_coinCellPrefab", coinCell);
                SetRef(popup, "_restoreButton", restore);

                PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
                return true;
            }
            finally
            {
                // Không lưu ngược vào template — BoosterPurchasePopup giữ nguyên.
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ShopCoinCellView BuildCoinCell(Transform scratch, Transform buttonTpl, TextMeshProUGUI textTpl)
        {
            RectTransform cell = NewUI("ShopCoinCell", scratch);
            cell.sizeDelta = CellSize;
            cell.gameObject.AddComponent<Image>().sprite = LoadSprite(CellBgPath);

            RectTransform icon = NewUI("Icon", cell);
            Place(icon, new Vector2(0f, 50f), new Vector2(120f, 120f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TextMeshProUGUI coins = CloneText(textTpl, cell, "Coins", "1,000", new Vector2(0f, -40f), new Vector2(220f, 40f));
            Button buy = CloneButton(buttonTpl, cell, "Buy Button", "0.99 $", new Vector2(0f, -105f), new Vector2(200f, 80f));

            GameObject popular = NewBadge(cell, "Popular Badge", $"{ShopArt}Icon Shop Tag 1.png");
            GameObject bestValue = NewBadge(cell, "BestValue Badge", $"{ShopArt}Icon Shop Tag 2.png");

            var view = cell.gameObject.AddComponent<ShopCoinCellView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_coinsText", coins);
            SetRef(view, "_priceText", buy.GetComponentInChildren<TextMeshProUGUI>(true));
            SetRef(view, "_popularBadge", popular);
            SetRef(view, "_bestValueBadge", bestValue);
            SetRef(view, "_buyButton", buy);

            return SaveCell<ShopCoinCellView>(cell.gameObject, CoinCellPath);
        }

        private static ShopRewardItemView BuildRewardItem(Transform scratch, TextMeshProUGUI textTpl)
        {
            RectTransform item = NewUI("ShopRewardItem", scratch);
            item.sizeDelta = new Vector2(110f, 50f);
            var row = item.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 4f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            RectTransform icon = NewUI("Icon", item);
            icon.sizeDelta = new Vector2(44f, 44f);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TextMeshProUGUI amount = CloneText(textTpl, item, "Amount", "x5", Vector2.zero, new Vector2(62f, 44f));
            amount.alignment = TextAlignmentOptions.Left;

            var view = item.gameObject.AddComponent<ShopRewardItemView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_amountText", amount);

            return SaveCell<ShopRewardItemView>(item.gameObject, RewardItemPath);
        }

        private static ShopComboCellView BuildComboCell(Transform scratch, Transform buttonTpl, TextMeshProUGUI textTpl,
            ShopRewardItemView rewardItem)
        {
            RectTransform cell = NewUI("ShopComboCell", scratch);
            cell.sizeDelta = new Vector2(760f, ComboHeight);
            cell.gameObject.AddComponent<Image>().sprite = LoadSprite(ComboBgPath);

            RectTransform icon = NewUI("Icon", cell);
            AnchorLeft(icon, 20f, new Vector2(160f, 160f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TextMeshProUGUI title = CloneText(textTpl, cell, "Title", "Starter Pack", Vector2.zero, new Vector2(380f, 50f));
            AnchorLeft(title.rectTransform, 200f, new Vector2(380f, 50f), y: 55f);
            title.alignment = TextAlignmentOptions.Left;

            RectTransform rewards = NewUI("Rewards", cell);
            AnchorLeft(rewards, 200f, new Vector2(380f, 110f), y: -30f);
            var grid = rewards.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(110f, 50f);
            grid.spacing = new Vector2(8f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;

            Button buy = CloneButton(buttonTpl, cell, "Buy Button", "1.99 $", Vector2.zero, new Vector2(150f, 80f));
            var buyRt = (RectTransform)buy.transform;
            buyRt.anchorMin = buyRt.anchorMax = buyRt.pivot = new Vector2(1f, 0.5f);
            buyRt.anchoredPosition = new Vector2(-20f, 0f);

            var view = cell.gameObject.AddComponent<ShopComboCellView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_titleText", title);
            SetRef(view, "_rewardRoot", rewards);
            SetRef(view, "_rewardItemPrefab", rewardItem);
            SetRef(view, "_priceText", buy.GetComponentInChildren<TextMeshProUGUI>(true));
            SetRef(view, "_buyButton", buy);

            return SaveCell<ShopComboCellView>(cell.gameObject, ComboCellPath);
        }

        // Vùng cuộn dọc duy nhất dưới header; con xếp theo thứ tự thêm vào.
        private static RectTransform BuildScrollPage(Transform box)
        {
            RectTransform rootRt = NewUI("Scroll", box);
            Stretch(rootRt, left: 40f, right: 40f, top: 200f, bottom: 40f);

            var scroll = rootRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = NewUI("Viewport", rootRt);
            Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = NewUI("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 24f;
            column.padding = new RectOffset(0, 0, 10, 20);
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        private static ShopRemoveAdsView BuildRemoveAdsBanner(Transform content, Transform buttonTpl, TextMeshProUGUI textTpl)
        {
            RectTransform banner = NewUI("Remove Ads Banner", content);
            banner.gameObject.AddComponent<Image>().sprite = LoadSprite(BannerBgPath);
            AddLayoutHeight(banner.gameObject, BannerHeight);

            RectTransform icon = NewUI("Icon", banner);
            AnchorLeft(icon, 20f, new Vector2(130f, 130f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.sprite = LoadSprite(NoAdsIconPath);

            TextMeshProUGUI title = CloneText(textTpl, banner, "Title", "Remove ads", Vector2.zero, new Vector2(380f, 56f));
            AnchorLeft(title.rectTransform, 170f, new Vector2(380f, 56f), y: 28f);
            title.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI subtitle = CloneText(textTpl, banner, "Subtitle", "Mua một lần, giữ mãi", Vector2.zero, new Vector2(380f, 40f));
            AnchorLeft(subtitle.rectTransform, 170f, new Vector2(380f, 40f), y: -28f);
            subtitle.alignment = TextAlignmentOptions.Left;
            subtitle.fontSizeMax = 28f;

            Button buy = CloneButton(buttonTpl, banner, "Buy Button", "4.99 $", Vector2.zero, new Vector2(150f, 80f));
            var buyRt = (RectTransform)buy.transform;
            buyRt.anchorMin = buyRt.anchorMax = buyRt.pivot = new Vector2(1f, 0.5f);
            buyRt.anchoredPosition = new Vector2(-20f, 0f);

            var view = banner.gameObject.AddComponent<ShopRemoveAdsView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_titleText", title);
            SetRef(view, "_subtitleText", subtitle);
            SetRef(view, "_priceText", buy.GetComponentInChildren<TextMeshProUGUI>(true));
            SetRef(view, "_buyButton", buy);
            return view;
        }

        private static Transform BuildVerticalList(Transform content, string name, float spacing)
        {
            RectTransform list = NewUI(name, content);
            var column = list.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = spacing;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            return list;
        }

        private static Transform BuildCoinGrid(Transform content)
        {
            RectTransform gridRt = NewUI("Coin Grid", content);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = CellSize;
            grid.spacing = new Vector2(20f, 20f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            return gridRt;
        }
```

Thêm 2 helper (cạnh `Place` / `Stretch`):

```csharp
        private static void AnchorLeft(RectTransform rt, float left, Vector2 size, float y = 0f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(left, y);
            rt.sizeDelta = size;
        }

        // Trong VerticalLayoutGroup điều khiển chiều cao: LayoutElement chốt chiều cao ô.
        private static void AddLayoutHeight(GameObject go, float height)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
        }
```

Ô combo (prefab) được `Combo List` (VerticalLayoutGroup, childControlHeight) kéo cao theo `LayoutElement` — thêm vào cuối `BuildComboCell`, ngay trước `var view = …`:

```csharp
            AddLayoutHeight(cell.gameObject, ComboHeight);
```

- [ ] **Step 5: Compile — phải xanh**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.
Run: `grep -n "_coinTabButton\|_itemTabButton\|_itemGridRoot\|_itemsText\|BuildScrollGrid" Assets/_Game/Editor/ShopSetup.cs` → không còn kết quả.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Editor/ShopSetup.cs Assets/_Game/Gameplay/Tests/ShopCatalogAssetTests.cs
git commit -m "Build Shop menu: single-page layout, Remove Ads banner, combo and reward prefabs

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Chạy menu, test EditMode, kiểm Play mode (user làm trong Unity)

**Files:**
- Modify (menu ghi): `Assets/_Game/Content/SO_ShopCatalog.asset`, `Assets/_Shared/Prefab/Popup/ShopPopup.prefab`, `ShopCoinCell.prefab`
- Create (menu ghi): `Assets/_Shared/Prefab/Popup/ShopComboCell.prefab`, `ShopRewardItem.prefab`
- `.meta` của các file `.cs` mới (Task 1, 3)

- [ ] **Step 1: Chạy menu**

Đóng Prefab Mode của các prefab shop → **WordStack ▸ Setup ▸ Build Shop**.
Expected Console: `SHOP: điền Remove Ads vào catalog.`, `SHOP: điền icon quà vào catalog.`, `SHOP: xong — …`.
Lưu ý: menu gọi `RegisterAddress` — có thể đụng `Default Local Group.asset` (đang có thay đổi chưa commit của user); address `ShopPopup` đã có nên thường không đổi. Kiểm `git diff --stat` trước khi commit.

- [ ] **Step 2: Test EditMode**

Test Runner ▸ EditMode ▸ `WordStack.Meta.Tests` ▸ Run All → PASS (gồm `NoAdsServiceTests`, `ShopServiceTests`, `ShopRewardFormatTests`, `ShopCatalogAssetTests`).

- [ ] **Step 3: Kiểm Play mode** (stub store, `_useRealStore` = 0)

1. Mở shop từ Main Menu: banner Remove Ads, 4 ô combo có tên + hàng icon quà (coin đầu tiên), lưới 3 cột 6 gói coin, cuộn mượt; header không cuộn.
2. Mua gói coin / combo: coin + item cộng đúng, ô coin header nảy, ô vừa mua nảy.
3. Mua Remove Ads: banner ẩn ngay; đóng/mở lại shop vẫn ẩn; tắt Play, vào lại vẫn ẩn.
4. Đang mua: các nút khác không bấm được, nút X không đóng popup.

Chỉnh thẩm mỹ trong prefab nếu cần (chạy lại menu là mất chỉnh tay).

- [ ] **Step 4: Commit**

```bash
git status --short
git add Assets/_Game/Content/SO_ShopCatalog.asset Assets/_Shared/Prefab/Popup/ShopPopup.prefab Assets/_Shared/Prefab/Popup/ShopCoinCell.prefab Assets/_Shared/Prefab/Popup/ShopComboCell.prefab Assets/_Shared/Prefab/Popup/ShopComboCell.prefab.meta Assets/_Shared/Prefab/Popup/ShopRewardItem.prefab Assets/_Shared/Prefab/Popup/ShopRewardItem.prefab.meta
git add Assets/_Game/Shop/Services/INoAdsService.cs.meta Assets/_Game/Shop/Services/Impl/NoAdsService.cs.meta Assets/_Game/Gameplay/Tests/NoAdsServiceTests.cs.meta Assets/_Game/UI/Popups/ShopRewardItemView.cs.meta Assets/_Game/UI/Popups/ShopComboCellView.cs.meta Assets/_Game/UI/Popups/ShopRemoveAdsView.cs.meta Assets/_Game/Gameplay/Tests/ShopRewardFormatTests.cs.meta
git commit -m "Shop prefabs and catalog for the single-page shop with Remove Ads

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

(Bỏ khỏi lệnh `add` những `.meta` đã commit ở Task 1/3.)
