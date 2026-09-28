# Figma Motion Importer

Chuyển timeline animation trong Figma thành `LitMotionAnimation` trên prefab, không viết code. Số liệu nằm trong một file JSON; sửa JSON rồi import lại là thấy ngay, xem trước bằng nút Play trong Inspector của `LitMotionAnimation`.

Code: `Assets/_Game/Board/Editor/FigmaMotion/`. Mẫu thật: `Assets/_Game/Art/Blocker/Blocker_Box/LockOpen.figma-motion.json` (mở hộp khoá nhóm, Root = `Lock Root` trong `Box.prefab`).

## Dùng

| Chỗ | Làm gì |
|---|---|
| **Tools ▸ Figma Motion ▸ Importer** | Cửa sổ đầy đủ: chọn **Root** + **File JSON** (hoặc bật *Dán JSON*), bấm **Kiểm tra** (chỉ đọc) rồi **Import** |
| Thanh **Figma Motion** trong Scene view | *Figma Motion* mở cửa sổ; **Import lại** áp file JSON lần trước lên GameObject đang chọn. Ẩn/hiện ở menu ⋮ ▸ Overlays |
| **Tools ▸ Figma Motion ▸ Import lại lên selection** | Như nút Import lại |

Import **ghi đè toàn bộ** component của `LitMotionAnimation` trên Root (Undo được), đặt Auto Play = None — code tự gọi `Play()`. Nhớ Save prefab.

## Định dạng JSON (`figma-motion/v1`)

```json
{
  "schema": "figma-motion/v1",
  "name": "Mở hộp khoá nhóm",
  "pxToUnit": 0.005,
  "flipY": true,
  "timeUnit": "s",
  "mode": "parallel",
  "ease": "ease-out",
  "tracks": [
    { "target": "Lit (1)", "property": "x", "mirrorX": true,
      "keys": [ { "time": 0, "value": 0 }, { "time": 0.229, "value": -30 }, { "time": 0.458, "value": -37 } ] },
    { "target": "", "property": "opacity", "ease": "cubic-bezier(0.4, 0, 0.2, 1)",
      "keys": [ { "time": 0.57, "value": 1 }, { "time": 0.67, "value": 0 } ] }
  ]
}
```

**Cả file**

| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `pxToUnit` | 0.01 | 1 px Figma bằng bao nhiêu unit trong không gian local của node. Sprite: `1 / PPU × hệ số xuất art` (art 2x, PPU 400 → 0.005). UI canvas: thường 1 |
| `flipY` | true | Figma trục y hướng xuống, Unity hướng lên |
| `timeUnit` | `s` | `s`, `ms`, hoặc `%` (phần trăm một vòng timeline — cần thêm `cycle` = số giây của vòng) |
| `mode` | `parallel` | `parallel`: mọi track chạy song song theo `time` của nó (khớp timeline Figma). `sequential`: track sau chạy khi track trước xong, `time` của key đầu thành khoảng nghỉ sau track trước |
| `ease` | `ease-out` | Easing cho đoạn nào không ghi riêng |

**Một track** = một thuộc tính của một node.

| Field | Ý nghĩa |
|---|---|
| `target` | Đường dẫn từ Root: `""` = chính Root, `"Middle/Top"`. Nhiều con **trùng tên** thì bắt buộc ghi chỉ số theo thứ tự anh em: `"UpperLit[0]"`, `"UpperLit[1]"` |
| `property` | `x`, `y` (px) · `rotation` (độ, dương = ngược chiều kim đồng hồ) · `scale`, `scaleX`, `scaleY` (hệ số, như Figma) · `opacity` (0..1) |
| `mirrorX` | Lật dấu `x` và `rotation` — node đối xứng qua trục dọc dùng chung số với node bên kia |
| `ease` | Easing mặc định của track |
| `keys` | Ít nhất 2 key, `time` tăng dần. Key có thể tự ghi `ease` cho **đoạn từ nó tới key sau** |

Easing: `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out`, hoặc `cubic-bezier(x1, y1, x2, y2)` chép từ ô *Custom* của Figma (y được vượt 0..1 để có overshoot). Curve dựng ra trùng cubic-bezier đó, không xấp xỉ.

## Importer quy đổi thế nào

- **delay / duration** = `time` key đầu / key cuối trừ key đầu. Không cần tự tính.
- **x, y, rotation, scale** là tương đối: lấy chênh lệch so với key đầu rồi **cộng lên giá trị đang author trong prefab** lúc Play. Figma ghi toạ độ tuyệt đối hay độ dời đều được.
- **scale** = tỉ lệ so với key đầu, nhân với scale author của node. `1 → 0.9` trên node scale 1.75 thành −0.175.
- **opacity** là tuyệt đối. Sprite: nhân lên alpha author của mọi `SpriteRenderer` bên dưới (`SpriteGroupAlphaAnimation`). UI (RectTransform): qua `CanvasGroup`, tự thêm nếu thiếu.
- Track nào giá trị không đổi thì bỏ qua và cảnh báo.

## Lấy số từ Figma

1. Mở frame, bảng Motion/Prototype của node, đọc từng thuộc tính có keyframe: thời điểm, giá trị, easing giữa hai keyframe.
2. Mỗi thuộc tính có keyframe là một track; `target` là tên layer tương ứng trong prefab.
3. Timeline Figma chạy lặp (cohort loop) nhưng trong game thường chạy một lần — importer không tự lặp.

## Bẫy thường gặp

- **Pivot**: Figma scale và xoay quanh tâm node. Sprite / RectTransform có scale hoặc rotation phải để pivot Center — nút *Kiểm tra* cảnh báo khi lệch.
- **Tên trùng**: importer không tự đoán; báo lỗi kèm gợi ý `Tên[0]…`.
- **Chỉnh tay trong Inspector** sẽ mất khi import lại — chép số đã chỉnh ngược vào JSON.
