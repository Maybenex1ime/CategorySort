# Board auto-fit: bàn chơi vừa khoảng trống giữa HUD

## Vấn đề

`BoardController.FitCamera` chừa lề cố định theo world unit (trên/dưới 1.5, hai bên 0.4, đẩy camera lên 0.5). Canvas UI là Screen Space Overlay 1080×1920, match theo chiều rộng, nên HUD trên và thanh booster dưới chiếm một tỉ lệ chiều cao khác nhau theo tỉ lệ màn hình:

- máy dài (20:9): lề thừa, bàn nhỏ;
- máy bè (4:3, tablet): HUD đè lên bàn.

Camera chỉ fit lúc dựng màn, đổi độ phân giải hay safe area giữa màn không fit lại.

## Thiết kế

### 1. Đánh dấu UI che bàn

`BoardController` thêm `[SerializeField] RectTransform[] hudBlockers`. Người dùng kéo các ô HUD trên (CoinArea, LevelBox, Settings Button, Progress Bar) và dưới (Box BG + các Booster Button) vào.

Mỗi blocker lấy góc bằng `GetWorldCorners` rồi đổi sang pixel màn hình bằng `RectTransformUtility.WorldToScreenPoint` (camera = null với canvas Overlay, `canvas.worldCamera` với canvas Camera).

- Tâm blocker ở nửa trên màn hình → mép trên vùng trống = `min(yMin)` của các blocker trên.
- Nửa dưới → mép dưới vùng trống = `max(yMax)` của các blocker dưới.
- Vùng trống giao với `Screen.safeArea`.
- Blocker inactive vẫn tính (zoom không nhảy khi HUD bật/tắt).

### 2. Khung bàn

Giữ khung lưới 3×3 cố định (mọi màn cùng zoom), union với pos thực tế như hiện tại:

- ngang: `minX·PitchX − BoxSize/2` tới `maxX·PitchX + BoxSize/2`;
- dọc: trên `−minY·PitchY + BoxSize/2`, dưới `−maxY·PitchY − BoxSize/2 − fitExtraBelow`;
- nới thêm `fitPadding` mỗi cạnh.

Inspector: `fitPadding` (0.15), `fitExtraBelow` (1.0 — chỗ cho lớp lấp ló + Tile Holder dưới hộp hàng cuối; núm chỉnh tay).

### 3. Hàm fit thuần

`BoardFit.FitOrtho(Rect board, Rect freePx, Vector2 screenPx, out Vector2 camPos, out float orthoSize)`:

- `orthoSize = max(board.h·H / (2·free.h), board.w·H / (2·free.w))` với H = chiều cao màn hình (pixel);
- world/pixel = `2·orthoSize / H`;
- `camPos = board.center − (free.center − screen/2) · worldPerPx` — tâm bàn rơi đúng tâm vùng trống;
- vùng trống rỗng/âm hoặc màn 0 px → coi vùng trống là cả màn.

Chỉ camera di chuyển; `root` của bàn đứng ở gốc (hit-test so world với local).

### 4. Fit lại khi đổi

`LateUpdate`: có bàn và (cờ `fitDirty` hoặc kích thước màn / `Screen.safeArea` khác lần fit trước) → `FitCamera()`. Dựng màn đặt `fitDirty = true` (HUD layout có thể xong muộn một frame). `FitCamera` gọi `Canvas.ForceUpdateCanvases()` trước khi đọc góc blocker. `OnValidate` đặt `fitDirty` để chỉnh padding thấy ngay.

### 5. Fallback

`hudBlockers` rỗng → công thức cũ, không đổi hành vi cho tới khi gán.

## Việc làm trong Unity (người dùng)

- `Main.unity`: kéo các ô HUD vào `BoardController.hudBlockers`.
- Khuyến nghị: Progress Bar trong `GamePlayUIRoot .prefab` đang neo giữa màn + đẩy lên ~786; màn thấp (4:3) nó ra ngoài mép trên. Đổi neo về `(0.5, 1)`.
- Chỉnh `fitExtraBelow` nếu Tile Holder hàng dưới sát thanh booster quá.

## Kiểm tra

- EditMode `BoardFitTests` (`WordStack.Board.Tests`): bàn nằm trọn trong vùng trống ở 9:16, 20:9, 4:3; tâm bàn trùng tâm vùng trống; bị giới hạn bởi chiều rộng hoặc chiều cao đúng màn; vùng trống rỗng → dùng cả màn.
- `./compilecheck.sh`, `bash .git/sdd/testcheck.sh`.
- Người dùng xem bằng dropdown screen size của Cheat.

Bỏ qua: trần zoom trên tablet (`maxOrthoSize`) — thêm khi thẻ trông quá to.
