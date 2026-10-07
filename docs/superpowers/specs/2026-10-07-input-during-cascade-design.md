# Input during cascade: chỉ khoá hộp đang diễn (feedback #4)

Feedback 2026-10-07 #4: khi animation clear hộp đang chạy, vẫn cho người chơi thao tác với các tile
ngoài hộp đang diễn. Phương án đã chốt: **A — khoá theo hộp**, thả vào hộp bận thì **thẻ bay về chỗ
cũ, không rung hộp**.

## Hiện trạng

- `Drop` → `AfterMove` → `StartCoroutine(Settle(flyDur))`. `Settle` bật `locked = true` suốt cả
  chuỗi CLEAR/COLLAPSE, `Update` thấy `locked` là bỏ mọi input.
- Mỗi bước `Settle` gọi `g.SettleStep` (domain đổi NGAY) rồi mới diễn animation → trong lúc diễn,
  domain của hộp đó đi trước view một bước.
- `Game.Status` chỉ được tính khi `SettleStep` trả `None` → thắng/kẹt luôn chốt ở cuối chuỗi.
- Booster (Magnet, Shuffle, Undo) và Revive tự bật `locked = true` rồi `yield return Settle()`;
  `Settle` mở `locked` ở cuối.
- Tầng meta: `RaiseMoveCommitted` → ViewModel sang `Evaluating` (chỉ nhận khi đang `Playing`);
  cuối `Settle` bắn một `EvaluationCompleted` → `Animating` → `AnimationCompleted` → `Playing`.
  Hết nước được `GameplayFlowAdapter` xét ở `EvaluationCompleted`.

## Thiết kế

### 1. Hộp bận (busy)

Mọi hit-test của bàn (hover, nhấc thẻ, thả) đi qua một danh sách `zones` dựng bởi `RefreshZones()`.
Khoá hộp = lọc ở đó:

- Hộp bận **không có zone thẻ** → không hover, không nhấc được.
- **Zone hộp vẫn giữ**, gắn cờ `Busy`. `Drop` vào zone bận: **chỉ `SnapBack`** (thẻ bay về chỗ cũ),
  không `Shake`, không gọi `g.MoveTile`, không tính nước.

Một stack là bận khi:

| Nguồn | Thời điểm vào | Thời điểm ra |
|---|---|---|
| **Đang diễn** (`busyStacks`): `ev.Stack` của bước hiện tại + các hộp khoá nhóm sắp mở (`opened`) | Ngay sau `SettleStep`, trước animation | Khi animation của bước đó xong (gồm `LiftAwayBox` + `RevealBox`), trước nhịp `cascadeGap` |
| **Chờ lượt** (`g.SettlePending(s)`): hộp đã đủ nhóm hoặc hộp rỗng sắp bị lấy đi nhưng chưa tới lượt | Tự động — tính lại mỗi lần `RefreshZones` | Khi `SettleStep` xử lý nó (chuyển sang "đang diễn") |

"Chờ lượt" chặn hai chuyện lạ: người chơi rút một thẻ khỏi bộ 4 đang chờ nổ, hoặc thả thẻ vào hộp
rỗng sắp bị lấy đi. Hộp chỉ đang **nhận thẻ còn bay** (nước trước của người chơi) **không** bận: domain
của nó đã đúng.

`Game.SettlePending(int s, bool drain)` (domain, mới) dùng đúng điều kiện của `SettleStep`: top box mở
và (có nhóm đủ, hoặc rỗng + không phải đáy + `drain || HadCollapse`). Đặt cạnh `SettleStep` để hai chỗ
không lệch nhau.

### 2. Một vòng cascade duy nhất

- `Settle(float delay = 0f, bool allowMoves = false)`: bật `settling = true`; chỉ bật `locked` khi
  `!allowMoves`. Cuối hàm `settling = false; locked = false` như cũ. Caller cũ (booster, Undo, Revive)
  không đổi → vẫn khoá toàn bàn.
- `AfterMove` và lần `Settle` lúc `Load` truyền `allowMoves: true`.
- `AfterMove` khi `settling` đang chạy: **không** mở `Settle` thứ hai — vòng đang chạy sẽ gặp trạng
  thái mới ở `SettleStep` kế tiếp, nhóm mới đủ nổ nối luôn trong cùng chuỗi.
- `landing`: số thẻ đang bay vào slot (`FlyTo` tăng, `OnComplete` giảm). Đầu mỗi vòng lặp, trước
  `SettleStep`: `while (landing > 0) yield return null;` — không hộp nào nổ khi thẻ còn giữa đường.
  Thay cho `Settle(flyDur)` hiện tại.

### 3. Chặn ở các chỗ còn lại

- Bước cascade đụng stack người chơi **đang kéo ra** (`dragFrom`): huỷ kéo (`CancelDrag`: xoá ghost,
  trả scale thẻ về 1) trước khi diễn. "Chờ lượt" đã chặn nhấc thẻ khỏi hộp sắp nổ nên đây chỉ là lưới
  an toàn.
- `AfterMove` (lúc đang cascade) gọi `RefreshTileVisuals(from/to)` — hai hộp này không bận. 
  `RefreshBlockerVisuals()` bỏ qua stack bận; stack được vẽ lại khi nhả.
- Nhả một stack xong gọi `RefreshBlockerVisuals()` → số đếm hộp khoá (`LockKind.Clears`) và hộp khoá
  mở **theo từng bước**, không đợi hết chuỗi. Cần vì domain đã mở hộp đó — người chơi nhấc được thẻ
  thì hộp phải trông như đã mở.
- Booster giữ khoá cả chuỗi: `BoosterGateOpen` từ chối khi `settling`. Nút booster vẫn bị tắt ở đầu
  `Settle` như cũ.
- Hết nước giữa chuỗi: `LevelSignals.OutOfMoves` (Contracts, mới). `GameplayFlowAdapter.OnMoveCommitted`
  đặt `= _startingMoves > 0 && Remaining(movesUsed) <= 0`; về `false` khi vào màn và khi
  `GrantExtraMoves`. `Update` của bàn thấy cờ bật thì không nhận nước mới. Thua vì hết nước vẫn chốt ở
  `EvaluationCompleted` cuối chuỗi như cũ.

### 4. Tầng meta

- `WordStackGameplayViewModel.NotifyPlayerActionCommittedAsync`: đang `Evaluating`/`Animating` thì chỉ
  cập nhật `RemainingMoves` (HUD đếm nước tụt ngay), không đổi phase. `Playing` → như cũ.
- Vẫn một `EvaluationCompleted` mỗi lần `Settle` chạy → không đổi máy phase.
- `GameplayBlockInputOverlayView` vẫn bật trong `Evaluating`/`Animating` (chặn nút HUD/booster). Nó chỉ
  chặn uGUI; bàn đọc raw Pointer nên không bị chặn. **Kiểm trong Unity**: overlay phải trong suốt, nếu
  không bàn sẽ tối đi trong lúc người chơi đang thao tác.

### 5. Undo

Không đổi. `MoveTile` chụp ảnh trước nước đi; `SettleStep` gọi `ClearUndo()` mỗi lần nổ nhóm. Nước đi
giữa chuỗi lùi được nếu sau nó không có nhóm nào nổ; lùi xong `UndoSequence` chạy `Settle()` dọn phần
còn lại.

## Rủi ro

- View lệch domain → `CheckInvariant` (Editor) báo ngay tại bước lệch; kiểm thêm sau mỗi lần nhả stack.
- Nhịp chuỗi dài hơn nếu người chơi liên tục tạo nhóm mới — chấp nhận, đúng ý feedback.

## Kế hoạch triển khai

1. **Domain** `Game.SettlePending(int s, bool drain)` + test board-tests: hộp đủ nhóm / hộp rỗng không
   đáy → true; hộp khoá, hộp đáy rỗng, hộp bình thường → false; sau `SettleStep` xử lý thì false.
   Test thứ hai: `MoveTile` giữa hai `SettleStep` rồi `Settle` tiếp → bàn hợp lệ, `Cleared` đúng.
   Chạy `./selfcheck.sh` trên bản copy lv-009/lv-010.
2. **Contracts** `LevelSignals.OutOfMoves` + `SetOutOfMoves` + reset. **Adapter** đặt cờ.
   **ViewModel** cập nhật moves khi `Evaluating`/`Animating` + test trong `WordStackGameplayViewModelTests`.
3. **BoardController**: `busyStacks`, `settling`, `landing`, `Zone.Busy`, `RefreshZones` lọc,
   `Drop` vào hộp bận → `SnapBack`, `Settle(…, allowMoves)`, `AfterMove` không mở `Settle` thứ hai,
   `CancelDrag`, `RefreshBlockerVisuals` bỏ stack bận + gọi khi nhả, `BoosterGateOpen` chặn khi
   `settling`, `Update` chặn khi `LevelSignals.OutOfMoves`.
4. `./compilecheck.sh`, `bash .git/sdd/testcheck.sh`. Người dùng kiểm tay trong Play mode (ghi vào
   `docs/feedback/unity-todo.md`): kéo thẻ ở hộp khác trong lúc nổ; thả vào hộp đang nổ (thẻ về chỗ cũ);
   tạo nhóm thứ hai giữa chuỗi; dùng nước cuối giữa chuỗi; booster bị tắt suốt chuỗi.
