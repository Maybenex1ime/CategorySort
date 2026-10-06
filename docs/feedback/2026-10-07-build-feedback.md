# Feedback bản build — 2026-10-07

Ghi lại để tìm phương án xử lý từng mục. Chưa có quyết định nào ở đây.
Cột "Điểm bắt đầu" chỉ là chỗ code liên quan để đọc trước, chưa phải hướng sửa.

Trạng thái: `[ ]` chưa xử lý · `[~]` đang làm · `[x]` xong.

| # | Khu vực | Feedback | Điểm bắt đầu | Trạng thái |
|---|---|---|---|---|
| 1 | Settings | Nút on/off: tăng hitbox lên toàn bộ button | `UI/Popups/SettingsPopup.cs`, `PausePopup.cs`, `_StudioSDK/UI/Components/UIToggleFeedbackDriver.cs` | [ ] |
| 2 | Popup Win | Đổi text nút thành "Claim" | `CompletedPopup.prefab` (nút `Quit Button`, text đang là "Resume") | [ ] |
| 3 | Popup Win | Hiện số coin phần thưởng, không có dấu "+" | `UI/Popups/CompletedPopup.cs` — `Initialize`: `"+" + args.RewardCoinAmount` | [ ] |
| 4 | Board | Khi animation clear hộp đang chạy, vẫn cho thao tác với các tile ngoài hộp đang diễn | `Board/Views/BoardController.cs` — cờ `locked` bật suốt `Settle()` | [ ] |
| 5 | Board | Animation clear nhóm: gộp lại thành một tile to ở giữa hộp, trên tile có tên nhóm, sau đó biến mất hoặc thu lại thành một tile nhóm | `BoardController.GatherTiles` / `ClearIntoLock` | [ ] |
| 6 | Board | Nếu tạo thành tile nhóm: khi thu về hộp thì pop ra hình của nhóm đó | Đi cùng #5 | [ ] |
| 7 | Cheat | Thêm Settings on/off: xếp hai tile cùng nhóm thì đổi màu (bật/tắt trong Cheat) | `Board/Views/TileView.cs` — `SetMatchState` (`bgPairFirst`/`bgPairSecond`…), Cheat panel | [ ] |
| 8 | Main Menu | Bấm "+" coin ở Main Menu thì mở thẳng tới coin pack trong Shop | `UI/Screens/MainMenuScreen.cs` (nút `More coins`), `UI/Popups/ShopPopup.cs` | [ ] |
| 9 | Main Menu | Lower banner: width căn theo màn hình | `MainMenuScreen.prefab` | [ ] |
| 10 | Ingame | Căn lại layout board cho đầy + UI ingame: phóng to hộp, thu nhỏ button Booster | `BoardController.FitCamera` / `BoxSize`, `Gameplay/Boosters/Views/BoosterButtonView.cs`, `GamePlayUIRoot .prefab` | [ ] |

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
```

## Câu hỏi cần chốt trước khi làm

- **#4:** cho phép kéo tile ở hộp khác trong lúc cascade sẽ đụng luật settle (chuỗi CLEAR/COLLAPSE nối nhau). Chỉ mở input cho hộp không dính cascade, hay xếp hàng nước đi chờ cascade xong?
- **#5/#6:** khi nào "biến mất", khi nào "thu lại thành tile nhóm"? Có phải CLEAR thường thì biến mất, còn nhóm có cha (COLLAPSE) thì thành tile nhóm?
- **#9:** "căn theo" cái gì — full chiều ngang màn hình hay theo safe area?
