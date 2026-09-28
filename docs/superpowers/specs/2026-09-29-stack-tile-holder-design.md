# Stack: một hộp + chồng Tile Holder — BẢN NHÁP (đang brainstorm)

> Trạng thái 2026-09-29: đang brainstorm (superpowers:brainstorming), **chưa duyệt thiết kế, chưa code**.
> Đã chốt 4 câu hỏi dưới đây; bước kế tiếp là user duyệt hướng dựng animation (mục 4), rồi trình bày thiết kế
> chi tiết từng phần → viết spec chính thức (thay file này) → writing-plans.

## 1. Yêu cầu (lời user)

Stack không còn hiển thị các hộp chồng nhau: chỉ **một hộp** + tối đa **5 tile holder** chồng bên dưới. Logic
"nhấc hộp lên khi clear" chuyển sang tile holder. Khi hộp được clear: các thẻ từ tile holder cao nhất bay vào ô
tương ứng trong hộp rồi hiện sprite từng thẻ; tile holder đó sau đó nhấc lên (như nhấc hộp). Chỉnh lại layer
trong stack cho phù hợp. Animation phải là **LitMotionAnimation** (không tween bằng script).

## 2. Hiện trạng code (đọc lúc 2026-09-29, HEAD 72b016a)

- `StackView` (`Assets/_Game/Board/Views/StackView.cs`): `boxAnchor`, `peekLayers[4]`, `nextTileMarkers[4]`;
  `ShowDepth(hidden, tilesInNext)` bật `peekLayers[i]` khi `i < hidden`, marker báo **số lượng** thẻ hộp kế.
- `Stack.prefab` (bản đã commit): `BoxAnchor` + `TileHolders/Peek1..Peek5` (scale 0.9, y −0.361 → −0.501, Bg
  order 5 → 1; Peek1 gần hộp nhất). Mỗi Peek có `TileMarkerHolder` 4 thẻ mini (Tile_0, scale 0.33, order 10,
  một hàng x = ±0.07/±0.21). **Working tree có thay đổi lớn chưa commit của user trên Stack.prefab** (≈2300 dòng,
  sprite `Tile Holder.png`) — phiên sau phải đọc lại prefab trước khi thiết kế tiếp.
- Clear hộp rỗng: `BoardController.Settle` → `ev.BoxRemoved` → `LiftAwayBox(s)` (`BoxView.LiftAway`: nhấc
  `unlockLift` + mờ, script LMotion) → `RevealBox(s)` (`ResetVisual`, `ShowDepth`, `SpawnTiles` hiện thẻ ngay,
  `RefreshBlockerVisuals`). `ShowDepth` còn được gọi ở `BuildBoard` và `UndoAnimation`.
- Level JSON: `boxes[0]` = hộp trên cùng. Mọi blocker hiện có (lv-009, lv-010) đều ở `boxes[0]`.

## 3. Đã chốt

| # | Câu hỏi | Chốt |
|---|---|---|
| 1 | Holder "cao nhất" + sau khi nhấc | **Peek1 luôn là lớp trên cùng.** Clear: thẻ mini Peek1 bay vào hộp → Peek1 nhấc lên + mờ → Peek1 hiện lại đúng chỗ với thẻ mini của hộp kế; Peek2…5 đứng yên, chỉ lớp sâu nhất tắt (như `ShowDepth` hiện tại). |
| 2 | Thẻ mini | **Ánh xạ 1:1 với ô**: thẻ mini i hiện ⇔ ô i của hộp tương ứng có thẻ; bay thẳng vào ô i. **Mọi lớp Peek** hiện thẻ mini của hộp nó đại diện. |
| 3 | Lộ mặt thẻ | **Mờ dần + nảy nhẹ**: thẻ mini to dần lên cỡ thẻ thật khi bay (lệch nhịp), hạ cánh là thẻ trơn, chữ + art mờ dần hiện lên kèm nảy scale nhỏ. |
| 4 | Blocker của hộp lộ ra | **Blocker chỉ có ở hộp trên cùng (layer 1)**; hộp từ layer 2 trở đi không bao giờ có blocker. Đề xuất: warning khi nạp level nếu có blocker dưới `boxes[0]`. |

## 4. Đang chờ user duyệt: cách dựng LitMotionAnimation

Đề xuất (khuyên dùng) — **tool Editor dựng sẵn rồi chỉnh tay trong Inspector**, giống
`Tools ▸ WordStack ▸ Build Count Lock Animations`:
- `Peek1 · Fill`: 4 thẻ mini bay vào 4 ô + to lên cỡ thẻ thật, lệch nhịp. Toạ độ đích tool đo thật (tạm gắn
  `Box.prefab` vào `BoxAnchor`, `InverseTransformPoint` về không gian cha của marker; scale theo `lossyScale`).
- `Peek1 · Lift`: nhấc lên (relative Y) + `SpriteGroupAlphaAnimation` mờ.
- `Tile · Reveal` (trên `Tile.prefab`): chữ (TMP alpha) + art (sprite alpha) 0→1 + punch scale.
- Code chỉ `Stop/Play` và chờ `IsPlaying` (mẫu `BoxView.CountUnlock`).

Phương án khác: user tự dựng tay theo spec (≈12 component, toạ độ dễ sai); tween bằng code (trái yêu cầu — loại).

## 5. Việc còn phải bàn trong thiết kế chi tiết

- Sorting order: thẻ mini khi bay phải nổi trên thân hộp nhưng lúc nằm trên Peek không đè hộp; Peek nhấc lên
  đi sau hay trước hộp.
- Thời lượng mặc định (bay ~0.3 s, lệch 0.05 s/thẻ, lộ mặt ~0.25 s, nhấc = `unlockDur` 0.35 s / `unlockLift` 0.25).
- `StackView.ShowDepth` đổi chữ ký (cần biết ô nào có thẻ cho từng hộp dưới, tối đa 5 hộp).
- Số phận `BoxView.LiftAway` / `BoardController.LiftAwayBox` (hộp không nhấc nữa) và các field `unlock*`.
- Undo / Magnet / Shuffle / DestroyBoard giữa chừng animation.
