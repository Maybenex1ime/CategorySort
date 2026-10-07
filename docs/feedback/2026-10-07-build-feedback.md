# Feedback bản build — 2026-10-07

Ghi lại để tìm phương án xử lý từng mục. Chưa có quyết định nào ở đây.
Cột "Điểm bắt đầu" chỉ là chỗ code liên quan để đọc trước, chưa phải hướng sửa.

Trạng thái: `[ ]` chưa xử lý · `[~]` code xong, chờ làm tay trong Unity / test tay · `[?]` chờ xác nhận · `[x]` xong.
Việc làm tay chi tiết: [`unity-todo.md`](unity-todo.md).

| # | Khu vực | Feedback | Điểm bắt đầu | Trạng thái |
|---|---|---|---|---|
| 1 | Settings | Nút on/off: tăng hitbox lên toàn bộ button | `UI/Popups/SettingsPopup.cs`, `PausePopup.cs`, `_StudioSDK/UI/Components/UIToggleFeedbackDriver.cs` | [x] |
| 2 | Popup Win | Đổi text nút thành "Claim" | `CompletedPopup.prefab` (nút `Quit Button`, text đang là "Resume") | [x] |
| 3 | Popup Win | Hiện số coin phần thưởng, không có dấu "+" | `UI/Popups/CompletedPopup.cs` — `Initialize`: `"+" + args.RewardCoinAmount` | [x] |
| 4 | Board | Khi animation clear hộp đang chạy, vẫn cho thao tác với các tile ngoài hộp đang diễn | `Board/Views/BoardController.cs` — cờ `locked` bật suốt `Settle()` | [x] |
| 5 | Board | Animation clear nhóm: gộp lại thành một tile to ở giữa hộp, trên tile có tên nhóm, sau đó biến mất hoặc thu lại thành một tile nhóm | `BoardController.GatherTiles` / `ClearIntoLock` | [x] |
| 6 | Board | Nếu tạo thành tile nhóm: khi thu về hộp thì pop ra hình của nhóm đó | Đi cùng #5 | [x] |
| 7 | Cheat | Thêm Settings on/off: xếp hai tile cùng nhóm thì đổi màu (bật/tắt trong Cheat) | `Board/Views/TileView.cs` — `SetMatchState` (`bgPairFirst`/`bgPairSecond`…), Cheat panel | [x] |
| 8 | Main Menu | Bấm "+" coin ở Main Menu thì mở thẳng tới coin pack trong Shop | `UI/Screens/MainMenuScreen.cs` (nút `More coins`), `UI/Popups/ShopPopup.cs` | [x] |
| 9 | Main Menu | Lower banner: width căn theo màn hình | `MainMenuScreen.prefab` | [x] |
| 10 | Ingame | Căn lại layout board cho đầy + UI ingame: phóng to hộp, thu nhỏ button Booster | `BoardController.FitCamera` / `BoxSize`, `Gameplay/Boosters/Views/BoosterButtonView.cs`, `GamePlayUIRoot .prefab` | [~] code `ef1ae35`, `0641934` |
| 11 | Booster | Sửa lại animation Shuffle cho đơn giản hơn | `Board/Views/BoardController.cs` — `ShuffleAnimation` / `Vortex`, `BoosterAnimSettings` (`shuffleInDur`, `shuffleOutDur`) | [x] |
| 12 | Main Menu / Tim | Ô tim ở Home: nút "+" chỉ hiện khi tim chưa đủ 5; bấm "+" mở `NoHeartsPopup`. Trong popup: xem ads chỉ được +1 tim, trả coin thì hồi đầy tim | `UI/Screens/MainMenuScreen.cs` (`BindHeartUI`, `SetHeartLayoutForFullState`), `MainMenuScreen.prefab` ▸ `HeartArea/Box/More coins` (nút "+" đã có, chưa nối), `UI/Popups/NoHeartsPopup.cs` (`OnAdClicked` đang +1, `OnBuyClicked` bắn `t_heart`), `Content/SO_TransactionCatalog.asset` ▸ `t_heart` (900 coin → **1** tim) | [x] |

## Tiến độ — cập nhật 2026-10-07

Code đã vào `main` cho 5 mục, nhưng **chưa prefab / scene nào được nối**, nên trong game hiện chỉ #4 có tác dụng.

| # | Code | Còn thiếu để chạy trong game |
|---|---|---|
| 1 | Xong (`fb292c0`) — `SliderTapToggle` đã gắn đủ 7 công tắc (3 PausePopup, 4 MainMenuScreen), 2026-10-08 | — |
| 2 | Xong — `Quit Button` ghi "Claim", 2026-10-08 | — |
| 3 | Xong — `CompletedPopup.cs:73` hiện `RewardCoinAmount.ToString()` | — |
| 4 | Xong (`efbe6ed`, `5fdc654`, `c690430`) — đã kiểm trong game 2026-10-08 | — |
| 5, 6 | Xong, đã kiểm trong game 2026-10-08: CLEAR — 4 thẻ chụm về tâm hộp thành thẻ to (x1.8) mang tên nhóm, đứng 0.45s rồi co mất; CLEAR mở lồng — thẻ to bay vào icon lồng như cũ; COLLAPSE — thẻ to thu về ô thẻ nhóm rồi thẻ nhóm pop ra với hình nhóm. Chỉnh nhịp ở `BoardController` ▸ "Thẻ nh| — |
| 7 | Xong (`54ae3db`) — `CheatBoardSectionView` gắn trên `Coloring tiles Toggle` (CheatRoot.prefab), 2026-10-08 | — |
| 8 | Xong (`15aeb71`) — `More coins` đã bật và nối vào `_moreCoinsButton`, 2026-10-08 | — |
| 9 | Xong — `Low Banner` vừa khít safe area (`sizeDelta.x = 0`), 2026-10-08 | — |
| 10 | Đang làm. `hudBlockers` đã có 2 ô, `fitPadding` 0.2, `fitExtraBelow` 0.5. Stack scale chuyển vào code: `BoardController.stackScale` = 1.25, khoảng cách hộp tự giãn theo. | 3 nút Booster vẫn 200×200; kiểm lại đủ ô che HUD trong `hudBlockers`. |
| 11 | Xong, chốt 2026-10-08: bỏ xoáy hai pha. Thẻ đổi chỗ bay vòng cung thẳng ô cũ → ô mới, lệch nhau 0.03s; thẻ chui xuống hộp chôn co mất, thẻ từ hộp chôn lên pop ra ở ô mới; thẻ đứng yên không nhúc nhích. Chỉnh ở `SO_BoosterAnim` ▸ "Xáo" (`shuffleFlyDur`, `shuffleFlyEase`, `shuffleStagger`, `shuffleArc`, `shufflePopDur`). | — |
| 12 | Xong, đã kiểm trong game 2026-10-08: nút "+" ở ô tim (`HeartArea/Box/More coins`, tự tìm cạnh Heart Icon) ẩn khi tim đầy, bấm mở `NoHeartsPopup` (`MainMenuScreenArgs.OnOpenHearts` → `AppFlowContext`). Ads +1 tim (giữ nguyên). Mua coin: `t_heart` 900 coin → 5 tim (hồi đầy, `HeartService.Add` tự kẹp ở 5). Popup chặn mua khi tim đã tự hồi đ| — |

Ngoài feedback: coin đếm số + coin bay khi Claim / mua coin (`e40f9bb`, `87e9cb4`, `ecc3fae`, `6cd852b`) cũng đã có code, nhưng `CompletedPopup.prefab` chưa có ô coin, chưa gắn `_coinFly`.

## Phiên kế tiếp

- [ ] **Chỉnh lại UI của Shop cho khít hơn.** Đã làm 2026-10-08: title Coin Packs + Coin Grid rộng 954 = một ô Combo
      (title scale 1.5143, ô coin 304.67 × 380.83). Còn: title `No Ads Offer` và `Special Offer Title` vẫn 630 × 1.8 = 1134,
      thò ra ngoài màn 1080; đồ trong ô coin (icon, số, nút Buy) cỡ cố định nên trông nhỏ trong ô mới.
- [ ] **Content của Scroll trong Shop đang bị lẹm mất một phần.** `Shop Panel/Scroll/Viewport/Content` (VerticalLayoutGroup +
      ContentSizeFitter). Nghi: Viewport co `sizeDelta.y = -330.5` / Scroll `-270` cắt đáy; phần tử dùng scale (Combo 1.5,
      title 1.8) không được layout tính vào chiều cao.
- [ ] **Settings Panel stretch theo màn.** `MainMenuScreen.prefab` ▸ `SafeArea/SetttingsPanel`.
- [ ] #10: thu 3 nút Booster (200 → ~150), soi 9:16 / 20:9 / 4:3.

## Nguyên văn

```
Settings khi chuyển on/off tăng hitbox lên toàn button
Đổi text Popup win thành claim
Hiển thị số lượng coin cua rphanaf thưởng, ko có dấu +
Khi animation clear hộp chạy, vẫn cho người chơi thao tác với các tile khác ngoài hộp đang diễn anim
Animation clear nhóm sẽ gộp lại thành một tile to ở giưuax hộp, trên tile có tên của nhóm sau đó biến mất hoặc sẽ thu lại thành một tile nhóm
Trong trường hợp tạo thành tile nhóm, khi thu về hộp sẽ pop ra hình của nhóm đó
Settings On/Off xếp hai tile cùng nhóm sẽ đổi màu trong Cheat
Click + coin ngoài main thì sẽ bắn thẳng vào coin pack ở trong shop
Lower banner trong Main Menu width căn theo
Căn lại layout board cho đầy + UI Ingame ( Phóng to hộp lên và cho nhỏ button Booster xuống)
Sửa lại animation của Shuffle cho đơn giản hơn
Phần hiện tim ở ngoài màn hình main chỉ bật button + khi mà tym ko đủ 5. Khi bấm vào button sẽ hiện ra NoHeartsPopup. Trong NoHeartsPopup, click xem ads sẽ chỉ lấy dc 1 tym nhưng dùng coin thì sẽ hồi full
```

## Câu hỏi cần chốt trước khi làm

- **#4:** cho phép kéo tile ở hộp khác trong lúc cascade sẽ đụng luật settle (chuỗi CLEAR/COLLAPSE nối nhau). Chỉ mở input cho hộp không dính cascade, hay xếp hàng nước đi chờ cascade xong?
- **#5/#6:** khi nào "biến mất", khi nào "thu lại thành tile nhóm"? Có phải CLEAR thường thì biến mất, còn nhóm có cha (COLLAPSE) thì thành tile nhóm?
- **#9:** "căn theo" cái gì — full chiều ngang màn hình hay theo safe area?
