# Shop Go-Live (CategorySort) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Người chơi bấm nút Shop ở Main Menu là mở được ShopPopup, mua gói coin (stub trong Editor, Unity IAP thật khi bật cờ) và mua item bằng coin.

**Architecture:** Code shop đã có gần đủ, nằm ở hai chỗ: `feat/safe-area` (hiện tại) có `ShopService`, `ShopPopup`, test; nhánh `origin/feat/unity-iap` chưa merge có `IIapFulfillment`, `UnityIAPService`, ví `AddOnce`, khởi tạo store lúc boot. Phần còn thiếu là **đi dây và asset**: merge nhánh IAP, gắn `ShopInstaller` + tạo `SO_ShopCatalog`, dựng prefab `ShopPopup` và đăng ký Addressables. **Không** port module `Logos.Shop` của Arrow Drop sang (xem phụ lục A).

**Tech Stack:** Unity 6000.3.8f1, Reflex DI, R3, Addressables 2.3.1, `com.unity.purchasing` 4.12.2 (do nhánh IAP thêm), NUnit EditMode.

## Global Constraints

- Product id cố định, không đổi sau lần build store đầu tiên: `coins_1000`, `coins_5000`, `coins_10000`, `coins_25000`, `coins_50000`, `coins_100000` (`Assets/_Game/Shop/ShopProductIds.cs`).
- Chỉ `Assets/_Game/Shop/Services/Impl/UnityIAPService.cs` được `using UnityEngine.Purchasing`.
- Build phát hành phải có `ShopInstaller._useRealStore == true`. `StubIAPService` phát coin miễn phí.
- UIManager load popup bằng `Addressables.InstantiateAsync(type.Name)`, nên address của prefab phải đúng bằng tên class: `ShopPopup`.
- Chạy compile ngoài Editor: `./compilecheck.sh`. Chạy test trong Editor: Window ▸ General ▸ Test Runner ▸ EditMode ▸ `WordStack.Meta.Tests`.

## Hiện trạng (đã kiểm ngày 2026-09-26)

| Thành phần | Có trên `feat/safe-area`? | Ghi chú |
|---|---|---|
| `ShopService`, `IShopService`, `StubIAPService`, `ShopCatalog` (SO class), `ShopInstaller` | Có | Cộng coin **sau** `await Purchase`: app chết giữa hai dòng là mất tiền của user |
| `ShopPopup`, `ShopCoinCellView`, `ShopItemCellView` (script) | Có | |
| `MainMenuScreen._shopButton` → `AppFlowContext.ShowShopPopupAsync()` | Có | |
| `IIapFulfillment`, `UnityIAPService`, `AddOnce`, init store ở `BootState`, giá bản địa, Restore | **Chỉ trên `origin/feat/unity-iap`** | Merge sẽ conflict 3 file AppFlow, conflict thuần cộng tham số |
| `ShopInstaller` gắn trên `Assets/Prefabs/ProjectScope.prefab` | **Không** | |
| `Assets/_Game/Content/SO_ShopCatalog.asset` | **Không** | |
| Prefab `ShopPopup` + cell prefab + address Addressables `ShopPopup` | **Không** | Bấm Shop lúc này ném `InvalidKeyException` và AppFlow log lỗi |
| Art | Có | `Assets/_Game/Art/UI_New/Shop/*` (6 Coin, 5 Bundle, 4 Tag), `Pop-Up/Title Main Shop.png` |

## File Structure

| File | Việc |
|---|---|
| `Assets/_Game/AppFlow/AppFlowContext.cs` | Sửa: gỡ conflict merge (giữ cả tham số revive/currency lẫn `shopService`) |
| `Assets/_Game/AppFlow/WordStackAppFlowManager.cs` | Sửa: gỡ conflict merge |
| `Assets/_Game/AppFlow/Installers/AppFlowInstaller.cs` | Sửa: gỡ conflict merge |
| `Assets/_Game/Gameplay/Tests/ShopCatalogAssetTests.cs` | Tạo: canh `SO_ShopCatalog.asset` khớp `ShopProductIds` và `SO_TransactionCatalog` |
| `Assets/_Game/Content/SO_ShopCatalog.asset` | Tạo (qua menu wiring), rồi điền dữ liệu |
| `Assets/Prefabs/ProjectScope.prefab` | Sửa (qua menu wiring): thêm `ShopInstaller` |
| `Assets/_Shared/Prefab/Popup/ShopPopup.prefab`, `ShopCoinCell.prefab`, `ShopItemCell.prefab` | Tạo trong Editor |
| `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset` | Sửa (Editor): thêm address `ShopPopup` |

---

### Task 1: Merge nhánh Unity IAP vào nhánh làm việc

**Files:**
- Modify: `Assets/_Game/AppFlow/AppFlowContext.cs`, `Assets/_Game/AppFlow/WordStackAppFlowManager.cs`, `Assets/_Game/AppFlow/Installers/AppFlowInstaller.cs`

**Interfaces:**
- Produces: `IShopService.InitializeStore()`, `IShopService.GetPriceLabel(string)`, `IShopService.RestorePurchases()`, `AppFlowContext.InitializeStoreInBackground()` (được `BootState` gọi), `ShopInstaller.UseRealStore`.

- [ ] **Step 1: Dọn working tree và tạo nhánh**

Trên `feat/safe-area` đang có thay đổi chưa commit (`Main.unity`, font, `Tile.prefab`, `Build.rar`…). Commit hoặc stash trước, tuỳ chủ nhánh quyết. Sau đó:

```bash
git fetch origin
git switch -c feat/shop-go-live
git merge origin/feat/unity-iap
```

Expected: `CONFLICT (content)` ở đúng 3 file `AppFlowContext.cs`, `AppFlowInstaller.cs`, `WordStackAppFlowManager.cs`. `Packages/manifest.json` và `compilecheck.sh` tự merge được.

- [ ] **Step 2: Gỡ conflict `AppFlowContext.cs`, giữ cả hai phía**

Khối field:

```csharp
        private readonly LogosMeta.Economy.ICurrencyService _currencyService;
        private readonly int _revivePrice;
        private readonly GameplayFlowAdapter _flowAdapter;
        private readonly int _reviveExtraMoves;
        private readonly LogosGame.Features.Shop.IShopService _shopService;
```

Cuối danh sách tham số constructor:

```csharp
            LogosMeta.Economy.ICurrencyService currencyService = null,
            int revivePrice = 0,
            GameplayFlowAdapter flowAdapter = null,
            int reviveExtraMoves = 0,
            LogosGame.Features.Shop.IShopService shopService = null)
```

Khối gán trong constructor, sau đó đến method khởi tạo store của nhánh IAP:

```csharp
            _currencyService = currencyService;
            _revivePrice = revivePrice;
            _flowAdapter = flowAdapter;
            _reviveExtraMoves = reviveExtraMoves;
            _shopService = shopService;
        }

        /// <summary>
        /// Khởi tạo store ngay lúc boot, KHÔNG chặn boot. Phải làm ở đây chứ không đợi mở
        /// Shop: giao dịch đã trả tiền mà chưa trao (app chết giữa chừng hôm trước) chỉ
        /// được store gửi lại sau bước này.
        /// </summary>
        public void InitializeStoreInBackground()
        {
            if (_shopService == null) return;
            InitializeStoreAsync();
        }

        private async void InitializeStoreAsync()
        {
            try
            {
                bool ready = await _shopService.InitializeStore();
                if (!ready) _logger.Warn("[AppFlow] Store chưa sẵn sàng — shop hiện giá dự phòng, nút mua báo StoreUnavailable.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[AppFlow] Lỗi khi khởi tạo store.");
            }
        }
```

- [ ] **Step 3: Gỡ conflict `WordStackAppFlowManager.cs`**

Tham số constructor:

```csharp
            LogosMeta.Economy.ICurrencyService currencyService = null,
            int revivePrice = 0,
            GameplayFlowAdapter flowAdapter = null,
            int reviveExtraMoves = 0,
            LogosGame.Features.Shop.IShopService shopService = null)
```

Lời gọi tạo context:

```csharp
                audioService, hapticService, heartService, currencyService, revivePrice,
                flowAdapter, reviveExtraMoves, shopService);
```

- [ ] **Step 4: Gỡ conflict `AppFlowInstaller.cs`**

```csharp
                        c.Resolve<LogosMeta.Economy.IHeartService>(),
                        c.Resolve<LogosMeta.Economy.ICurrencyService>(),
                        _revivePrice,
                        c.Resolve<GameplayFlowAdapter>(),
                        _reviveExtraMoves,
                        // Vắng khi ProjectScope chưa gắn ShopInstaller — boot vẫn chạy, chỉ không có store.
                        c.TryGetResolver<LogosGame.Features.Shop.IShopService>(out _)
                            ? c.Resolve<LogosGame.Features.Shop.IShopService>()
                            : null);
```

- [ ] **Step 5: Kiểm không còn marker và compile được**

```bash
git grep -n "^<<<<<<<\|^>>>>>>>" -- Assets
./compilecheck.sh
```

Expected: `git grep` không in gì. `compilecheck.sh` báo 0 lỗi cho cả 3 target.

- [ ] **Step 6: Mở Editor cho import `com.unity.purchasing` rồi chạy test**

Mở project bằng Unity 6000.3.8f1 và đợi Package Manager resolve `com.unity.purchasing` 4.12.2. Nếu Unity ép lên 5.x thì dừng lại báo: chỉ `UnityIAPService.cs` phải đổi API (spec `docs/superpowers/specs/2026-09-21-unity-iap-design.md`, quyết định 2).

Test Runner ▸ EditMode ▸ chạy `WordStack.Meta.Tests`.
Expected: `ShopServiceTests`, `CurrencyServiceTests` PASS. `ShopReleaseConfigTests` báo Ignored (chưa có define `RELEASE_BUILD`).

- [ ] **Step 7: Commit**

```bash
git add Assets/_Game/AppFlow
git commit -m "merge: bring Unity IAP shop work into shop go-live branch"
```

---

### Task 2: Gắn ShopInstaller và điền SO_ShopCatalog, có test canh dữ liệu

**Files:**
- Create: `Assets/_Game/Gameplay/Tests/ShopCatalogAssetTests.cs`
- Create (qua menu): `Assets/_Game/Content/SO_ShopCatalog.asset`
- Modify (qua menu): `Assets/Prefabs/ProjectScope.prefab`

**Interfaces:**
- Consumes: `LogosGame.Features.Shop.ShopCatalog` (`CoinBundles`, `ItemTransactionIds`), `LogosGame.Features.Currency.Transactions.TransactionCatalog.TryGet`, `ShopProductIds.*`, `WordStack.Meta.ShopInstaller`.
- Produces: asset `SO_ShopCatalog.asset` mà ShopPopup ở Task 3 đọc qua `IShopService`.

- [ ] **Step 1: Viết test (sẽ fail vì asset chưa tồn tại)**

```csharp
using LogosGame.Features.Currency.Transactions;
using LogosGame.Features.Shop;
using LogosMeta.Economy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Canh dữ liệu shop: gói nào trong ShopProductIds mà thiếu trong catalog là nút mua
    /// báo UnknownProduct; mã item mà không có trong SO_TransactionCatalog là tab Item
    /// lặng lẽ thiếu ô. Cả hai đều chỉ lộ ra khi người chơi bấm.
    /// </summary>
    public sealed class ShopCatalogAssetTests
    {
        private const string ShopCatalogPath = "Assets/_Game/Content/SO_ShopCatalog.asset";
        private const string TransactionCatalogPath = "Assets/_Game/Content/SO_TransactionCatalog.asset";
        private const string ProjectScopePath = "Assets/Prefabs/ProjectScope.prefab";

        private static readonly string[] ExpectedProducts =
        {
            ShopProductIds.Coins1000, ShopProductIds.Coins5000, ShopProductIds.Coins10000,
            ShopProductIds.Coins25000, ShopProductIds.Coins50000, ShopProductIds.Coins100000,
        };

        [Test]
        public void MoiProductId_CoGoiHopLeTrongCatalog()
        {
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            Assert.IsNotNull(catalog, "Chưa có " + ShopCatalogPath + " — chạy WordStack ▸ Setup ▸ Wire meta components.");

            foreach (string id in ExpectedProducts)
            {
                CoinBundleDefinition? found = null;
                foreach (CoinBundleDefinition b in catalog.CoinBundles)
                    if (b.ProductId == id) found = b;

                Assert.IsTrue(found.HasValue, $"Catalog thiếu gói '{id}'.");
                Assert.Greater(found.Value.Coins, 0, $"'{id}' có Coins <= 0.");
                Assert.IsFalse(string.IsNullOrEmpty(found.Value.PriceLabelFallback), $"'{id}' chưa có PriceLabelFallback.");
                Assert.IsNotNull(found.Value.Icon, $"'{id}' chưa gán Icon.");
            }

            Assert.AreEqual(ExpectedProducts.Length, catalog.CoinBundles.Count,
                "Catalog có gói ngoài ShopProductIds — thêm const vào ShopProductIds hoặc xoá gói.");
        }

        [Test]
        public void MoiMaItem_CoTrongTransactionCatalog()
        {
            ShopCatalog shop = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            TransactionCatalog tx = AssetDatabase.LoadAssetAtPath<TransactionCatalog>(TransactionCatalogPath);
            Assert.IsNotNull(shop, "Chưa có " + ShopCatalogPath);
            Assert.IsNotNull(tx, "Chưa có " + TransactionCatalogPath);
            Assert.Greater(shop.ItemTransactionIds.Count, 0, "Tab Item đang trống.");

            foreach (string id in shop.ItemTransactionIds)
                Assert.IsTrue(tx.TryGet(id, out TransactionDefinition _), $"'{id}' không có trong SO_TransactionCatalog.");
        }

        [Test]
        public void ProjectScope_CoShopInstaller_TroDungCatalog()
        {
            GameObject scope = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectScopePath);
            Assert.IsNotNull(scope, "Không thấy " + ProjectScopePath);

            ShopInstaller installer = scope.GetComponentInChildren<ShopInstaller>(true);
            Assert.IsNotNull(installer, "ProjectScope chưa gắn ShopInstaller.");

            var so = new SerializedObject(installer);
            Object assigned = so.FindProperty("_shopCatalog").objectReferenceValue;
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath), assigned,
                "ShopInstaller._shopCatalog chưa trỏ tới SO_ShopCatalog.asset.");
        }
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Test Runner ▸ EditMode ▸ `ShopCatalogAssetTests`.
Expected: cả 3 FAIL với thông báo "Chưa có Assets/_Game/Content/SO_ShopCatalog.asset" hoặc "ProjectScope chưa gắn ShopInstaller".

- [ ] **Step 3: Chạy menu wiring**

Menu **WordStack ▸ Setup ▸ Wire meta components**.
Expected: Console in `WIRE: thêm ShopInstaller vào ProjectScope.`, `WIRE: tạo Assets/_Game/Content/SO_ShopCatalog.asset (RỖNG …)` và `WIRE: gán SO_ShopCatalog vào ShopInstaller.`

- [ ] **Step 4: Điền `SO_ShopCatalog.asset` trong Inspector**

Tab Coin (`_coinBundles`). Giá là **đề xuất**, GD chốt; giá thật do store trả theo vùng.

| ProductId | Coins | PriceLabelFallback | Icon (`Assets/_Game/Art/UI_New/Shop/`) | Tag |
|---|---|---|---|---|
| coins_1000 | 1000 | 0.99 $ | Icon Shop Coin 1 | None |
| coins_5000 | 5000 | 3.99 $ | Icon Shop Coin 2 | None |
| coins_10000 | 10000 | 6.99 $ | Icon Shop Coin 3 | Popular |
| coins_25000 | 25000 | 14.99 $ | Icon Shop Coin 4 | None |
| coins_50000 | 50000 | 24.99 $ | Icon Shop Coin 5 | None |
| coins_100000 | 100000 | 39.99 $ | Icon Shop Coin 6 | BestValue |

Tab Item (`_itemTransactionIds`): `t_booster_shuffle`, `t_booster_magnet`, `t_booster_undo`, `t_heart` (khớp `SO_TransactionCatalog.asset`).

Trên `ProjectScope.prefab` ▸ `ShopInstaller`: để `_useRealStore` **tắt** (dev dùng stub).

- [ ] **Step 5: Chạy lại test**

Expected: `ShopCatalogAssetTests` 3/3 PASS, các test cũ vẫn PASS.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Gameplay/Tests/ShopCatalogAssetTests.cs Assets/_Game/Gameplay/Tests/ShopCatalogAssetTests.cs.meta Assets/_Game/Content/SO_ShopCatalog.asset Assets/_Game/Content/SO_ShopCatalog.asset.meta Assets/Prefabs/ProjectScope.prefab
git commit -m "feat(shop): wire ShopInstaller and fill SO_ShopCatalog, guarded by asset tests"
```

---

### Task 3: Dựng prefab ShopPopup và đăng ký Addressables

**Files:**
- Create: `Assets/_Shared/Prefab/Popup/ShopPopup.prefab`, `Assets/_Shared/Prefab/Popup/ShopCoinCell.prefab`, `Assets/_Shared/Prefab/Popup/ShopItemCell.prefab`
- Modify: `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset`

**Interfaces:**
- Consumes: script `ShopPopup` (field `_coinCounterText`, `_closeButton`, `_coinTabButton`, `_itemTabButton`, `_coinTabRoot`, `_itemTabRoot`, `_coinGridRoot`, `_coinCellPrefab`, `_itemGridRoot`, `_itemCellPrefab`, `_restoreButton`), `ShopCoinCellView` (`_icon`, `_coinsText`, `_priceText`, `_popularBadge`, `_bestValueBadge`, `_buyButton`), `ShopItemCellView` (`_nameText`, `_descriptionText`, `_priceText`, `_buyButton`).
- Produces: address `ShopPopup` mà `UIManager.LoadPopupAsync` tìm.

- [ ] **Step 1: Cell prefab `ShopCoinCell`**

Duplicate một ô có sẵn trong `BoosterPurchasePopup.prefab` làm khung, hoặc tạo mới gồm: Image nền, Image icon, TMP số coin, TMP giá nằm trên một Button, badge Popular (`Icon Shop Tag 1`) và badge BestValue (`Icon Shop Tag 2`), cả hai tắt sẵn. Gắn `ShopCoinCellView` lên root rồi kéo đủ 6 field. Lưu vào `Assets/_Shared/Prefab/Popup/ShopCoinCell.prefab`.

- [ ] **Step 2: Cell prefab `ShopItemCell`**

Tạo root gồm: TMP tên, TMP mô tả, TMP giá (coin), Button mua. Gắn `ShopItemCellView` rồi kéo đủ 4 field. Lưu thành `ShopItemCell.prefab`.

- [ ] **Step 3: Prefab `ShopPopup`**

Duplicate `BoosterPurchasePopup.prefab` để thừa hưởng khung popup, CanvasGroup, transition và SafeArea, rồi đổi tên thành `ShopPopup.prefab`. Xoá component `BoosterPurchasePopup` và gắn `ShopPopup`. Dựng các phần:
- Tiêu đề `Pop-Up/Title Main Shop.png`, TMP coin counter, nút Close.
- Hai nút tab Coin/Item, mỗi tab có một root (`GameObject`).
- Mỗi tab root chứa ScrollRect + GridLayoutGroup; content của nó chính là `_coinGridRoot` / `_itemGridRoot`.
- Nút Restore (script tự ẩn khi không phải iOS).

Kéo đủ 11 field, với `_coinCellPrefab` = `ShopCoinCell`, `_itemCellPrefab` = `ShopItemCell`.

- [ ] **Step 4: Đăng ký Addressables**

Window ▸ Asset Management ▸ Addressables ▸ Groups ▸ kéo `ShopPopup.prefab` vào `Default Local Group`, đặt address **đúng** `ShopPopup` (bỏ đường dẫn mặc định).

```bash
grep -n "m_Address: ShopPopup$" "Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset"
```

Expected: in đúng 1 dòng.

- [ ] **Step 5: Nghiệm thu trong Play Mode (Editor, stub store)**

Mở `Assets/Scenes/Main.unity` ▸ Play ▸ vào Main Menu, rồi kiểm:
1. Bấm Shop: popup mở, tab Coin có 6 ô đúng icon/giá fallback, badge Popular ở `coins_10000`, BestValue ở `coins_100000`. Console không có `InvalidKeyException`.
2. Bấm `coins_1000`: counter tăng đúng 1000, Console có dòng log `[StubIAPService]` (giả lập).
3. Sang tab Item: 4 ô. Nút nào có giá > số coin thì bị xám.
4. Mua `t_heart` khi đủ coin: coin giảm đúng giá, số tim tăng.
5. Đóng Shop rồi mở lại: không nhân đôi ô (popup được cache, `BuildOnce`).
6. Stop, Play lại: số coin còn nguyên (ví đã lưu).

- [ ] **Step 6: Commit**

```bash
git add Assets/_Shared/Prefab/Popup/ShopPopup.prefab* Assets/_Shared/Prefab/Popup/ShopCoinCell.prefab* Assets/_Shared/Prefab/Popup/ShopItemCell.prefab* "Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset"
git commit -m "feat(shop): ShopPopup prefab with coin/item cells, addressable as ShopPopup"
```

---

## Trước khi phát hành (không phải việc code, làm theo spec IAP mục 6)

- Tạo app trên Play Console. Đổi `applicationIdentifier` (hiện là `com.DefaultCompany.2D-URP`).
- Tạo 6 sản phẩm in-app đúng id ở Global Constraints, loại Consumable, và đặt giá.
- Bật `ShopInstaller._useRealStore`. Build với define `RELEASE_BUILD`, khi đó `ShopReleaseConfigTests` phải PASS.
- Test trên máy bằng license tester: mua, huỷ, tắt app ngay sau khi trả tiền rồi mở lại (coin phải về).

## Ngoài phạm vi

- **No-Ads**: game chưa có impl quảng cáo (`IAdService` mới là interface). Khi có, thêm một product NonConsumable vào catalog; adapter đã hỗ trợ.
- **Mở Shop từ popup "thiếu coin"**: `NotEnoughGoldPopup` chỉ bật lên trong gameplay (đang khoá input bàn chơi). Arrow Drop cũng cố tình không chuyển sang Shop giữa ván. Làm khi GD yêu cầu.
- **Xác thực hoá đơn** (`CrossPlatformValidator`): pha 2 của spec IAP, cần public key Play Console.

## Phụ lục A: So sánh với Arrow Drop

| Khía cạnh | Arrow Drop (`Logos.Shop`) | CategorySort | Chọn |
|---|---|---|---|
| Thanh toán | `IShopStore.Buy` + event `Purchased`, chỉ có `InstantStore` | `IIAPService` async + `IIapFulfillment` (nhánh IAP), có adapter Unity IAP thật | CategorySort |
| Chống trao trùng | Chỉ chặn cho No-Ads (`wallet.NoAds`) | `AddOnce(amount, transactionId)` nguyên tử cùng file ví | CategorySort |
| Kết quả mua | `void` | `ShopPurchaseResult` (Success / UnknownProduct / StoreDeclined / StoreUnavailable) | CategorySort |
| Giá | `priceLabel` tĩnh | Giá bản địa từ store, fallback về label | CategorySort |
| Nội dung | Gói coin + No-Ads | Tab Coin + tab Item (mua bằng coin qua `IPurchaseService`) + badge | CategorySort |
| UI | `ShopView` thuần, panel game bọc ngoài | `PopupBase<ShopPopupArgs>` + khoá bấm chồng khi đang mua | CategorySort |
| No-Ads | Có | Chưa (chưa có ads) | Ngoài phạm vi |
| Tính mang đi | Module riêng + asmdef, không phụ thuộc DI | Nằm trong `WordStack.Meta`, dựa vào Reflex/R3/LogosSDK | Arrow Drop |

Kết luận: port `Logos.Shop` sang đây là **lùi**. Nếu muốn một module shop dùng chung cho các game Logos, nên trích từ bản CategorySort sau khi go-live, không lấy từ Arrow Drop.
