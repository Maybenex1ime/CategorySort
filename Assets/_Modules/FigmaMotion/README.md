# Figma Motion — timeline Figma sang LitMotion

Module này chuyển animation dựng trên **timeline Motion của Figma** thành component `LitMotionAnimation` trên prefab Unity. Code game không phải viết lại từng tween: số liệu nằm trong một file JSON, importer dựng component, game chỉ gọi `Play()`.

```
Figma (timeline Motion)
   │  Exporter/export-figma-motion.js  — chạy qua Figma MCP (use_figma), chỉ đọc
   ▼
*.figma-motion.json  (figma-motion/v1, lưu trong Assets: cây layer + track)
   │  Tools ▸ Figma Motion ▸ Importer  — hoặc nút "Figma Motion" trên Scene view
   │    Dựng hierarchy  → GameObject + SpriteRenderer trống (anh gắn sprite tay)
   │    Import          → LitMotionAnimation
   ▼
LitMotionAnimation trên GameObject gốc (Auto Play = None)
   │  code game: anim.Play()
   ▼
Chạy trong game
```

## 1. Package cần có

| Package | Bản đang dùng | Ghi chú |
|---|---|---|
| `com.annulusgames.lit-motion` | 2.0.2 | Tự kéo Burst, Collections, Mathematics |
| `com.annulusgames.lit-motion.animation` | 2.0.2 | Component `LitMotionAnimation` + Inspector xem trước |
| `com.unity.ugui` | có sẵn trong template Unity | LitMotion.Animation chỉ bật `CanvasGroupAlphaAnimation` khi có uGUI — importer cần kiểu này |

Thêm vào `Packages/manifest.json` (khoá đúng commit đang chạy ổn):

```json
"com.annulusgames.lit-motion": "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion#ab6e92bfe78ff911def2fd3c4e9c79bb5a186946",
"com.annulusgames.lit-motion.animation": "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion.Animation#ab6e92bfe78ff911def2fd3c4e9c79bb5a186946",
```

Đã chạy thật trên Unity 6000.3.8f1, URP 2D. Bản Unity cũ hơn chưa thử.

Xuất từ Figma cần thêm: **Figma MCP** (tool `use_figma`) nối với Claude, và file Figma có timeline Motion (tính năng beta của Figma). Gói Figma Starter giới hạn số lượt gọi MCP; mỗi lần xuất tốn vài lượt.

## 2. Trong module có gì

| Đường dẫn | Assembly | Vai trò |
|---|---|---|
| `Runtime/FigmaEase.cs` | `FigmaMotion` | Dựng `AnimationCurve` trùng khít `cubic-bezier` của Figma |
| `Runtime/SpriteGroupAlphaAnimation.cs` | `FigmaMotion` | Component LitMotion tự viết: mờ cả nhóm sprite (Figma opacity của group). Có trong menu Add ▸ Custom/Sprite Group Alpha |
| `Editor/FigmaMotionImporter.cs` | `FigmaMotion.Editor` | Đọc JSON, quy đổi đơn vị, dựng GameObject còn thiếu, ghi `LitMotionAnimation` |
| `Editor/FigmaMotionWindow.cs` | `FigmaMotion.Editor` | Cửa sổ Importer, menu Tools, thanh overlay trên Scene view |
| `Exporter/export-figma-motion.js` | — | Script Plugin API đọc timeline Figma, trả về JSON. Unity coi là file thường, không biên dịch |

Namespace `FigmaMotion` / `FigmaMotion.Editor`. Module chỉ phụ thuộc Unity và LitMotion, không dính code game.

## 3. Mang sang dự án khác

### Đóng gói (ở dự án nguồn)

1. Chuột phải thư mục `Assets/_Modules/FigmaMotion` ▸ **Export Package…**
2. Giữ tick mọi file. Bỏ tick **Include dependencies** — module không cần asset nào khác.
3. Lưu thành `FigmaMotion.unitypackage`.

File `.unitypackage` **không mang theo LitMotion** (package UPM không đi qua unitypackage).

### Cài (ở dự án đích)

1. Thêm 2 dòng LitMotion ở mục 1 vào `Packages/manifest.json`, chờ Unity tải xong. Làm bước này **trước**, không thì import gói sẽ lỗi biên dịch.
2. **Assets ▸ Import Package ▸ Custom Package…** chọn `FigmaMotion.unitypackage`.
3. Kiểm tra: menu **Tools ▸ Figma Motion ▸ Importer** mở được; Scene view có thanh **Figma Motion** (không thấy thì bật ở menu ⋮ ▸ Overlays).
4. Assembly của game muốn gọi `SpriteGroupAlphaAnimation` / `FigmaEase` trực tiếp thì thêm reference `FigmaMotion` vào asmdef. Chỉ gọi `LitMotionAnimation.Play()` thì không cần.

Có thể dời thư mục đi chỗ khác trong `Assets` — asmdef tham chiếu theo tên, không theo đường dẫn.

## 4. Làm một animation mới

1. **Figma:** dựng animation trên timeline Motion cho **một group**. Group này sẽ là Root bên Unity.
2. **Xuất JSON:** gửi Claude link node Figma của group (và prefab + node Root nếu đã có). Claude sửa `CONFIG` trong `export-figma-motion.js` rồi chạy qua `use_figma`, lưu kết quả thành `Tên.figma-motion.json` trong `Assets`. Chi tiết `CONFIG` ở mục 5.
3. **Root:** tạo một GameObject rỗng làm Root (trong prefab hoặc scene). Root ứng với **tâm** group Figma. Nên thêm `SortingGroup` cho Root để `sortingOrder` của các sprite chỉ so với nhau, không lẫn với object khác.
4. **Dựng hierarchy:** **Tools ▸ Figma Motion ▸ Importer** ▸ chọn **Root** và **File JSON** (hoặc bật *Dán JSON*) ▸ **Dựng hierarchy**. Cách dựng ở mục 6.
5. **Gắn sprite:** export ảnh từ Figma như mọi khi, import vào Unity với pivot **Center**, rồi kéo vào các `SpriteRenderer` trống. Kích thước sprite trên màn nên bằng `width × pxToUnit` của layer Figma (xem JSON).
6. **Import:** trong cửa sổ Importer bấm **Kiểm tra** (chỉ đọc, báo lỗi và cảnh báo) ▸ **Import** ▸ Save prefab.
7. **Xem trước:** nút Play trong Inspector của `LitMotionAnimation`.
8. **Gọi trong code:** mục 7.
9. **Chỉnh số:** sửa Figma rồi xuất lại, hoặc sửa thẳng JSON, rồi mở Importer (file JSON lần trước đã chọn sẵn) ▸ **Import**. Figma có thêm layer mới thì bấm **Dựng hierarchy** lại — chỉ node còn thiếu được tạo.

Prefab đã dựng tay từ trước (như hộp khoá) thì bỏ bước 3–5: đặt `targets` trong `CONFIG` cho khớp tên node rồi Import thẳng.

Import **ghi đè toàn bộ** component của `LitMotionAnimation` trên Root (Undo được) và đặt Auto Play = None. Chỉnh tay trong Inspector sẽ mất ở lần import sau — chép số đã chỉnh ngược vào JSON.

## 5. Xuất JSON từ Figma

`CONFIG` đầu file `export-figma-motion.js`:

| Field | Ý nghĩa |
|---|---|
| `rootId` | Node id của group gốc trong Figma (lấy từ link, `node-id=373-1245` → `'373:1245'`) |
| `name` | Tên ghi vào JSON |
| `pxToUnit` | 1 px Figma bằng bao nhiêu unit Unity (xem mục 6) |
| `sampleRate` | Số mẫu/giây khi phải lấy mẫu (mặc định 120) |
| `targets` | **Tuỳ chọn.** Mặc định mỗi track trỏ theo đường dẫn trong cây Figma — khớp hierarchy do nút Dựng hierarchy tạo. Chỉ cần khi trỏ vào prefab có sẵn đặt tên khác: node id Figma → đường dẫn từ Root (`''` = Root) |

Script trả về `{ json, warnings }`. Nó **chỉ đọc**, không sửa file Figma.

**Cây layer (`nodes`)** — script đi xuống mọi nhánh còn chứa layer có animation. Nhánh nào bên dưới không còn gì chuyển động thì dừng lại, cả nhánh thành **một sprite** (art xuất nguyên nhánh đó thành một ảnh). Ví dụ group trang trí 10 vector đứng yên → 1 sprite.

- Layer ẩn bị bỏ qua. Layer TEXT, layer mask được cảnh báo: Unity dựng thành SpriteRenderer, cần tự đổi sang TextMeshPro / SpriteMask.
- Tên trùng giữa các layer anh em thành `Tên[0]`, `Tên[1]`… Tên có `/` đổi thành `-` (vì `/` ngăn đường dẫn).

Mọi số tính theo **group gốc**, không theo toạ độ world hay page:

- Figma ghi độ dời theo **trục riêng của layer**. Layer bị lật hoặc xoay (kể cả do group cha) thì "trượt trái" trên số có thể là trượt phải trên màn — script nhân độ dời với hướng của layer so với group gốc. Ví dụ tấm phải của hộp khoá bị lật ngang: số Figma −37 thành **+37** trong JSON.
- Scale theo trục riêng của layer: layer xoay 90° thì scaleX của nó thành `scaleY` trong JSON. Lật không đổi độ lớn scale.
- Góc xoay đổi dấu khi layer bị lật.
- Một thuộc tính có thể là tổng nhiều track (preset + keyframe tay), mỗi track có mốc bắt đầu riêng. Không chồng nhau thì giữ nguyên easing từng đoạn; chồng nhau thì lấy mẫu `sampleRate` lần/giây (cộng mọi mép đoạn) và nội suy thẳng.
- Easing spring (Gentle, Quick, Bouncy…) chưa hỗ trợ — script cảnh báo và tạm dùng ease-out.
- Timeline Figma chạy lặp, nhưng importer không tự lặp: animation chạy một lần.

Không có Figma MCP thì vẫn viết JSON tay được theo mục 6.

## 6. Định dạng JSON (`figma-motion/v1`)

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
    { "target": "Lit (1)", "property": "x",
      "keys": [ { "time": 0, "value": 0 }, { "time": 0.229, "value": 30 }, { "time": 0.458, "value": 37 } ] },
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
| `ease` | Easing mặc định của track |
| `keys` | Ít nhất 2 key, `time` tăng dần. Key có thể tự ghi `ease` cho **đoạn từ nó tới key sau** |

Easing: `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out`, hoặc `cubic-bezier(x1, y1, x2, y2)` chép từ ô *Custom* của Figma (y được vượt 0..1 để có overshoot).

### Importer quy đổi thế nào

1. **Đọc và kiểm:** thiếu track, track dưới 2 key, `time` không tăng, easing hay property lạ → báo lỗi, **không ghi gì**.
2. **Tìm node** theo `target`; tên trùng mà không ghi chỉ số thì báo lỗi kèm gợi ý `Tên[0]…`, không tự đoán.
3. **Đổi số**, lấy chênh lệch so với key đầu:

   | property | Công thức | Component LitMotion |
   |---|---|---|
   | `x` | `(v − v0) × pxToUnit` | `TransformPositionAnimation` |
   | `y` | `(v − v0) × pxToUnit × −1` | `TransformPositionAnimation` |
   | `rotation` | `v − v0` | `TransformRotationAnimation` |
   | `scale*` | `(v / v0 − 1) × scale author của node` | `TransformScaleAnimation` |
   | `opacity` | `v` (tuyệt đối) | `SpriteGroupAlphaAnimation` (sprite) · `CanvasGroupAlphaAnimation` (UI, tự thêm `CanvasGroup`) |

4. **Dựng curve:** `start` = key đầu, `end` = key lệch xa nhất (track "đi rồi về" vẫn đúng). Mọi key chuẩn hoá về 0..1 rồi nối thành một `AnimationCurve`, mỗi đoạn trùng khít `cubic-bezier` của nó. `delay` = `time` key đầu, `duration` = key cuối − key đầu.
5. **Ghi** vào `LitMotionAnimation` trong một bước Undo.

Mọi track trừ opacity đặt **Relative**: lúc `Play()` LitMotion chụp vị trí/xoay/scale hiện tại rồi **cộng** giá trị animation lên, nên đặt node ở đâu trong prefab cũng chạy đúng; `Stop()` trả về giá trị đã chụp. Track nào giá trị không đổi thì bỏ qua và cảnh báo.

### Cây layer (`nodes`) và nút Dựng hierarchy

Mảng tuỳ chọn, cha đứng trước con, theo thứ tự layer Figma từ dưới lên:

```json
"nodes": [
  { "path": "Panel", "x": 30, "y": -10, "rotation": 0, "scaleX": -1, "scaleY": 1,
    "width": 40, "height": 60, "opacity": 0.5, "sprite": false },
  { "path": "Panel/Door", "x": -10, "y": 0, "rotation": 0, "scaleX": 1, "scaleY": 1,
    "width": 20, "height": 60, "opacity": 1, "sprite": true }
]
```

| Field | Ý nghĩa |
|---|---|
| `path` | Cùng cú pháp `target` của track |
| `x`, `y` | Tâm layer so với tâm layer cha, px, theo trục của cha (y Figma hướng xuống) |
| `rotation` | Độ, dương = ngược chiều kim đồng hồ |
| `scaleX`, `scaleY` | Âm = lật (script để lật ngang ở `scaleX`) |
| `width`, `height` | px — để đối chiếu khi gắn sprite, importer không dùng |
| `opacity` | Của riêng layer |
| `sprite` | `true` = lá, thêm `SpriteRenderer` |

**Dựng hierarchy** làm:

1. Với từng node, node **đã có** thì giữ nguyên, kể cả vị trí chỉnh tay. Chỉ node còn thiếu được tạo.
2. Node mới: `localPosition = (x, −y) × pxToUnit`, xoay quanh trục z, `localScale = (scaleX, scaleY, 1)`.
3. Lá có `SpriteRenderer` **trống** (gắn sprite tay). `sortingOrder` = 0, 1, 2… theo thứ tự lá trong JSON. Alpha = tích opacity của lá với mọi group cha (group không có component nào để giữ opacity).
4. Toàn bộ là một bước Undo. Root là UI (`RectTransform`) thì báo lỗi — mới hỗ trợ sprite.

## 7. Gọi trong code

```csharp
using LitMotion.Animation;

var anim = root.GetComponent<LitMotionAnimation>();
anim.Stop();              // trả node về trạng thái lúc Play trước (nếu đang chạy dở)
anim.Play();
while (anim.IsPlaying) yield return null;   // coroutine: LitMotionAnimation không báo trước tổng thời lượng
```

- `Stop()` gọi `OnStop` từng component: vị trí, scale, alpha về giá trị chụp lúc `Play()`.
- `SpriteGroupAlphaAnimation` chụp lại alpha của mọi sprite **mỗi lần Play**, nên code được phép đổi alpha trước khi gọi `Play()`.
- Không destroy hoặc tắt Root khi animation còn chạy — chờ `IsPlaying` về false rồi mới dọn.

## 8. Bẫy thường gặp

- **Pivot:** Figma scale và xoay quanh tâm node. Pivot lệch thì kết quả lệch theo — nút *Kiểm tra* cảnh báo.
- **Tên trùng:** phải ghi `Tên[i]`.
- **Sprite lệch kích thước:** Dựng hierarchy không co giãn sprite. Ảnh xuất ở tỉ lệ khác hoặc PPU sai thì sprite to/nhỏ hơn layer Figma — so với `width × pxToUnit` trong JSON. Sprite bị cắt viền trong suốt (trim) làm tâm lệch khỏi tâm layer.
- **Dựng hierarchy không sửa node đã có:** layout Figma đổi thì xoá node cũ rồi dựng lại, hoặc chỉnh tay.
- **Chỉnh tay trong Inspector** mất khi import lại.
- **Màu khi mờ dần khác Figma ở đoạn giữa:** project ở Linear color space trộn lớp trong suốt theo ánh sáng tuyến tính, Figma trộn theo sRGB. Đen 50% trên trắng: Figma ra `#808080`, Unity ra `#BCBCBC`. Đầu và cuối (100%, 0%) giống nhau. Animation mờ dài mà cần khớp thì chỉnh curve alpha trong JSON bằng mắt; không đáng đổi cả project sang Gamma.
- **Opacity của group:** Figma vẽ gộp group rồi mới làm mờ, các lớp bên trong không lộ ra nhau. `SpriteGroupAlphaAnimation` mờ từng sprite, nên ở đoạn giữa chỗ các sprite chồng nhau nhìn xuyên được. Fade ngắn thì không thấy; cần khớp tuyệt đối thì phải vẽ group vào render texture.
- **Thiếu uGUI:** dự án không có `com.unity.ugui` thì `CanvasGroupAlphaAnimation` không tồn tại và `FigmaMotion.Editor` lỗi biên dịch.

## Trong repo CategorySort

- Mẫu thật: `Assets/_Game/Art/Blocker/Blocker_Box/LockOpen.figma-motion.json` — mở hộp khoá nhóm, Root = `Lock Root` trong `Assets/Prefabs/Box.prefab`, gọi từ `BoxView.OpenGroupLock`.
- `Assets/_Game/Board/Editor/LockOpenAnimationBuilder.cs` — sửa cấu trúc prefab một lần rồi gọi importer (riêng của game, không nằm trong module).
- Test EditMode: `Assets/_Game/Board/Tests/FigmaEaseTests.cs`, `FigmaMotionImporterTests.cs`, `SpriteGroupAlphaAnimationTests.cs`.
- Kiểm script xuất không cần Figma: `node tools/figma-motion/check-export.mjs` chạy nó trên dữ liệu thật của hộp khoá (so với số Figma) và trên một cây giả (lồng, lật, xoay, trùng tên, ẩn, TEXT).
