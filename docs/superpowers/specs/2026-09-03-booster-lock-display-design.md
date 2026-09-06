# Booster HUD — hiển thị locked / "+" / số lượng

Ngày chốt: 2026-09-03 · Trạng thái: đã duyệt thiết kế

## 1. Mục tiêu

Mỗi nút booster trên HUD phải cho biết đúng một trong ba trạng thái:

| Trạng thái | Nền nút | Icon | Badge | Chữ Lv | Bấm được |
|---|---|---|---|---|---|
| Chưa mở khoá | `Booster Lock.png` (xám, ổ khoá in sẵn) | ẩn | ẩn | `Lv.N` | không |
| Mở khoá, hết lượt | `Button Booster.png` (xanh) | icon booster | `+` trên SPR_AddBoosterCircle | ẩn | có, mở popup mua |
| Mở khoá, còn lượt | `Button Booster.png` | icon booster | số lượt trên SPR_QuantityBooster | ẩn | có nếu bàn dùng được |

Hai dòng cuối đã chạy trong `Magnet/Shuffle/UndoBoosterButtonView`. Thiết kế này thêm dòng đầu và
bật/tắt badge + chữ Lv, hai thứ đang inactive sẵn trong prefab.

## 2. Quyết định

- **Dùng lại `GameplayUiRoot`** (port aquapark) làm chủ trạng thái khoá. Không thêm logic vào ba view.
- **Level mở khoá đọc từ `SO_UnlockSchedule`**, không hardcode. Asset có 3 entry Shuffle/Magnet/Undo,
  level tạm = 1 (chưa khoá gì) cho tới khi GD điền số thật trong Inspector.
- **Khoá qua `CanvasGroup`** trên GameObject nút, không ghi `Button.interactable`. Lý do: ba view cũng
  ghi field đó (xám khi bàn không dùng được) và sẽ bật lại nút đang khoá mỗi lần count đổi.
  `CanvasGroup.interactable = false` thắng mọi `Button.interactable` bên dưới.
- **Icon**: lịch có icon thì dùng, không thì giữ icon sẵn trong prefab (gán null là ra ô trắng).
  Khoá mà không có sprite khoá riêng thì ẩn hẳn icon vì nền đã in ổ khoá.
- **Nền mặc định của nút trong prefab đổi sang `Button Booster.png`**: `GameplayUiRoot` cache nền lúc
  Start làm hình "đã mở", nên nền trong prefab phải là hình mở khoá.
- Nguồn level hiện tại: `IGameplayFlowController.LevelTitle` ("Level N"), parse như aquapark.

## 3. Việc phải làm

| Mảnh | Sửa gì |
|---|---|
| `GameplayUiRoot.cs` | cache icon gốc + CanvasGroup mỗi slot; locked → CanvasGroup off, icon ẩn; unlocked → icon lịch hoặc gốc |
| `GamePlayUIRoot .prefab` | thêm component GameplayUiRoot ở root, 3 slot, `_unlockSchedule`, `_lockedBgSprite`, `_hudView` (tiêu đề level chưa ai đẩy vào HUD); nền 3 nút → Button Booster |
| `SO_UnlockSchedule.asset` | 3 entry, LevelIndex 1 |
| `ProjectScope.prefab` | `_unlockSchedule` trên ProgressionInstaller (popup mua lấy tên/icon cùng nguồn) |

## 4. Ngoài phạm vi

- Cấp lượt ban đầu khi mở khoá (`InitialCount`), popup "mở khoá booster mới".
- Icon riêng cho Magnet/Shuffle/Undo (chưa có art).
- Second-chance, animation.
