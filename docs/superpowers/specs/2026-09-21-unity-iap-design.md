# Nối store thật cho Shop — Unity IAP

Ngày: 2026-09-21 · Trạng thái: **bản nháp chờ duyệt** · Nhánh: `feat/unity-iap` (tách từ `main` `61d3e59`)

## 1. Mục tiêu

Shop của game đã đủ mọi tầng (`ShopService`, `SO_ShopCatalog`, `ShopPopup`, test) nhưng đang bán bằng `StubIAPService` — luôn báo "mua thành công", không gọi store nào. Bản này thay mảnh đó bằng Unity IAP thật để gói coin bán được bằng tiền thật trên Google Play (và iOS sau này), **không đổi** phần còn lại của shop.

Tham chiếu: hệ IAP của dự án Cooking (`D:\cooking-cozy`, Gley EasyIAP + `HandleIAP`). Bản này lấy kinh nghiệm vận hành của nó, **không lấy code**: Cooking dùng singleton + listener đăng ký lúc bấm nút, không ghép được với Reflex DI và `Awaitable` ở đây; và nó có hai lỗi mà bản này phải tránh ngay từ đầu (mục 2, quyết định 3 và 5).

Nguyên tắc chi phối:
- **Tiền thật đã trừ thì coin phải tới**, kể cả khi app bị tắt giữa chừng, popup đã đóng, hay máy mất mạng ngay sau khi thanh toán.
- **Một chỗ duy nhất biết Unity IAP là gì.** Mọi `using UnityEngine.Purchasing` nằm trong đúng một file adapter. `ShopService`, UI và test không thấy SDK.
- **Editor và build nội bộ vẫn chạy được không cần store** — `StubIAPService` ở lại, chọn bằng cờ trong installer.

## 2. Quyết định đã chốt

| # | Câu hỏi | Chốt | Lý do |
|---|---|---|---|
| 1 | Dùng plugin bọc (Gley EasyIAP như Cooking) hay Unity IAP trực tiếp? | **Unity IAP trực tiếp**, một adapter `UnityIAPService : IIAPService`. | `IIAPService` đã là lớp bọc. Gley thêm một lớp nữa + enum sinh tự động + asset trả phí, không mua được gì thêm. |
| 2 | Phiên bản package? | **`com.unity.purchasing` 4.12.2** — bản Cooking đang chạy thật. Bước đầu tiên của plan kiểm nó resolve và compile trên Unity 6000.3.8f1; nếu Unity ép lên 5.x thì chỉ file adapter đổi API. | API 4.x (`IDetailedStoreListener`) đã biết rõ. Rủi ro được nhốt trong một file nhờ quyết định 1. |
| 3 | Ai cộng coin, lúc nào? | **Adapter nhận giao dịch → gọi `IIapFulfillment.Fulfill` → trao + lưu xong mới `ConfirmPendingPurchase`.** `ProcessPurchase` luôn trả `Pending`. `ShopService` là bên hiện thực `IIapFulfillment`; nó **không còn** cộng coin sau `await Purchase`. | Bản hiện tại cộng coin sau `await`: app chết giữa hai dòng là mất tiền của user. Giao dịch chưa Confirm được store gửi lại ở lần mở app sau — nên việc trao thưởng phải chạy được khi **không có popup nào mở**. (Cooking mắc lỗi này: listener bị huỷ khi đóng popup.) |
| 4 | Chống trao hai lần khi store gửi lại? | **Sổ giao dịch đã trao** (`IapLedgerData`, domain save riêng): lưu `transactionID` đã xử lý, giữ 200 mục gần nhất. Thứ tự: cộng coin → ghi sổ → `SaveAll()` → Confirm. Gặp lại id đã có trong sổ thì chỉ Confirm. | Hai domain save không ghi nguyên tử được. Chọn thứ tự để cửa sổ lỗi (chết giữa "cộng coin" và "ghi sổ") nghiêng về phía **user được lợi** (trao hai lần), không phải mất tiền. |
| 5 | Xác thực hoá đơn? | **Có, nhưng ở pha 2.** Pha 1 đặt sẵn điểm móc `IReceiptValidator` (mặc định chấp nhận hết, log cảnh báo). Pha 2 bật `CrossPlatformValidator` khi có khoá Google Play. | File tangle sinh từ public key của app trên Play Console — app chưa tồn tại trên Console (`applicationIdentifier` còn là `com.DefaultCompany.2D-URP`). Cooking sinh tangle rồi để `useReceiptValidation: 0`; bản này ghi rõ là việc chưa làm thay vì làm nửa vời. |
| 6 | Giá hiển thị? | Thêm `string GetLocalizedPrice(string productId)` vào `IIAPService`; trả `null` khi chưa có. `ShopCoinCellView` dùng giá store nếu có, không thì `PriceLabelFallback`. | Giá do store quyết theo vùng (49.000₫ / $1.99). Fallback đã có sẵn đúng cho việc này. |
| 7 | Product id? | **Giữ nguyên** 6 id trong `ShopProductIds` (`coins_1000` … `coins_100000`), loại Consumable, dùng chung Android và iOS. | Chưa publish nên chưa bị khoá, nhưng đã nhất quán với test và catalog. Không cần quy ước `com.<app>.<gói>` của Cooking — Google chỉ đòi chữ thường, số, `.` và `_`. |
| 8 | Khởi tạo store lúc nào? | **Ở `BootState`**, không đợi mở Shop. Không chặn boot: init chạy nền, thất bại thì shop hiện giá fallback và nút mua báo `StoreUnavailable`. | Giao dịch Pending chỉ được gửi lại sau khi init. Đợi user mở shop mới init là user trả tiền hôm qua mà hôm nay không vào shop thì vẫn chưa có coin. |
| 9 | Chọn stub hay store thật? | Cờ `[SerializeField] bool _useRealStore` trên `ShopInstaller` + luôn dùng stub trong Editor khi cờ tắt. Build phát hành phải bật; test config canh việc này (mục 5). | "Đổi 1 dòng trước khi phát hành" là thứ sẽ bị quên. Stub phát coin miễn phí. |
| 10 | Restore + non-consumable (No-Ads)? | `RestorePurchases` nối thật (iOS bắt buộc có nút). **Chưa thêm sản phẩm No-Ads** — game chưa có quảng cáo. `IsOwned` đọc từ store. | YAGNI. Khi có quảng cáo, thêm một id Non-Consumable vào catalog là đủ; adapter đã hỗ trợ. |

## 3. Thiết kế

### 3.1 Hợp đồng — `Assets/_StudioSDK/Services/IIAPService.cs`

```csharp
public enum IapProductKind { Consumable, NonConsumable }

public readonly struct IapProduct
{
    public readonly string Id;
    public readonly IapProductKind Kind;
    public IapProduct(string id, IapProductKind kind) { Id = id; Kind = kind; }
}

/// Bên trao thưởng. Trả true = đã trao VÀ đã lưu xuống đĩa → adapter mới được Confirm với store.
/// Trả false = chưa trao được (thiếu service, id lạ) → giao dịch ở lại Pending, store gửi lại lần sau.
public interface IIapFulfillment
{
    bool Fulfill(string productId, string transactionId);
}

public interface IIAPService
{
    bool IsReady { get; }
    Awaitable<bool> Initialize(IReadOnlyList<IapProduct> products, IIapFulfillment fulfillment);
    Awaitable<bool> Purchase(string productId);   // true = giao dịch đã được TRAO và Confirm
    Awaitable RestorePurchases();
    bool IsOwned(string productId);
    string GetLocalizedPrice(string productId);   // null khi chưa init / store không có id này
}
```

`Initialize` gọi nhiều lần là an toàn (lần sau trả ngay kết quả lần đầu).

### 3.2 Adapter — `Assets/_Game/Shop/Services/Impl/UnityIAPService.cs`

File **duy nhất** được `using UnityEngine.Purchasing`. Hiện thực `IIAPService` + `IDetailedStoreListener`.

| Sự kiện Unity IAP | Hành vi |
|---|---|
| `OnInitialized` | Giữ `IStoreController` / `IExtensionProvider`; `IsReady = true`; hoàn tất `Awaitable` của `Initialize`. |
| `OnInitializeFailed` | Log lý do; `Initialize` trả `false`; `IsReady` giữ `false`. Không ném. |
| `ProcessPurchase(args)` | (1) `IReceiptValidator.IsValid(receipt)` — sai → log, **Confirm để store thôi gửi lại**, không trao, hoàn tất `Purchase` = `false`. (2) `fulfillment.Fulfill(id, transactionID)` — `true` → `ConfirmPendingPurchase` + hoàn tất `Purchase` = `true`; `false` → để Pending, hoàn tất `Purchase` = `false`. **Luôn `return Pending`.** |
| `OnPurchaseFailed` | Log `PurchaseFailureDescription`; hoàn tất `Purchase` = `false`. User huỷ (`UserCancelled`) log mức Info, còn lại Warn. |

Quy tắc:
- Một lần mua tại một thời điểm: `Purchase` khi đang có giao dịch dở trả `false` ngay.
- Giao dịch tới khi **không có** `Purchase` nào đang chờ (store gửi lại lúc init) vẫn đi đúng đường `ProcessPurchase` → `Fulfill` → Confirm. Đây là ca quyết định 3 nhắm tới.
- `GetLocalizedPrice` = `product.metadata.localizedPriceString`; rỗng thì `null`.
- `RestorePurchases`: iOS gọi `IAppleExtensions.RestoreTransactions`; Android hoàn tất ngay (Google tự khôi phục lúc init).
- Trước `UnityPurchasing.Initialize` phải `await UnityServices.InitializeAsync()` (Unity IAP 4.x cảnh báo nếu thiếu). Thất bại bước này **không** chặn init store — chỉ log.

### 3.3 `ShopService` hiện thực `IIapFulfillment`

- Thêm phụ thuộc `ISaveManager` (tuỳ chọn như các service khác — vắng thì `Fulfill` trả `false`, không trao mà không lưu được).
- `Fulfill(productId, transactionId)`:
  1. `transactionId` đã có trong `IapLedgerData` → trả `true` (đã trao rồi, cho Confirm).
  2. Không tìm thấy gói trong catalog, hoặc thiếu `ICurrencyService` → log Warn, trả `false`.
  3. `_currency.Add(bundle.Coins)` → thêm `transactionId` vào sổ (cắt còn 200 mục mới nhất) → `_save.SaveAll()` → trả `true`.
- `PurchaseCoinBundle(productId)`: giữ các kiểm tra trước khi gọi store (`UnknownProduct`, `StoreUnavailable` — thêm điều kiện `!_iap.IsReady`), rồi `await _iap.Purchase(productId)`. **Bỏ dòng `_currency.Add`** — coin đã được cộng trong `Fulfill`. `CoinsGranted` lấy từ `bundle.Coins` khi thành công.
- `IShopService` thêm `string GetPriceLabel(string productId)`: giá store nếu có, không thì `PriceLabelFallback`. `ShopCoinCellView` gọi hàm này thay vì tự đọc fallback.
- `IShopService` thêm `Awaitable InitializeStore()`: gom `CoinBundles` thành `IapProduct[]` (Consumable) và gọi `_iap.Initialize(products, this)`.

`IapLedgerData` (`Assets/_Game/Shop/IapLedgerData.cs`): `public List<string> ProcessedTransactionIds = new();` — đăng ký với `ISaveManager` trong `ShopInstaller` cùng kiểu các domain khác (storage giống `CurrencyData`).

### 3.4 `StubIAPService`

Cập nhật theo hợp đồng mới: `Initialize` giữ `fulfillment`, `IsReady = true`; `Purchase` gọi `fulfillment.Fulfill(id, "stub-" + Guid)` rồi trả kết quả của nó; `GetLocalizedPrice` trả `null` (UI rơi về fallback). Vẫn log Warn "GIẢ LẬP".

### 3.5 Đi dây

- `ShopInstaller`: cờ `_useRealStore`; `#if UNITY_EDITOR` mặc định stub. Đăng ký `IapLedgerData` với `ISaveManager`. Truyền `ISaveManager` vào factory của `ShopService`.
- `BootState`: sau khi save system sẵn sàng, resolve `IShopService` và gọi `InitializeStore()` **không await** (bọc try/catch, log lỗi). Đây cũng là chỗ khiến `ShopService` — vốn đăng ký Lazy — được dựng từ lúc boot.
- `ShopPopup`: nút mua tắt khi đang có giao dịch dở; mã `StoreUnavailable` hiện thông báo "Cửa hàng chưa sẵn sàng, thử lại sau". Thêm nút Restore chỉ hiện trên iOS.
- asmdef `WordStack.Meta` thêm reference `Unity.Purchasing`, `Unity.Purchasing.Stores`, `Unity.Services.Core`. `compilecheck.sh` target `meta` thêm các DLL này theo đúng cách đang mượn `Library/ScriptAssemblies`.

### 3.6 Analytics

Trong `Fulfill` lần đầu trao thành công (không phải ca "đã có trong sổ"): `IAnalyticsService.LogEvent("iap_purchase", { product_id, coins })` nếu service có mặt. Doanh thu do SDK attribution tự ghi khi được tích hợp — ngoài phạm vi.

## 4. Test

`ShopServiceTests` (EditMode, đã có) — sửa `FakeIap` theo hợp đồng mới (giữ `fulfillment`, `Purchase` gọi nó khi `Accept`), rồi thêm:

| Test | Kiểm gì |
|---|---|
| `Fulfill_GiaoDichMoi_CongCoin_GhiSo_VaLuu` | Coin tăng đúng, id vào sổ, `SaveAll` được gọi, trả `true` |
| `Fulfill_GiaoDichDaCoTrongSo_KhongCongLan2_VanTraTrue` | Store gửi lại → không trao hai lần nhưng vẫn cho Confirm |
| `Fulfill_GoiLaHoacThieuVi_TraFalse_KhongGhiSo` | Không trao được thì để Pending |
| `Fulfill_KhongCanPurchaseDangCho` | Gọi `Fulfill` trực tiếp (mô phỏng store gửi lại lúc boot) vẫn cộng coin |
| `Fulfill_SoVuot200_CatMucCuNhat` | Sổ không phình vô hạn |
| `PurchaseCoinBundle_ThanhCong_KhongCongCoinLan2` | Coin chỉ tăng **một lần** (trong `Fulfill`), không thêm sau `await` |
| `PurchaseCoinBundle_StoreChuaSanSang_TraStoreUnavailable_KhongGoiStore` | `IsReady == false` |
| `GetPriceLabel_CoGiaStore_DungGiaStore` / `_KhongCo_DungFallback` | Quyết định 6 |
| 4 test mua cũ | Vẫn xanh sau khi đổi cơ chế |

Test config (`WordStack.Meta.Tests`, EditMode): `ShopInstaller` trên `ProjectScope.prefab` có `_useRealStore == true` **khi** define `RELEASE_BUILD` bật — canh quyết định 9. (Không có define thì test bỏ qua.)

Adapter `UnityIAPService` không unit-test (toàn lời gọi SDK); nó được kiểm bằng mục 5.

Cổng máy: `./compilecheck.sh` 3/3 + toàn bộ EditMode xanh.

## 5. Nghiệm thu

**Làm được ngay, không cần Play Console** — Unity IAP có *fake store* trong Editor:
1. Bật `_useRealStore` trong Editor, Play, mở Shop: giá hiện chuỗi fake store thay cho fallback.
2. Mua một gói → hộp thoại fake store → Buy: coin tăng đúng một lần, sổ có 1 id.
3. Mua → Cancel: coin không đổi, popup không kẹt, nút mua bấm lại được.
4. Tắt Play giữa lúc hộp thoại đang mở, Play lại: không sập, không trao nhầm.
5. Tắt `_useRealStore`: stub chạy như cũ.

**Cần tiền đề (mục 6) mới làm được:** mua thật bằng tài khoản license tester trên máy Android; tắt app ngay sau khi thanh toán rồi mở lại — coin phải tới ở lần boot sau mà không cần mở Shop; mua khi offline.

## 6. Tiền đề phía người vận hành (không phải việc của code)

| Việc | Vì sao chặn |
|---|---|
| Đặt `applicationIdentifier` Android thật (đang là `com.DefaultCompany.2D-URP`) | Play Console gắn sản phẩm theo package name |
| Tạo app trên Play Console + 6 sản phẩm in-app đúng id ở quyết định 7, đặt giá | Store thật không trả sản phẩm nào nếu chưa tạo |
| Link project với Unity Gaming Services (`cloudProjectId` đang trống) | `UnityServices.InitializeAsync` cần project id; thiếu thì IAP vẫn chạy nhưng log cảnh báo mỗi lần mở |
| Tải một bản build lên internal testing + thêm license tester | Google chỉ cho mua thử trên bản đã qua Console |
| (Pha 2) Lấy public key → sinh `GooglePlayTangle` | Xác thực hoá đơn |

Pha 1 code + test + nghiệm thu fake store **không phụ thuộc** bảng này.

## 7. Ngoài phạm vi

- Gói combo (coin + booster + tim) kiểu Starter/Chef của Cooking, giá gạch ngang/%-sale, banner gói. Cần thiết kế kinh tế riêng; `CoinBundleDefinition` hiện chỉ chở coin.
- Sản phẩm No-Ads và toàn bộ quảng cáo.
- Subscription.
- Xác thực hoá đơn phía server.
- Pha 2 của quyết định 5 (bật `CrossPlatformValidator`) — làm khi có khoá.

## 8. Thứ tự triển khai đề xuất

1. Cài `com.unity.purchasing` 4.12.2, xác nhận compile trên 6000.3.8f1, nối DLL vào `compilecheck.sh`. **Dừng báo nếu Unity ép 5.x.**
2. Hợp đồng mới (`IIAPService`, `IIapFulfillment`, `IapProduct`) + cập nhật `StubIAPService` và `FakeIap` — toàn bộ test cũ xanh lại.
3. TDD `ShopService.Fulfill` + `IapLedgerData` + bỏ cộng coin sau `await` + `GetPriceLabel`/`InitializeStore`.
4. `UnityIAPService` + `IReceiptValidator` mặc định.
5. Đi dây: `ShopInstaller` (cờ, ledger, save), `BootState`, `ShopCoinCellView`, `ShopPopup`.
6. Nghiệm thu fake store (mục 5 phần đầu).
