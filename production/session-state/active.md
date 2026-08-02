# Session State

> Cập nhật cuối: 2026-08-02. File này là điểm bàn giao giữa các phiên — đọc trước khi làm gì.

## Đang ở đâu

**Giai đoạn 1b — Prototype vứt đi**, bước 2 (port sang Unity). Concept ở **Rev 4**, status **Draft**.

Dự án đã **pivot** khỏi *Category Sort* (collector/quota/move budget) sang **WordStack**.
Core loop xác nhận bằng demo HTML chơi thử được; user duyệt "logic coregame này ổn".
Mọi mô tả gameplay Rev 3 trở về trước đã hết hiệu lực.

**Xong:** P-doc · P1a/P1b (domain + validate + solver + SelfCheck) · P2–P4 (view) · Level Editor · 3 level.
**Chặn:** thiếu 9 file PNG — xem mục "Chặn" bên dưới.

## Luật chơi hiện hành

Nguồn chân lý: `design/gdd/game-concept.md` mục "Luật chơi cốt lõi"; hành vi tham chiếu:
`demo/wordstack-clear-demo.html`. Tóm tắt để khỏi phải mở file:

- Bàn chơi = các **stack**, mỗi stack là nhiều **hộp 4 slot xếp chồng**. Chỉ **hộp trên cùng** hoạt động; hộp dưới có sẵn thẻ nhưng bị che.
- **Chỉ 1 loại nước đi**: kéo thẻ trong hộp trên cùng sang hộp trên cùng của stack khác, vào **slot trống đầu tiên**. Hộp đích đầy → từ chối. Không giới hạn nước đi, không timer.
- Gom đủ **4 thẻ của một group vào cùng một hộp** → **CLEAR**. Hộp rỗng mà không phải hộp đáy → bị xoá, hộp dưới lộ ra. Sau mỗi nước đi chạy dây chuyền tới khi bàn đứng yên.
- **Màu gợi ý** cục bộ theo từng hộp: group ≥2 thẻ mới được tô màu.
- **Thắng** = sạch bàn. **Kẹt** = mọi hộp trên cùng đầy. Không có màn Thua.
- **Chưa có**: COLLAPSE, Undo, Hint, âm thanh, move budget.

## Schema level

`layout` (vị trí) tách khỏi `meaning` (ý nghĩa). Card **lồng trong group** của nó:

```jsonc
"meaning": { "groups": [
  { "id":"fruit", "text":"Fruit", "cards": [
      { "id":"apple",  "text":"Apple",  "art":"apple"  },
      { "id":"banana",                  "art":"banana" },   // chỉ ảnh
      { "id":"grape",  "text":"Grape"                  },   // chỉ chữ
      { "id":"orange", "text":"Orange", "art":"orange" }
  ]}
]}
```

Chi tiết đầy đủ + 11 luật validate: `docs/wordstack-design-log.md` Mục 3.
`art` là **tên file** trong `Assets/Prototype/Resources/Art/`, load bằng
`Resources.Load<Sprite>("Art/" + key)`.

## Đã làm, đã kiểm chứng

| File | Nội dung |
|------|----------|
| `demo/wordstack-clear-demo.html` | Demo web self-contained, 2 level. **Bản tham chiếu hành vi** — port Unity lệch chỗ nào là bug chỗ đó |
| `demo/check.mjs` | Bộ check của demo; trích engine thẳng từ file HTML. Chạy: `node demo/check.mjs` |
| `Assets/Prototype/PrototypeDomain.cs` | Luật + model + mini JSON reader + validate + beam solver + `SelfCheck`. **Không import UnityEngine** |
| `Assets/Prototype/PrototypeView.cs` | View tự bootstrap: render stack/hộp/lớp lấp ló/thẻ, kéo thả, coroutine cascade, HUD, win/stuck |
| `Assets/Prototype/Editor/LevelEditorWindow.cs` | Tool xếp level (`WordStack ▸ Level Editor`) |
| `Assets/Prototype/PrototypeSelfCheckMain.cs` | Entry console (`#if !UNITY_5_3_OR_NEWER`) |
| `Assets/Prototype/Resources/Levels/lv-00{1,2,3}.json` | 3 level |
| `Assets/Prototype/Resources/Art/*.png` | 3 PNG **placeholder** (apple/banana/orange) — ghi đè bằng art thật |

Số liệu solver **trùng khít** giữa C# và `demo/check.mjs` (6/6/9/9 nước,
31322/32318/96530/104202 nút) — cùng đường duyệt, cùng kết quả, tức port đúng hành vi,
không chỉ đúng kết quả. C# nhanh hơn ~13×.

## Chặn / cần quyết định

**Thiếu 9 file art PNG.** SelfCheck dừng ở preflight và in đủ danh sách. Thả vào
`Assets/Prototype/Resources/Art/`:

```
dog.png  cat.png  bear.png  car.png  airplane.png
bicycle.png  guitar.png  piano.png  violin.png
```

Chưa có thì chỉ **lv-003** render được (bấm phím `3` trong game). Sáu thẻ
`grape · rabbit · bus · drum` là **chỉ-chữ**, không cần art.

**`PrototypeView.cs` chưa ai nhìn thấy chạy** — compile sạch và Unity đã nhận, nhưng render,
kéo thả, nhịp cascade phải bấm Play mới nghiệm thu được, mà bấm Play cần art.

Unity `6000.3.8f1` khớp `ProjectVersion.txt`.

## Ghi chú kỹ thuật

- **Chạy SelfCheck không cần mở Unity** — vòng phản hồi nhanh nhất khi sửa luật hoặc sửa level:

  ```bash
  ./selfcheck.sh
  ```

  Tự đọc version từ `ProjectSettings/ProjectVersion.txt`, dùng Roslyn đi kèm Unity Hub (không
  cần .NET SDK riêng), build vào `Temp/` (đã gitignore). ~3 giây.

  Hai bẫy đã gặp, script né sẵn: đường dẫn Unity có dấu cách nên **phải** truyền `-r` qua
  response file; và build vào `%TEMP%` của user bị Application Control policy chặn thực thi.

- `PrototypeDomain.cs` **không được import UnityEngine** — mất ràng buộc này là mất luôn
  `./selfcheck.sh`. Đây là lý do `BoxColorIndices` trả **index** palette chứ không trả màu, và
  `Validate` nhận `Predicate<string> hasArt` do host cấp thay vì tự gọi `Resources.Load`.

- **MCP Unity**: bridge do package `com.unity.ai.assistant` cung cấp. Nó nằm trong
  `Packages/manifest.json` của worktree nhưng **cố ý chưa commit** (giống repo chính) vì là
  tooling, không phải dependency của game. Bridge đăng ký theo **project path**, nên Unity mở ở
  repo chính sẽ không điều khiển được worktree — phải mở Editor trỏ đúng worktree. Bridge rớt
  mỗi lần domain reload; đợi vài giây là lên lại.

- **Game feel** trong `PrototypeView.cs` theo `D:\Balatro-Feel` (`CardVisual.cs`), tự viết lerp
  thay DOTween: ghost lerp đuổi cursor, xoay Z theo độ trễ, tilt sin/cos, scale pop, shadow,
  snap-back khi thả hụt, hộp rung khi từ chối, hover phồng nhẹ.

- File `.md` trong repo bị git chuyển sang CRLF. Sửa bằng công cụ khớp chuỗi LF sẽ trượt —
  normalize về LF trước (git tự chuẩn hoá lúc commit nên không tạo diff giả).

## Câu hỏi mở

Bảng Q1-Q14 (mâu thuẫn / lỗ hổng trong GDD gốc + giả định mặc định) ở
`docs/wordstack-design-log.md` Mục 7. Còn treo:

- `Rules.RemoveEmptyNonBottomBox` đang `true`. Cả 3 level chạy được ở **cả hai** chế độ nên đây
  là lựa chọn tự do, không bị data ép. Cân nhắc đảo về `false` (đọc chặt §7) nếu Undo quay lại.
- Nguồn art thật + license.
- COLLAPSE / Undo / Hint — schema đã chừa đường (`group.group`), validate đang chặn.
