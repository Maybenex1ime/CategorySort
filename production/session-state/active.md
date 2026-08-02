# Session State

> Cập nhật cuối: 2026-08-02. File này là điểm bàn giao giữa các phiên — đọc trước khi làm gì.

## Đang ở đâu

**Giai đoạn 1b — Prototype vứt đi**, bước 2 (port sang Unity). Concept ở **Rev 4**, status vẫn **Draft**.

Dự án đã **pivot** khỏi *Category Sort* (collector/quota/move budget) sang **WordStack**.
Core loop đã xác nhận bằng demo HTML chơi thử được, user duyệt "logic coregame này ổn".
Mọi mô tả gameplay Rev 3 trở về trước đã hết hiệu lực.

**Xong:** P-doc, P1a, P1b (domain + validate + engine + solver + SelfCheck).
**Đang tới:** P2 (view tĩnh) → P3 (kéo thả) → P4 (cascade + trạng thái). Bảng phase ở `docs/development-plan.md` Giai đoạn 1b.

## Luật chơi hiện hành

Nguồn chân lý: `design/gdd/game-concept.md` mục "Luật chơi cốt lõi"; hành vi tham chiếu:
`demo/wordstack-clear-demo.html`. Tóm tắt để khỏi phải mở file:

- Bàn chơi = các **stack**, mỗi stack là nhiều **hộp 4 slot xếp chồng**. Chỉ **hộp trên cùng** hoạt động; hộp dưới có sẵn thẻ nhưng bị che.
- **Chỉ 1 loại nước đi**: kéo thẻ trong hộp trên cùng sang hộp trên cùng của stack khác, vào **slot trống đầu tiên**. Hộp đích đầy → từ chối. Không giới hạn nước đi, không timer.
- Gom đủ **4 thành viên một group vào cùng một hộp** → **CLEAR**. Hộp rỗng mà không phải hộp đáy → bị xoá, hộp dưới lộ ra. Sau mỗi nước đi chạy dây chuyền tới khi bàn đứng yên.
- **Màu gợi ý** cục bộ theo từng hộp: group ≥2 thẻ mới được tô màu.
- **Thắng** = sạch bàn. **Kẹt** = mọi hộp trên cùng đầy. Không có màn Thua.
- **Chưa có**: COLLAPSE, Undo, Hint, âm thanh, move budget.

## Đã làm, đã kiểm chứng

| File | Nội dung |
|------|----------|
| `demo/wordstack-clear-demo.html` | Demo web self-contained, 2 level, chơi được bằng chuột + ngón tay. **Bản tham chiếu hành vi** — port Unity lệch chỗ nào là bug chỗ đó |
| `demo/check.mjs` | Bộ check của demo; trích engine thẳng từ file HTML nên test đúng code đang chạy. Chạy: `node demo/check.mjs` |
| `Assets/Prototype/PrototypeDomain.cs` | Toàn bộ luật + model + mini JSON reader + validate + beam solver + `SelfCheck`. **Không import UnityEngine** |
| `Assets/Prototype/PrototypeSelfCheckMain.cs` | Entry console (`#if !UNITY_5_3_OR_NEWER`), đọc level + art bằng `System.IO` |
| `Assets/Prototype/Resources/Levels/lv-00{1,2}.json` | 2 level, schema `layout` + `meaning` |
| `Assets/Prototype/PrototypeView.cs` | **CHƯA port** — vẫn là view của luật cũ. Đây là việc của P2-P4 |

SelfCheck phủ: 12 loại level hỏng bị validate chặn · luật nước đi (thả về chỗ cũ, slot trống đầu tiên,
hộp bị che, hộp đầy + state không đổi) · CLEAR + xoá hộp + lộ hộp dưới · CLEAR ở hộp đáy · màu 3 case ·
thắng/kẹt · solver chứng minh mọi level giải được ở **cả hai** cách đọc luật xoá hộp.

Số liệu solver **trùng khít** giữa C# và `demo/check.mjs` (6/6/9/9 nước, 31322/32318/96530/104202 nút)
— cùng đường duyệt, cùng kết quả, tức port đúng hành vi. C# nhanh hơn ~13×.

## Chặn / cần quyết định

**Thiếu 12 file art PNG.** SelfCheck dừng ở preflight và in đủ danh sách. Thả vào
`Assets/Prototype/Resources/Art/`:

```
apple.png  banana.png  orange.png  dog.png       cat.png    bear.png
car.png    airplane.png bicycle.png guitar.png   piano.png  violin.png
```

Bốn thẻ `grape · rabbit · bus · drum` là **chỉ-chữ**, không cần art. Luật "mỗi level phải có ≥1 thẻ
chỉ-ảnh và ≥1 thẻ chỉ-chữ" giữ cho "chữ và ảnh ngang vai" là thuộc tính kiểm được.

Unity `6000.3.8f1` đã cài trong Hub, khớp `ProjectVersion.txt` và `docs/engine-reference/unity/VERSION.md`.

## Ghi chú kỹ thuật

- **Chạy SelfCheck không cần mở Unity** — vòng phản hồi nhanh nhất khi sửa luật hoặc sửa level:

  ```bash
  ./selfcheck.sh
  ```

  Script tự đọc version từ `ProjectSettings/ProjectVersion.txt`, dùng Roslyn đi kèm Unity Hub
  (không cần cài .NET SDK riêng), build vào `Temp/` (đã gitignore). Chạy hết ~3 giây.

  Hai cái bẫy đã gặp, script né sẵn: đường dẫn Unity có dấu cách nên **phải** truyền `-r` qua
  response file; và build vào `%TEMP%` của user bị Application Control policy chặn thực thi.

- `PrototypeDomain.cs` **không được import UnityEngine** — mất ràng buộc này là mất luôn `./selfcheck.sh`.
  Đây là lý do `boxColors` trả **index** palette chứ không trả màu, và validate nhận
  `Predicate<string> hasArt` do host cấp thay vì tự gọi `Resources.Load`.

- **Game feel** đã cấy sẵn trong `PrototypeView.cs` theo `D:\Balatro-Feel` (`CardVisual.cs`), tự viết lerp
  thay DOTween: ghost lerp đuổi cursor, xoay Z theo độ trễ, tilt sin/cos, scale pop, shadow tách lớp,
  snap-back khi thả hụt, punch khi nhận thẻ, hover phồng nhẹ. Khối này **độc lập với luật** nên P3 tái
  dùng gần như nguyên; chỉ phần vẽ bàn (lưới-slot → stack-of-box) phải viết lại.

- File `.meta` cho `Resources/` và các `.json` sẽ do Unity sinh lần mở Editor tiếp theo.

## Câu hỏi mở

Bảng Q1-Q14 (mâu thuẫn / lỗ hổng trong GDD gốc + giả định mặc định) ở `docs/wordstack-design-log.md` Mục 7.
Hai chỗ còn treo:

- `Rules.RemoveEmptyNonBottomBox` đang `true`. Cả 2 level chạy được ở **cả hai** chế độ nên đây là lựa
  chọn tự do, không bị data ép. Cân nhắc đảo về `false` (đọc chặt §7) nếu Undo quay lại.
- Nguồn art thật + license.
