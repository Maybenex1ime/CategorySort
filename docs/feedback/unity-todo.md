# Việc làm tay trong Unity

Code đã vào `main`; những bước dưới phải làm trong Editor (prefab / scene / asset).

Trạng thái: `[ ]` chưa làm · `[x]` xong. Cập nhật 2026-10-08 — mọi feedback build 2026-10-07 (#1–#12) đã xong.

## Còn lại

- [ ] **Coin animation — kiểm trong Play mode.** Prefab đã được nối thẳng trên đĩa lúc Unity tắt (2026-10-08), mở Unity
      lần đầu xem Console có báo lỗi prefab không, rồi kiểm:
  - Thắng màn → popup có ô coin (`Wallet/CoinArea`, bản sao ô coin HUD) hiện số trước thưởng → bấm Claim → coin bay
    từ số thưởng vào ô, ô đếm lên, popup đóng. Ô coin popup phải trùng chỗ ô coin HUD bên dưới — lệch thì chỉnh
    `Wallet/CoinArea` trong `CompletedPopup.prefab`.
  - Shop: mua gói coin → coin bay từ ô vừa mua về counter header, counter đếm lên.
  - Home: nút **Play** thở nhẹ (+ lắc nhẹ định kỳ — `SO_Button_CTA` bật cả wobble). Không động đậy → `UIIdlePulseDriver`
    không được inject `IUIAnimationService`.
  - Chỉnh cỡ / nhịp coin trên component `CoinFly` (root `CompletedPopup`, `Shop Panel`) nếu cần.
- [ ] Test Runner ▸ EditMode, chạy: `CountUpTextTests`, `CoinFlyTests`, `ShopScrollTests`, `SliderTapToggleTests`,
      `BoardFitTests`, `BoardSettleInputTests`, `WordStackGameplayViewModelTests`, `LevelCatalogOrderTests`.

## Đã xong

- [x] `.meta` của mọi script / test mới (`b9dea87`).
- [x] Feedback #1–#12 (chi tiết: [`2026-10-07-build-feedback.md`](2026-10-07-build-feedback.md)).
- [x] Coin animation đã nối prefab (2026-10-08):
  - `CompletedPopup.prefab`: `Wallet` (SafeAreaFitter) + `CoinArea` → `_coinBoxText`, `_coinBoxIcon`; `CoinFly` trên root → `_coinFly`.
  - `MainMenuScreen.prefab`: `CoinFly` trên `Shop Panel` → `ShopPopup._coinFly`; `_coinPacksTitle` = `Coin Packs`;
    `UIIdlePulseDriver` + `SO_Button_CTA` trên `Play Button`.
