# Stack: một hộp + chồng Tile Holder — Thiết kế

> Trạng thái 2026-09-30: **đã huỷ** — visual hộp + khay chồng lên nhau không đẹp, quay về hộp chồng cũ.
> Code đã làm nằm trên nhánh `origin/feat/stack-tile-holder`. Giữ lại từ đợt này: luật Magnet xoá hộp chôn
> bị hút rỗng (Mục 7.1) và `AnimationBuildKit`.
>
> (2026-09-29: đã duyệt thiết kế, thay bản nháp cùng tên.)

## 1. Mục tiêu

Stack không còn vẽ các hộp chồng nhau: chỉ **một hộp** + tối đa **5 Tile Holder** (Peek1…Peek5) bên dưới,
mỗi holder đại diện một hộp chôn và mang **bộ thẻ mini riêng** của hộp đó. Khi hộp trên cùng được xoá, thẻ mini
của holder trên cùng bay vào hộp và thành thẻ thật; holder đó nhấc lên rồi biến mất — hộp **không** nhấc đi
nữa. Mọi chuyển động là `LitMotionAnimation` (không tween bằng script).

## 2. Quyết định đã chốt

| # | Chủ đề | Chốt |
|---|---|---|
| 1 | Holder bị tiêu thụ | **Từ trên xuống.** Clear: holder trên cùng đang hiện bay đi rồi tắt hẳn; holder kế trở thành lớp trên cùng, thẻ mini của nó đã hiện sẵn. Không holder nào hiện lại, không lớp nào dịch chuyển |
| 2 | Thẻ mini | Thẻ mini *i* hiện ⇔ ô *i* của hộp nó đại diện có thẻ. Mọi holder hiện thẻ mini của hộp mình; lớp dưới bị nền lớp trên che, lộ ra khi lớp trên đi |
| 3 | Lộ mặt thẻ | Thẻ mini bay + to dần lên cỡ thẻ thật (lệch nhịp); hạ cánh thành thẻ thật, **art** mờ dần hiện lên kèm nảy nhẹ (thẻ không có chữ) |
| 4 | Blocker | Chỉ hộp trên cùng lúc đầu màn (`boxes[0]`) có blocker; hộp chôn không bao giờ có |
| 5 | Dựng animation | Tool Editor dựng sẵn, chỉnh tay trong Inspector |
| 6 | Độ sâu | Tối đa **5 hộp chôn** (stack ≤ 6 lớp) — level design đảm bảo; vượt thì cảnh báo |
| 7 | `HorizontalSpriteLayout` | **Giữ**, vẫn dồn thẻ mini đang bật vào giữa lúc chơi; tắt tạm trong lúc Fill |
| 8 | Thứ tự vẽ | Hạ `Background_0` xuống −100, holder dùng dải −7…8 (Mục 3.4) |
| 9 | Magnet hút rỗng hộp chôn | **Xoá hộp đó ngay** (luật); thẻ mini dồn lên trong dữ liệu, trên màn chỉ **holder cuối tắt** (không animation) |
| 10 | Magnet — điểm xuất phát | Thẻ hút từ hộp chôn bay ra **từ thẻ mini** trên holder của hộp đó |
| 11 | Shuffle | Luật + animation giữ nguyên (xuất phát từ hộp); thẻ mini cập nhật tức thì, không chuyển động |

## 3. Cấu trúc

### 3.1 `StackView`

- `peekLayers` / `nextTileMarkers` cũ thay bằng mảng **5 holder**, mỗi holder:
  - `root` — GameObject Peek*k*;
  - `layout` — `HorizontalSpriteLayout` của `TileMarkerHolder`;
  - `minis[4]` — thẻ mini ứng với **ô 0..3** của hộp (con `Mini 0..3`, thứ tự con = thứ tự ô, trái → phải);
  - `fill`, `lift` — hai `LitMotionAnimation` trên Peek*k*.
- `consumed` (int) — số holder đã tiêu thụ từ trên xuống. Holder trên cùng = **Peek(consumed + 1)**.

### 3.2 Holder nào mang hộp nào

Hộp chôn thứ *d* (1 = ngay dưới hộp trên cùng) nằm trên **Peek(consumed + d)**; holder không mang hộp nào thì tắt.

| Sự kiện | `consumed` |
|---|---|
| Dựng bàn | = 0 |
| Hộp trên cùng bị xoá, lộ hộp kế (clear, hoặc nước đi kéo rỗng hộp) | +1 |
| Magnet xoá hộp chôn | giữ nguyên (các hộp sau dồn lên trong mapping, holder cuối tắt) |
| Undo đưa hộp cũ về | −1 |

Ví dụ 3 hộp chôn: dựng bàn → Peek1-2-3; clear → Peek2-3 (`consumed` 1); Magnet hút rỗng hộp chôn cuối → Peek2.
`consumed` là trạng thái riêng của view (lịch sử), không suy ra được từ bàn chơi.

### 3.3 Thẻ mini

- `minis[i]` bật ⇔ ô *i* của hộp mà holder đại diện có thẻ. Cập nhật tức thì lúc dựng bàn, sau clear, sau
  Magnet, sau Shuffle, sau Undo.
- `HorizontalSpriteLayout` giữ nguyên: thẻ đang bật dồn giữa hàng mỗi frame. Nó **tắt** trong lúc `fill` của
  holder đó chạy (nếu không nó ghi đè vị trí mỗi `LateUpdate`) và bật lại khi dọn.

### 3.4 Thứ tự vẽ

Project chỉ có sorting layer `Default`. `Background_0` (`Main.unity`) hạ **0 → −100**; nếu `Main 1.unity` hoặc
scene trong `_Recovery/` còn được dùng thì hạ theo.

| Lớp | Nền holder | Thẻ mini |
|---|---|---|
| Hộp (không đổi) | Tray Bottom 5, Bg 6, Shadow 7 | thẻ thật Bg 10, Art 11 |
| Peek1 | 4 | **8** — trên thân hộp khi bay vào, dưới thẻ thật |
| Peek2 | −1 | 0 |
| Peek3 | −3 | −2 |
| Peek4 | −5 | −4 |
| Peek5 | −7 | −6 |

Chuỗi `nền Peek5 < mini Peek5 < … < nền Peek1 < hộp` làm thẻ mini lớp dưới bị nền lớp trên che, và lộ ra khi
lớp trên nhấc đi.

## 4. Luồng khi hộp trên cùng bị xoá — `StackView.RevealFromHolder`

Thay `LiftAwayBox` + `RevealBox` ở nhánh `ev.BoxRemoved` của `BoardController.Settle`. Hộp đứng yên, rỗng.

1. **Fill** — holder Peek(consumed + 1): tắt `layout`; gán `Target` = ô *i* của hộp cho `FlyToTargetAnimation`
   của thẻ mini *i*; `fill.Stop()` rồi `Play()`; chờ `IsPlaying` về false.
2. **Thẻ thật** — tắt 4 thẻ mini; `SpawnTiles(s)` dựng thẻ thật: `bg` hiện ngay, `art` alpha 0; mỗi thẻ chạy
   `reveal` (art 0 → 1 + nảy).
3. **Lift** — song song với bước 2: `lift.Play()` (holder nhấc lên + mờ). Chờ cả `reveal` lẫn `lift` xong.
4. **Dọn** — `lift.Stop()` (trả vị trí/alpha gốc), tắt holder; `fill.Stop()` (thẻ mini về chỗ); bật lại
   `layout`; `consumed + 1`; cập nhật thẻ mini các holder còn lại; `RefreshBlockerVisuals`.

Trường hợp riêng:
- **Hộp lộ ra rỗng**: không thẻ mini nào bay; holder vẫn nhấc đi; `Settle` xoá hộp rỗng ở bước kế và lặp lại với
  holder kế. Magnet đã xoá hộp chôn bị hút rỗng (Mục 7), nên trường hợp này chỉ còn khi level đặt sẵn một hộp
  chôn rỗng.
- **Hộp không có holder** (sâu hơn 6 lớp): thẻ hiện ngay tại chỗ, không animation (như `RevealBox` hiện nay).
- **Hộp đáy được clear**: không có `BoxRemoved`, không đổi.

Dọn code cũ: bỏ `BoxView.LiftAway` và `BoardController.LiftAwayBox`. `unlockDur` / `unlockLift` /
`unlockEase` của `BoxView` bỏ nếu không còn nơi dùng.

## 5. Component `FlyToTargetAnimation`

Component LitMotion tự viết (`Assets/_Game/Board/Views/FlyToTargetAnimation.cs`), cùng kiểu
`SpriteGroupAlphaAnimation`: `target` (Transform thẻ mini), `duration` / `delay` / `ease` chỉnh trong Inspector.

- Thuộc tính `Target` (Transform đích) gán lúc chạy, không lưu trong prefab.
- `Play`: chụp `position` hiện tại (sau khi layout đã căn giữa), bay tới `Target.position` (world).
- `Stop`: trả về vị trí đã chụp.
- `Target` null thì không bay.

Phần to dần dùng `TransformScaleAnimation` có sẵn (tỉ lệ cố định).

## 6. Tool `Tools ▸ WordStack ▸ Build Stack Holder Animations`

Chạy trong `Stack.prefab`, chọn object gốc (có `StackView`). Undo được. Chạy lại = dựng lại từ đầu, ghi đè số đã
chỉnh (như `Build Count Lock Animations`).

1. **Chuẩn hoá**: trong mỗi `TileMarkerHolder`, đổi tên + xếp con thành `Mini 0..3` theo x trái → phải; đặt thứ tự
   vẽ theo Mục 3.4; nối 5 holder vào `StackView`.
2. **Đo**: tạm dựng `Box.prefab` + một `Tile.prefab` vào `BoxAnchor`, lấy kích thước world thẻ thật trong ô so với
   thẻ mini → scale đích; xoá bản tạm.
3. **Mỗi Peek*k*** — hai `LitMotionAnimation` (Auto Play = None):
   - `fill`: mỗi thẻ mini *i* một `FlyToTargetAnimation` (0.3 s, delay *i* × 0.05 s, OutQuad) + một
     `TransformScaleAnimation` (scale gốc → cỡ thẻ thật, cùng nhịp);
   - `lift`: `TransformPositionAnimation` relative +0.25 Y + `SpriteGroupAlphaAnimation` 1 → 0 (0.35 s, OutCubic).
4. **`Tile.prefab`** — `reveal`: `SpriteGroupAlphaAnimation` trên `Art` 0 → 1 (0.25 s) +
   `TransformScalePunchAnimation` trên gốc thẻ (0.1, 0.25 s); nối vào `TileView.revealAnim`.

## 7. Magnet

### 7.1 Luật (`GameMagnet.ApplyMagnet`)

Cách chọn nhóm và cách hút giữ nguyên. Thêm: sau khi hút, **xoá mọi hộp chôn** (chỉ số ≥ 1 trong stack) vừa
thành rỗng. Hộp trên cùng rỗng vẫn để `Settle` xử lý như cũ. `MagnetResult` thêm danh sách hộp chôn đã xoá
(stack + chỉ số trước khi xoá) để view biết tắt holder nào. `Picks[].Box` giữ chỉ số **trước** khi xoá.
Magnet vẫn gọi `ClearUndo`, nên Undo không bao giờ phải khôi phục hộp chôn.

### 7.2 View

- Thẻ hút từ hộp chôn chỉ số *b* bay ra **từ thẻ mini** ô tương ứng trên Peek(consumed + *b*); thẻ mini đó tắt
  ngay khi bay. Hộp chôn không có holder → bay từ chỗ như hiện nay. Thẻ ở hộp trên cùng bay từ ô của nó.
- Sau Magnet: cập nhật thẻ mini theo mapping mới (hộp sau dồn lên), tắt holder thừa ở cuối — tức thì,
  `consumed` giữ nguyên.

## 8. Shuffle

Luật và animation giữ nguyên (chỉ đụng hộp trên cùng, thẻ xuất phát từ hộp). Sau khi xáo: cập nhật thẻ mini tức
thì, không chuyển động.

## 9. Undo

Hộp cũ chỉ quay về khi nước đi vừa rồi kéo rỗng hộp trên cùng (xoá không qua clear — clear và Magnet đều mất
quyền Undo). Khi đó: `consumed − 1`, holder đó hiện lại tức thì kèm thẻ mini, hộp cũ trượt về như hiện nay.

## 10. Huỷ bàn / chơi lại giữa chừng

`StackView.Abort()`: dừng `fill` / `lift` mọi holder và `reveal` đang chạy, trả vị trí/alpha gốc, bật lại
`layout`. `RevealFromHolder` dùng token (mẫu `BoxView.OpenGroupLock`) để tự thoát khi bị cắt. `DestroyBoard` gọi
`Abort()`; `BuildBoard` đặt `consumed = 0`.

## 11. Nạp level

`BuildBoard` log cảnh báo (không chặn) khi: một stack có hơn 6 lớp; có blocker ở `boxes[1..]`.

## 12. Kiểm thử

| Loại | Nội dung |
|---|---|
| Luật — `selfcheck.sh` | Magnet xoá hộp chôn bị hút rỗng, không đụng hộp trên cùng rỗng; `Picks` giữ chỉ số cũ; mọi level vẫn giải được. Bản demo HTML không có Magnet — không phải sửa |
| EditMode | Mapping holder qua chuỗi dựng bàn → clear → Magnet → Undo; thẻ mini bật đúng theo ô; `FlyToTargetAnimation` bay tới đích, `Stop` trả chỗ cũ; tool dựng đủ 5 × (`fill`, `lift`) + `reveal`, đúng thứ tự vẽ |
| Trong Unity (mắt) | `lv-002` / `lv-008` (4 hộp mỗi stack): clear từng lớp; Magnet hút rỗng hộp chôn + thẻ bay từ thẻ mini; Undo nước kéo rỗng hộp; chơi lại giữa lúc animation chạy |
| Biên dịch | `./compilecheck.sh`, `.git/sdd/testcheck.sh` |

## 13. Ngoài phạm vi

- Holder quay vòng khi stack sâu hơn 6 lớp.
- Chuyển động khi dồn holder sau Magnet, chuyển động của thẻ mini khi Shuffle.
- Stack/holder dạng UI (RectTransform).
- Blocker trên hộp chôn.
