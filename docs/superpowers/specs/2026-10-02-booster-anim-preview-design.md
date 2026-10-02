# Xem trước animation booster trong Editor

Ngày chốt: 2026-10-02 · Trạng thái: đã duyệt thiết kế · Nhánh `main`

## 1. Mục tiêu

Xem trước (preview) các hiệu ứng booster ngay trong Editor, không cần bấm Play. Các booster đã chạy bằng code
LitMotion với thông số trong `SO_BoosterAnim` (`BoosterAnimSettings`) — giữ nguyên cách đó cho phần đường bay và
xoáy, chỉ thêm đường xem trước.

Hướng đã chọn (B — lai):

| Hiệu ứng | Cách làm | Xem trước bằng |
|---|---|---|
| Nền xám mờ vào / ra | Chuyển sang component `LitMotionAnimation` trên Panel nền | Nút Play của component |
| Nút booster nảy khi bấm | Chuyển sang component `LitMotionAnimation` trên `BoosterSlot.prefab` | Nút Play của component |
| Thẻ phồng / nổ / nở, thẻ hiện ra | Giữ trong code (nằm trong chuỗi bay, tách ra là lệch nhịp) | Nút Preview mới trong Inspector của `SO_BoosterAnim` |

Ngoài phạm vi: xem trước cả booster (đường bay Nam châm/Undo, xoáy Xáo) — cần refactor chuỗi trong
`BoardController`, để sau nếu cần.

## 2. Hành vi của `LitMotionAnimation` cần nhớ

- `Play()` khi đang chạy là **tiếp tục**, không chơi lại → luôn `Stop()` rồi mới `Play()`.
- `Stop()` **trả giá trị về lúc trước khi Play** (`PropertyAnimationComponent.OnStop` ghi lại `startValue`).
- Chế độ Parallel giữ handle đã xong và **vẫn ghi giá trị cuối mỗi frame** cho tới khi `Stop()`.

## 3. Nền xám (`BoardController.boosterBackdrop`)

- Trên GameObject nền (Panel có `CanvasGroup`): hai `LitMotionAnimation`, Auto Play = None.
  - **Fade In**: `UI/Canvas Group/Alpha` 0 → 1, 0.15 s, OutQuad.
  - **Fade Out**: 1 → 0, 0.15 s, OutQuad.
  (bằng giá trị đang có ở `SO_BoosterAnim.backdropFadeIn/Out`.)
- `BoardController` thêm `[SerializeField] LitMotionAnimation backdropFadeIn, backdropFadeOut`.
- `Backdrop(true)`: `alpha = 0`, `SetActive(true)`, `backdropFadeIn.Stop(); backdropFadeIn.Play()`, chờ tới khi
  `IsPlaying` tắt. **Không** `Stop` khi xong — `Stop` sẽ trả alpha về 0.
- `Backdrop(false)`: `backdropFadeIn.Stop()` rồi đặt ngay `alpha = 1` cùng frame (khỏi nháy), `backdropFadeOut.Stop();
  backdropFadeOut.Play()`, chờ xong, `backdropFadeOut.Stop()`, `SetActive(false)`.
- Thiếu component (field null) → bật/tắt khan bằng `SetActive`, alpha đặt thẳng 1/0. Thiếu `boosterBackdrop` → như cũ,
  không làm gì.
- Xoá `backdropFadeIn`, `backdropFadeOut` khỏi `BoosterAnimSettings` — giá trị chỉ còn một chỗ (component).
- Menu Editor `Tools/WordStack/Build Booster Backdrop Animation`: tìm `BoardController` trong scene đang mở, thêm
  hai component vào `boosterBackdrop` (thêm `CanvasGroup` nếu thiếu), nối hai field, đánh dấu scene bẩn. Người dùng
  lưu scene. Không sửa `Main.unity` trên đĩa.

## 4. Nút booster (`BoosterModule/BoosterSlotView`)

- `BoosterSlot.prefab`: một `LitMotionAnimation` (Auto Play = None) với `Transform/Scale (Punch)` lên chính nút:
  biên độ 0.2 (= `_punchScale − 1`), 0.2 s, Frequency 10, DampingRatio 1.9 (giá trị đang chạy).
- `BoosterSlotView`: bỏ `_animationDuration`, `_punchScale`, `_punch` và đoạn `LMotion.Punch`; thêm
  `[SerializeField] LitMotionAnimation _clickPunch`. Bấm nút: `_clickPunch.Stop(); _clickPunch.Play();` — `Stop` trả
  scale về cỡ thường trước khi nảy lại, đúng như `TryComplete` hiện tại. Field null → không nảy.
- `BoosterModule.asmdef` thêm tham chiếu `LitMotion.Animation`.
- Menu Editor `Tools/WordStack/Build Booster Slot Punch`: dựng component vào `BoosterSlot.prefab` và nối field
  (cùng mẫu với `FixedTileAnimationBuilder`, dùng `AnimationBuildKit`).

## 5. Preview thẻ trong Inspector của `SO_BoosterAnim`

- Custom Editor `BoosterAnimSettingsEditor` (Editor-only, `WordStack.Board.Editor`): vẽ mọi field như cũ, thêm khối
  **Preview** với 4 nút. Chỉ đổi scale (và góc nếu có), không bay:

| Nút | Chuỗi | Thông số |
|---|---|---|
| **Thẻ nam châm** | phồng 1 → `magnetPopScale` (OutQuad) → to dần tới `magnetGatherScale` trong `magnetFlyDur` (OutQuad; xoay tới `magnetSpin` nếu ≠ 0, ease `magnetFlyEase`) → đứng `magnetHold` → nổ về 0 (`magnetBurstEase`, `magnetBurstDur`) | Nam châm |
| **Thẻ cha** | nở 0 → `magnetGatherScale` (OutBack, `magnetParentBloomDur`) → đứng `magnetParentHold` → co về 1 (OutQuad, `magnetParentFlyDur`) | Nam châm (collapse) |
| **Thẻ undo** | phồng 1 → `undoPopScale` (OutQuad, `undoPopDur`) → về 1 (OutQuad, `undoFlyDur`) | Undo |
| **Thẻ hiện ra** | 0 → 1 (OutBack, `magnetRevealDur`) | Nam châm (thẻ chôn nhô lên) |

- Diễn trên **đối tượng đang chọn** (`Selection.activeTransform`): thẻ trong scene hoặc `Tile.prefab` đang mở
  Prefab Mode. Scale 1 ở bảng trên nghĩa là **cỡ hiện tại của đối tượng** (nhân lên, không đặt tuyệt đối).
- Chạy bằng `EditorMotionScheduler.Update`. Xong hoặc bấm **Stop** → trả scale và góc về như trước khi bấm; không
  ghi gì vào prefab/scene (không `SetDirty`, không Undo record).
- Đang Play mode, hoặc chưa chọn đối tượng → thay nút bằng dòng hướng dẫn.
- Các ease cố định trong `BoardController` (OutQuad, OutBack) lặp lại trong preview — chú thích chéo ở cả hai phía
  để sửa một bên thì nhớ sửa bên kia.

## 6. Kiểm thử

- `./compilecheck.sh` (game / editor / meta) và `bash .git/sdd/testcheck.sh`.
- EditMode `WordStack.Board.Tests`: menu dựng nút nảy trên **bản sao tạm** của `BoosterSlot.prefab` → có đúng một
  `LitMotionAnimation` với component `Transform/Scale (Punch)` trỏ vào root, đúng biên độ / thời lượng / frequency /
  damping, field `_clickPunch` đã nối. Mẫu `FixedTileAnimationBuilderTests`.
- Người dùng trong Unity: chạy hai menu, lưu scene; bấm Play của component nền và nút để xem; bấm 4 nút Preview trên
  một thẻ; Play mode — booster vẫn có nền mờ vào/ra, nút vẫn nảy.
