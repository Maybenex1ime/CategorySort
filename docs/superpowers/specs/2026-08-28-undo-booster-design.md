# Booster Undo — thiết kế logic

Ngày chốt: 2026-08-28 · Trạng thái: đã duyệt thiết kế

## 1. Mục tiêu

Undo trả bàn về đúng trạng thái **trước nước kéo thẻ gần nhất**. Không thông minh, không tối ưu, không xếp hộ — nó chỉ là nút "quay lại" cho một sai lầm vừa xảy ra.

Nguyên tắc chi phối: **không bao giờ hoàn tác thứ người chơi đã trả tiền**. Undo chỉ đụng vào nước đi, không đụng vào hiệu ứng của booster khác.

## 2. Bốn quyết định hành vi

| # | Câu hỏi | Chốt |
|---|---|---|
| 1 | Nước đi vừa rồi làm nổ nhóm thì sao? | Hoàn tác **tất cả**, kể cả Clear/Collapse. Progress bar tụt lại, thẻ đã nổ hiện về |
| 2 | Lùi được mấy bước? | **Đúng 1**. Undo xong nút xám tới nước đi kế |
| 3 | Undo được nước Magnet/Shuffle không? | **Không**. Dùng hai booster đó là mất quyền undo |
| 4 | Undo cứu được thế thua không? | **Không**. Chỉ dùng khi `Status == Playing` |

Quyết định 1 gần như miễn phí: ảnh chụp lấy trước nước đi thì cascade nằm trọn phía sau nó, không cần luật riêng nào để "gỡ" Clear.

Quyết định 3 là thứ giữ cho thiết kế sạch. Ảnh chụp là **toàn bàn**, nên nếu người chơi đi một nước rồi bấm Shuffle rồi bấm Undo, khôi phục sẽ nuốt luôn kết quả Shuffle vừa mua. Cách duy nhất không sinh bài toán hoàn tiền là để booster khác **xoá ảnh chụp**.

## 3. Cơ chế

Không có thuật toán. `Game.Clone()` đã sao sâu toàn bộ bàn kèm `Moves`, `Cleared`, `uidSeq` và giữ nguyên `Uid` từng thẻ — undo chỉ là giữ lại một bản sao rồi lắp lại.

```
Người chơi kéo thẻ
  │
  ├─ MoveTile: qua hết chốt hợp lệ, NGAY TRƯỚC khi mutate → undoSnapshot = Clone()
  ├─ AfterMove → Settle cascade (Clear / Collapse / RemoveBox)
  └─ cuối Settle → SetUndoAvailable(Playing && CanUndo)

Bấm Undo
  │
  ├─ ViewModel: còn hàng + IsUsable → RequestUse() → trừ 1 lượt
  ├─ MetaSession bắc cầu → LevelCommands.RequestUndo()
  ├─ BoardController: BoosterGateOpen → g = g.ApplyUndo()
  └─ RebuildBoardViews() + Settle() → nút tự xám (hết ảnh chụp)
```

### Vì sao chụp bên trong `MoveTile`

Có hai chỗ gọi `MoveTile` ở view (`Drop` và `DebugMove`) và chụp ở call site thì cả hai đều phải nhớ chụp **trước** khi gọi — quên một chỗ là undo lặng lẽ sai. Đặt trong `MoveTile`, sau toàn bộ chốt từ chối và ngay trước dòng mutate đầu tiên, thì đúng theo cấu tạo: nước đi bị từ chối không chụp phí, nước đi được nhận luôn có ảnh.

Cái giá là `Solver` cũng gọi `MoveTile` — hàng vạn lần mỗi lần giải. Nên có cờ `UndoEnabled`, mặc định **tắt**, `BoardController` bật cho đúng ván đang chơi. `Clone()` cố ý **không** sao cờ này: solver nhân bản từ bàn thật, sao cờ sang là mọi nút của cây tìm kiếm đều clone thêm một lần.

## 4. Vòng đời ảnh chụp

| Sự kiện | Ảnh chụp |
|---|---|
| `MoveTile` thành công (cờ bật) | **ghi đè** — chỉ giữ nước gần nhất |
| Bấm Undo | dùng rồi **xoá** — không lùi tiếp |
| Bấm Magnet / Shuffle | **xoá** (Mục 2, quyết định 3) |
| Nạp màn / restart | **xoá** cùng cả `Game` cũ |

`CanUndo` = có ảnh chụp. Nút sáng khi `Playing && CanUndo` và còn lượt trong túi.

## 5. Nối vào game

Dùng nguyên hạ tầng của Magnet/Shuffle, không phát minh gì mới:

| Mảnh | Cách làm |
|---|---|
| Lệnh meta → bàn | `LevelCommands.RequestUndo()` — bàn không nghe `Bus.Global` được, `WordStack.Board` chỉ tham chiếu `WordStack.Contracts` |
| Cầu nối | `MetaSession` thêm nhánh `BoosterId.Undo`, thay chỗ log cảnh báo "chưa có luật" |
| Xám nút | `LevelSignals.UndoAvailable`, board đẩy sau mỗi settle |
| Chặn input khi diễn | Hai vế như Magnet: `locked` chặn kéo thẻ, `RaiseMoveCommitted(g.Moves)` đẩy phase khỏi `Playing` để overlay phủ nút HUD |
| Chốt từ chối | `BoosterGateOpen("Undo")` — dùng lại nguyên hàm chung |
| ViewModel | Theo `ShuffleBoosterViewModel` |
| Enum | `BoosterId.Undo = 3` — số đã đặt chỗ sẵn từ `f8c45aa` |

## 6. Quyết định đã chốt

- **Undo không tính là một nước đi.** `Moves` không tăng vì nó *giảm* — về đúng số trước nước bị gỡ. Giống Magnet/Shuffle ở chỗ không tự cộng thêm.
- **Trừ lượt ngay khi bấm.** Nút chỉ sáng khi undo chắc chắn chạy được nên không có cửa mất lượt oan; cùng lập luận với hai booster kia.
- **Vẫn gọi `Settle()` sau khi khôi phục.** Trạng thái được khôi phục là trạng thái đã đứng yên nên `SettleStep` trả `None` ngay — nhưng đi qua `Settle()` là cách duy nhất bàn đẩy lại cờ booster, hạ overlay và bắn `EvaluationCompleted`. Bỏ nó thì phase kẹt ngoài `Playing`.
- **Logic sống ở `Domain/`**, không import `UnityEngine` — `selfcheck.sh` kiểm được ngoài Unity.
- **Animation để placeholder** ở nhịp đầu, giống `MagnetAnimation`. Chặn input phải chạy thật ngay từ nhịp đó.

## 7. Ngoài phạm vi

- Animation thật (thẻ bay ngược về ô cũ, thẻ đã nổ hiện lại) — nhịp riêng.
- Nút trong prefab HUD, entry cheat, entry `t_booster_undo` trong `SO_TransactionCatalog` — việc trong Editor.
- **Second-chance / revive**: hoãn bắn thua khi bàn `Stuck` để mời người chơi dùng Undo cứu. Là điểm monetization mạnh nhất của booster này nhưng đụng vào flow thua (`ReportResultIfFinished`, phase meta, popup, luồng mua trong lúc treo án) — nhịp riêng, không gộp vào đây.
- Undo nhiều bước. Hạ tầng chịu được (đổi một biến thành `Stack<Game>`), chỉ là chưa cần.
