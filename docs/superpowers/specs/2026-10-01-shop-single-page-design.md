# Shop một trang + Remove Ads — Thiết kế

> Trạng thái 2026-10-01: **đã duyệt thiết kế** (brainstorming), bước kế là plan. Nhánh `feat/shop-single-page`.

## 1. Mục tiêu

Shop kiểu Beads Drop (`D:\Arrow-drop`, productName `BeadsDrop`): **một trang cuộn**, banner **Remove Ads** trên cùng,
lưới 3 cột gói coin. Khác Beads Drop: giữ 4 gói combo tiền thật và giữ nguyên tầng IAP thật đã có (Unity IAP 5.4.3,
`AddOnce`, khởi tạo store lúc boot, giá bản địa, Restore iOS). Beads Drop không có IAP thật — chỉ lấy giao diện và
sản phẩm Remove Ads.

## 2. Quyết định đã chốt

| # | Chủ đề | Chốt |
|---|---|---|
| 1 | Bố cục | Bỏ 2 tab. Header cố định (ô coin · tiêu đề · nút đóng) + một vùng cuộn dọc |
| 2 | Thứ tự trong vùng cuộn | Banner Remove Ads → 4 ô combo → lưới 3 cột 6 gói coin → nút Restore (chỉ iOS) |
| 3 | Gói combo | Giữ, mỗi gói một ô ngang full chiều rộng: icon gói, tên, hàng quà, nút giá |
| 4 | Remove Ads | id `remove_ads`, `NonConsumable`, giá dự phòng `4.99 $`, không tặng coin; mua xong banner ẩn, không mua lại được |
| 5 | Phạm vi No-Ads | Chỉ cờ + service (`INoAdsService`). Chưa chặn ads — game chưa có ads; rewarded ad sau này **không** bị chặn |
| 6 | Hàng quà trong ô combo | Icon + "x5"; tim vô hạn hiện thời lượng ("1h", "30m"). Icon theo loại quà cấu hình trong catalog |
| 7 | Dựng prefab | Viết lại menu `WordStack/Setup/Build Shop` để dựng bố cục mới với sprite thật. Chạy lại menu = mất chỉnh tay |
| 8 | Gắn Remove Ads | Mở rộng `ShopService` (chung luồng mua/xác nhận); không tách service IAP riêng — Unity IAP chỉ một bên nhận đơn |

## 3. Dữ liệu

**`ShopProductIds`** (`Assets/_Game/Shop/ShopProductIds.cs`): thêm `RemoveAds = "remove_ads"` và hằng cho 4 id combo
đang có trong catalog (`special_offer`, `beginner`, `medium`, `high`) — hiện thiếu nên `ShopCatalogAssetTests` lệch.
Id không đổi sau lần build store đầu tiên.

**`ShopCatalog`** (`SO_ShopCatalog.cs`, asset `Assets/_Game/Content/SO_ShopCatalog.asset`):
- `RemoveAdsDefinition RemoveAds` — `ProductId`, `PriceLabelFallback`, `Icon`, `Title` ("Remove ads"), `Subtitle`
  ("Mua một lần, giữ mãi").
- `List<RewardIcon> RewardIcons` — `{ ResourceType Type; Sprite Icon; }`. `Coin` dùng icon coin; loại thiếu icon thì
  ô quà chỉ hiện chữ (tên loại + số).
- `IShopCatalog` thêm `RemoveAds` và `TryGetRewardIcon(ResourceType, out Sprite)`.
- `CoinBundles` giữ nguyên (6 gói coin + 4 combo, combo là gói có `Items`).

**Lưu cờ:** `NoAdsData { int SchemaVersion; bool Owned; }`, save domain `"noads"` trên `JsonFileStorage`
(`GameSaveInstaller`), ghi ngay khi đổi.

## 4. Service

**`INoAdsService`** (mới, `Assets/_Game/Shop/Services/`):
- `bool IsNoAds`, `ReadOnlyReactiveProperty<bool>` (R3) hoặc event báo đổi.
- `void Grant()` — bật cờ + `SaveImmediate`; gọi lại khi đã bật là no-op.
- Không có `Revoke` (hoàn tiền xử lý sau, ngoài phạm vi).

**`ShopService`**:
- `InitializeStore`: đăng ký `RemoveAds.ProductId` là `IapProductKind.NonConsumable`; mọi gói trong `CoinBundles` vẫn
  `Consumable`.
- `Fulfill(productId, txId)`: nếu là `remove_ads` → `NoAds.Grant()` và trả `true` (không qua `AddOnce`, không cần
  coin > 0; trao lại nhiều lần an toàn vì cờ đã bật). Gói khác giữ nguyên luồng cũ.
- Đồng bộ quyền sở hữu: sau khi store khởi tạo xong và sau `RestorePurchases`, nếu `IIAPService.IsOwned("remove_ads")`
  → `NoAds.Grant()`. Cài lại game vẫn giữ quyền (Android tự trả đơn lúc khởi tạo; iOS qua nút Restore).
- API mua: `PurchaseProduct(string productId)` dùng chung cho gói coin, combo và Remove Ads (đổi tên từ
  `PurchaseCoinBundle`; cập nhật mọi chỗ gọi). Remove Ads khi đã sở hữu → trả mã `AlreadyOwned`, không gọi store.
- `GetPriceLabel` nhận cả `remove_ads`.
- Analytics `iap_purchase` như gói khác.

## 5. Giao diện

**`ShopPopup`** (`Assets/_Game/UI/Popups/ShopPopup.cs`, prefab `Assets/_Shared/Prefab/Popup/ShopPopup.prefab`,
address `ShopPopup` giữ nguyên):
- Bỏ `_coinTabButton`, `_itemTabButton`, `_coinTabRoot`, `_itemTabRoot`, `_itemGridRoot`, `_itemCellPrefab` cũ.
- Header cố định: ô coin (`_coinCounterText`), tiêu đề, nút đóng.
- `ScrollRect` dọc, `Content` có `VerticalLayoutGroup` + `ContentSizeFitter`, con theo thứ tự:
  `RemoveAdsBanner` → `ComboList` (VerticalLayoutGroup) → `CoinGrid` (GridLayoutGroup 3 cột cố định) → `RestoreButton`.
- `RemoveAdsBanner` ẩn khi `IsNoAds` (cả lúc mở popup lẫn ngay sau khi mua).
- Khoá mua khi đang có giao dịch (giữ cơ chế `_isPurchasing` + chặn đóng popup).
- Mua thành công: ô coin header nảy nhẹ (LitMotion punch scale), ô vừa mua sáng lên thoáng qua. Không popup thưởng.

**Ô combo** — `ShopComboCellView` (mới) + prefab `ShopComboCell.prefab`: icon gói, tên (`Title`), hàng quà, nút giá.
Hàng quà = `ShopRewardItemView` (mới, prefab `ShopRewardItem.prefab`): icon + text. Coin của gói (`Coins`) là món
đầu tiên. Định dạng số: `x5`; coin `2000`; tim vô hạn theo phút → `"1h"` nếu chia hết 60, ngược lại `"30m"`.

**Ô coin** — giữ `ShopCoinCellView` + `ShopCoinCell.prefab`, chỉ dùng cho gói không có `Items`. Badge Popular /
Best value như cũ.

**Banner Remove Ads** — `ShopRemoveAdsView` (mới) trong `ShopPopup.prefab`: icon (`Icon No Ads (big).png`), tiêu
đề, phụ đề, nút giá.

**Menu `Build Shop`** (`Assets/_Game/Editor/ShopSetup.cs`): `BuildPrefabs` dựng lại `ShopPopup`, `ShopCoinCell`,
`ShopComboCell`, `ShopRewardItem` theo cấu trúc trên, gắn sprite có sẵn (`Assets/_Game/Art/UI_New/Shop/*`,
`Icon No Ads (big).png`) và nối mọi field. `EnsureCatalog` điền thêm `RemoveAds` và `RewardIcons` (sprite booster/tim
có sẵn) khi còn trống. `WireInstaller`, `RegisterAddress` giữ nguyên.

## 6. Kiểm thử

EditMode (`WordStack.Meta.Tests`):
- `ShopServiceTests`: `remove_ads` đăng ký `NonConsumable`, gói khác `Consumable`; `Fulfill(remove_ads)` bật cờ, không
  cộng coin, gọi lại vẫn `true`; mua khi đã sở hữu → `AlreadyOwned`, không gọi store; đồng bộ `IsOwned` sau
  khởi tạo và sau Restore; combo và gói coin không đổi hành vi.
- `NoAdsServiceTests`: `Grant` bật cờ, lưu ngay, đọc lại từ save; `Grant` lần hai no-op.
- `ShopCatalogAssetTests`: khớp hai chiều `ShopProductIds` ↔ catalog (gồm combo + `remove_ads`); `RemoveAds` có
  fallback + icon; mọi `ResourceType` dùng trong `Items` có icon.
- Định dạng hàng quà (thời lượng tim vô hạn, `x5`).

Play mode (stub store):
1. Mở shop: banner Remove Ads, 4 combo có tên + hàng icon, lưới coin, cuộn mượt; header không cuộn.
2. Mua gói coin / combo: coin và item cộng đúng, ô coin nảy.
3. Mua Remove Ads: banner ẩn; mở lại shop vẫn ẩn; khởi động lại game vẫn ẩn.
4. Đang mua: không bấm được ô khác, không đóng được popup.

## 7. Ngoài phạm vi

- Chặn interstitial / banner ads theo cờ No-Ads (làm cùng lúc tích hợp SDK ads).
- Mở shop từ luồng thiếu coin (`NotEnoughGoldPopup` chưa có prefab).
- Popup thưởng sau khi mua, xác thực hoá đơn, hoàn tiền (revoke No-Ads).
