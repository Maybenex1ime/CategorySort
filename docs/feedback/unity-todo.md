# Việc làm tay trong Unity

Code đã vào `main`; những bước dưới phải làm trong Editor (prefab / scene / asset). Chưa làm thì
game chạy như cũ — không có bước nào làm vỡ build.

Trạng thái: `[ ]` chưa làm · `[x]` xong.

## Chung

- [ ] Mở Unity cho nó sinh `.meta` rồi commit các file mới:
  - `Assets/_Game/UI/Common/` (folder) + `CountUpText.cs`, `CoinFly.cs`
  - `Assets/_Game/Board/Views/BoardFit.cs`
  - `Assets/_Game/Cheat/Views/CheatBoardSectionView.cs`
  - `Assets/_Game/Gameplay/Tests/CountUpTextTests.cs`, `CoinFlyTests.cs`, `ShopScrollTests.cs`
  - `Assets/_Game/Board/Tests/BoardFitTests.cs`
- [ ] Test Runner ▸ EditMode: chạy `CountUpTextTests`, `CoinFlyTests`, `ShopScrollTests`, `BoardFitTests`.

## Coin animation (spec `docs/superpowers/specs/2026-10-06-ui-coin-anim-design.md`)

- [ ] `CompletedPopup.prefab`: thêm ô coin (icon + TMP) phía trên popup.
  - Gán TMP vào `_coinBoxText`, icon vào `_coinBoxIcon`.
  - Thêm `CoinFly` lên root popup, gán sprite coin, kéo vào `_coinFly`.
- [ ] Shop panel trong `MainMenuScreen.prefab`: thêm `CoinFly` (đặt trên `Shop Panel` để coin vẽ đè lên lưới), gán sprite coin, kéo vào `ShopPopup._coinFly` (component nằm trên GameObject `Scroll`).
- [ ] Nút Play pulse:
  - **Create ▸ LogosSDK ▸ UI ▸ Button Feedback** → đặt tên `SO_Button_CTA`, tick **Idle Pulse Enabled** (scale 1.06, 0.9 s).
  - Thêm `UIIdlePulseDriver` lên nút **Play** ở Home, gán profile `SO_Button_CTA`.

## Board auto-fit — feedback #10 (spec `docs/superpowers/specs/2026-10-06-board-autofit-design.md`)

- [ ] `Main.unity` ▸ `BoardController` ▸ `hudBlockers`: kéo vào CoinArea, LevelBox, Settings Button, Progress Bar, Box BG, Booster Button (1), (2), (3).
- [ ] `GamePlayUIRoot .prefab`: thu nhỏ Booster Button (200 → khoảng 150) — hộp tự to ra theo.
- [ ] `GamePlayUIRoot .prefab`: đổi neo Progress Bar về `(0.5, 1)` (đang neo giữa + đẩy lên ~786, màn 4:3 bị ra ngoài mép trên).
- [ ] Soi 9:16, 20:9, 4:3 bằng dropdown screen size của Cheat; chỉnh `fitExtraBelow` / `fitPadding` trên `BoardController` nếu sát quá.

## Feedback #1 — bấm đâu trên công tắc Settings cũng đảo on/off

- [ ] Thêm component `SliderTapToggle` lên 7 GameObject `Slider` đang có `SliderHandleSprite`:
  - `PausePopup.prefab` ▸ `Pause UI/Settings/Notification Settings`, `(1)`, `(2)` ▸ `Slider` (3 cái)
  - `MainMenuScreen.prefab` ▸ `SafeArea/SetttingsPanel` — 4 công tắc
- [ ] File mới cần `.meta`: `Assets/_Game/UI/SliderTapToggle.cs`, `Assets/_Game/Gameplay/Tests/SliderTapToggleTests.cs`; chạy `SliderTapToggleTests`.

## Feedback #4 — đi tiếp trong lúc hộp đang nổ (spec `docs/superpowers/specs/2026-10-07-input-during-cascade-design.md`)

- [ ] File mới cần `.meta`: `Assets/_Game/Board/Tests/BoardSettleInputTests.cs`; chạy `BoardSettleInputTests` + `WordStackGameplayViewModelTests`.
- [ ] Kiểm `GameplayBlockInputOverlayView` (bật trong lúc cascade): phải trong suốt — nếu có màu thì bàn tối đi khi người chơi đang thao tác.
- [ ] Play mode, kiểm tay:
  - Trong lúc một hộp đang nổ, kéo thẻ giữa hai hộp khác → được.
  - Thả thẻ vào hộp đang nổ → thẻ bay về chỗ cũ, hộp không rung, số nước không đổi.
  - Không nhấc được thẻ trong hộp đang nổ, cũng như hộp đã đủ 4 đang chờ lượt.
  - Tạo nhóm thứ hai giữa chuỗi → nổ nối luôn; số đếm hộp khoá giảm theo từng bước.
  - Dùng nước cuối giữa chuỗi → không đi thêm được, hết chuỗi thì thua vì hết nước.
  - Nút booster tắt suốt chuỗi; Magnet / Shuffle / Undo vẫn khoá cả bàn khi đang diễn.

## Feedback #7 — Cheat bật/tắt tô màu thẻ cùng nhóm

- [ ] `Assets/_Game/Art/Prefabs/UI/Cheat/CheatRoot.prefab`: thêm một hàng có `Toggle` (nhãn ví dụ "Màu thẻ cùng nhóm") + component `CheatBoardSectionView`, kéo Toggle vào `_matchColorsToggle`.

## Feedback #8 — "+" coin ở Home mở thẳng Coin Packs

- [ ] `MainMenuScreen.prefab`: kéo Button `SafeArea/HomePanel/CoinArea/Box/More coins` vào `MainMenuScreen._moreCoinsButton`.
- [ ] (Tuỳ chọn) GameObject `Shop Panel/Scroll` ▸ `ShopPopup._coinPacksTitle` = `Viewport/Content/Coin Packs`. Bỏ trống thì cuộn tới lưới coin (tiêu đề nằm ngay trên, bị che).

## Feedback khác cần làm tay

- [ ] #2 `CompletedPopup.prefab`: chữ nút `Quit Button` "Resume" → "Claim".
- [ ] #9 `MainMenuScreen.prefab` ▸ `Low Banner`: căn width theo safe area (đặt trong node có `SafeAreaFitter`, anchor ngang 0..1).
