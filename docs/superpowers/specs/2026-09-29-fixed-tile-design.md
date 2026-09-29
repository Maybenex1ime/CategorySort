# Blocker thẻ đóng đinh (FixedTile) — Thiết kế

> Trạng thái 2026-09-29: **đã duyệt thiết kế** (brainstorming), bước kế là plan.

## 1. Mục tiêu

Thẻ bị đóng đinh xuống hộp: người chơi **không kéo nó đi được**, nhưng vẫn **ăn được** bằng cách gom 3 thẻ
cùng nhóm còn lại vào chính hộp đó. Lúc bị ăn, 4 cái đinh vặn ra, nhấc lên rồi biến mất cùng tấm back vàng,
sau đó mới tới animation gộp thẻ như thường. Visual đã có sẵn trong `Tile.prefab` (`Fixed Tile`, đang tắt).

## 2. Quyết định đã chốt

| # | Chủ đề | Chốt |
|---|---|---|
| 1 | Vị trí | Chỉ ở **lớp đầu** (`boxes[0]`) của stack lúc đầu màn |
| 2 | Kéo thẻ | Không nhặt được; bấm vào thì thẻ **rung nhẹ**, không có Ghost, không phồng khi hover |
| 3 | Gom nhóm | Thẻ Fixed **tính** vào bộ 4 (khác thẻ băng) |
| 4 | Magnet | Hút được, nhưng nhóm có thẻ Fixed **xếp sau mọi nhóm không có** |
| 5 | Shuffle | Thẻ Fixed **đứng yên** đúng ô; ô đó không nhận thẻ xáo tới |
| 6 | Kết hợp | Không đứng chung với `ice` trên một thẻ; **được** nằm trong hộp `locked` / `grouplock` |
| 7 | Animation phá đinh | `LitMotionAnimation` do tool dựng, chỉnh trong Inspector |

## 3. Dữ liệu level

Khai báo trên card (cùng chỗ với `ice`):

```json
{ "id": "a1", "text": "A1", "blockers": { "fixed": true } }
```

Validator (`LevelData`) báo lỗi khi nạp:
- giá trị khác `true`;
- card mang `fixed` nằm ở hộp chỉ số ≥ 1 của stack;
- card vừa `fixed` vừa `ice` — `Blockers.CardPairs` rỗng nên đã tự chặn, chỉ cần thêm `fixed` vào `Blockers.CardIds`.

## 4. Luật (Domain — không import UnityEngine)

- `LockKind.Fixed`; `Game.Build` gán `Tile.Lock = { Kind = Fixed }` cho thẻ mang `fixed`. Một thẻ chỉ mang một
  `Lock` là đủ vì Fixed không đứng chung Ice. `Game.IsFixed(t)`.
- `MoveTile`: từ chối khi thẻ nguồn là Fixed. Thẻ khác vẫn vào/ra hộp chứa thẻ Fixed bình thường.
- `CompletedGroupIn` / `SettleStep`: thẻ Fixed đếm và bị xoá như thẻ thường (clear lẫn collapse). Thẻ cha sinh
  ra từ collapse là thẻ thường.
- `HasAnyMove`: thẻ Fixed không phải thẻ đi được.
- Magnet: `IsPullable` trả true cho thẻ Fixed. `IsBetterTarget` thêm tiêu chí **đứng đầu**: nhóm không có thẻ
  Fixed thắng nhóm có. Các tiêu chí cũ giữ nguyên thứ tự phía sau.
- Shuffle: thẻ Fixed không vào tập thẻ được xáo, ô của nó không phải ô đích.
- Không đổi: Undo (thẻ Fixed không bao giờ đi), `Solver.Encode` (mã hoá theo `CardId`, card quyết định Fixed),
  `TickIce`.
- Hệ quả thiết kế màn: hộp chứa thẻ Fixed không rỗng cho tới khi nhóm của nó được gom, nên hộp dưới chỉ lộ ra
  sau đó. `check.mjs` bắt màn không giải được.

**Bản JS phải khớp:** engine trong `demo/wordstack.html` (mà `demo/check.mjs` nạp lại), `demo/tool-check.mjs`,
`docs/wordstack-rules.md` §11. `SelfCheck` thêm assert cho từng gạch đầu dòng ở trên.

## 5. View

**TileView**
- `[SerializeField] GameObject fixedRoot` (= `Fixed Tile`), `[SerializeField] LitMotionAnimation fixedBreakAnim`.
- Bind / làm mới blocker: `fixedRoot.SetActive(IsFixed(tile))`.
- `PlayFixedBreak()` (Stop rồi Play), `IsBreakingFixed`, `EndFixedBreak()` (Stop rồi tắt `fixedRoot`).
  **Phải Stop sau khi chờ xong**: `LitMotionAnimation` giữ motion đã xong và vẫn ghi giá trị cuối mỗi frame.

**BoardController**
- Thẻ Fixed vẫn có vùng chạm. `BeginDrag` gặp thẻ Fixed thì không tạo Ghost, gọi rung nhẹ (punch vị trí ngang
  ~0.06 trong ~0.2 s, cùng kiểu `Shake` của hộp). Hover bỏ qua thẻ Fixed.
- Clear / collapse: trong các thẻ bị gom có thẻ Fixed → chạy `PlayFixedBreak` song song cho chúng, chờ xong,
  `EndFixedBreak`, rồi mới chạy animation gộp hiện có.
- Magnet: thẻ Fixed bị hút phá đinh trước (chờ xong, `EndFixedBreak`), rồi mới vào chuỗi phồng → bay → nổ.
- Shuffle: thẻ Fixed không có animation.

## 6. Animation phá đinh

Trên `Tile.prefab`, 4 đinh chạy cùng lúc:

| Nhịp | Làm gì | Mặc định |
|---|---|---|
| 1 + 2 (song song) | Mỗi đinh quay 1 vòng quanh tâm nó (ngược chiều kim đồng hồ) + nhấc +0.3 Y | 0.6 s, InOutSine / OutCubic |
| 3 | 4 đinh + tấm back vàng (`Fixed Tile/Ice Tile`) mờ về 0 | 0.2 s, bắt đầu lúc 0.6 s |

Tổng ~0.8 s. Tool `Tools ▸ WordStack ▸ Build Fixed Tile Break Animation` dựng bằng `AnimationBuildKit`, sửa thẳng
asset `Tile.prefab` (đóng Prefab Mode trước), nối vào `fixedRoot` + `fixedBreakAnim`. Chạy lại = ghi đè số đã
chỉnh trong Inspector. Hoàn tác bằng git.

## 7. Kiểm thử

- `SelfCheck` (luật, chạy ngoài Unity) + `demo/check.mjs` / `demo/tool-check.mjs`.
- EditMode: tool dựng đủ component và nối field (chạy trên bản mở tạm của prefab, không lưu).
- Play mode:
  1. Thẻ Fixed hiện đinh + back; bấm vào thì rung, không kéo được.
  2. Gom 3 thẻ cùng nhóm vào hộp chứa nó → đinh vặn + nhấc → mờ cùng back → gộp như thường.
  3. Nhóm có cha: collapse sau khi phá đinh, thẻ cha là thẻ thường.
  4. Magnet: chỉ chọn nhóm có Fixed khi không còn nhóm nào khác; phá đinh trước khi bay.
  5. Shuffle: thẻ Fixed đứng yên.
  6. Hộp `locked` chứa thẻ Fixed: mở khoá xong thẻ vẫn bị đóng đinh.
  7. Retry giữa lúc phá đinh: bàn dựng lại sạch, Console không lỗi.

## 8. Ngoài phạm vi

- Level test chứa thẻ Fixed (thêm tay, hoặc sửa một màn có sẵn như lv-010).
- VFX lúc phá đinh.
