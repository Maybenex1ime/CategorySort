---
name: query-me
description: "Kỹ năng tra khảo chuyên sâu — kích hoạt khi người dùng nói 'Tra khảo tôi về [chủ đề]', '/query-me [chủ đề]', hoặc muốn làm rõ thiết kế/logic cho một tính năng trước khi bắt tay code. Skill này trích xuất toàn bộ ngữ cảnh ẩn, ràng buộc kỹ thuật, và quyết định thiết kế từ đầu người dùng thông qua tra khảo có cấu trúc — từng câu một, có đề xuất Options. Luôn kích hoạt skill này khi người dùng cần brainstorm, làm rõ spec, hoặc muốn đạt output 90% chính xác ngay từ lần đầu thay vì sửa đi sửa lại."
---

# QUERY ME — Knowledge Interrogator

## Mục tiêu

Trích xuất toàn bộ logic ngầm, ràng buộc kỹ thuật, và tư duy thiết kế từ đầu người dùng. Mục tiêu là xây dựng một tài liệu spec đạt ~90% chính xác ngay từ phiên đầu, thay vì iterate nhiều vòng vì thiếu context.

## Lệnh kích hoạt

- `"Tra khảo tôi về [chủ đề]"`
- `"/query-me [chủ đề]"`
- Bất kỳ khi người dùng muốn làm rõ thiết kế/spec một tính năng trước khi bắt đầu làm

## Quy trình (thực hiện theo đúng thứ tự)

### Bước 1 — Đọc tài liệu hiện có TRƯỚC

Trước khi hỏi bất kỳ câu nào, hãy chủ động tìm và đọc:
- Tất cả file `.md` liên quan đến chủ đề trong project
- Các file GDD, design doc, hoặc spec đã có

Nếu câu trả lời đã có trong tài liệu → **không hỏi lại**, chỉ xác nhận nhanh. Chỉ hỏi khi thực sự thiếu thông tin.

### Bước 2 — Khởi tạo file brainstorm (Checkpoint)

Tạo file: `brainstorm/[slug-chu-de].md` với cấu trúc sau:

```markdown
---
# BẢN THẢO BRAINSTORM: [Tên Chủ Đề]
**Ngày:** [date]
**Trạng thái:** 🟡 Đang tra khảo

## 1. Tóm tắt & Quyết định cốt lõi
## 2. Sơ đồ Logic / Core Mechanics / Bảng số
## 3. Nhật ký Hỏi Đáp (Q&A Log)
| # | Câu hỏi | Câu trả lời | Quyết định chốt |
|---|---|---|---|
## 4. [OPEN FLAGS] — Điểm chưa rõ, cần kiểm tra hoặc xác nhận thêm
## 5. Kết quả cuối phiên
---
```

### Bước 3 — Tra khảo (Nguyên tắc bất biến)

- **CHỈ HỎI TỪNG CÂU MỘT.** Không bao giờ list nhiều câu cùng lúc.
- Hỏi theo thứ tự dependency: quyết định cốt lõi trước, chi tiết sau.
- Mỗi câu hỏi phải kèm **2–3 Options** (A/B/C) với hệ quả ngắn gọn để người dùng chọn nhanh hoặc phản biện.

**Format câu hỏi chuẩn:**

```
**Câu hỏi [N]:** [Câu hỏi một chiều, rõ ràng]

- **Option A:** [Mô tả] → Hệ quả: [...]
- **Option B:** [Mô tả] → Hệ quả: [...]
- **Option C (Custom):** Tự điền

*(Gợi ý: Option [X] vì [...] — nhưng đây là quyết định của bạn)*
```

**Khi gặp điểm chưa rõ** mà không cần hỏi ngay (cần kiểm tra codebase, cần hỏi team, hoặc không block câu hỏi tiếp theo) → ghi vào `[OPEN FLAGS]`, tiếp tục hỏi câu tiếp theo.

### Bước 4 — Cập nhật checkpoint sau MỖI câu trả lời

Sau mỗi câu trả lời của người dùng:
1. Xác nhận ngắn: *"Ghi nhận: [quyết định]."*
2. Cập nhật file brainstorm (Q&A Log + Quyết định cốt lõi)
3. Đặt câu hỏi tiếp theo ngay

Việc cập nhật liên tục đảm bảo context không bị mất khi đoạn chat kéo dài.

### Bước 5 — Kết thúc phiên

Dừng khi không còn kẽ hở logic nào. Sau đó:
1. Đổi trạng thái file: `✅ Hoàn tất tra khảo`
2. Điền mục "Kết quả cuối phiên" — tóm tắt tất cả quyết định đã chốt
3. Hỏi người dùng: *"Bạn có muốn tôi cập nhật các quyết định vừa chốt vào tài liệu chính của dự án không?"*

## Tại sao approach này hiệu quả

Cách làm thông thường (đưa request ngắn → nhận output 60% → sửa đi sửa lại) tốn nhiều token và context window bị phình to bởi hội thoại rác. Tra khảo có cấu trúc dành 15–30 phút ban đầu để làm rõ toàn bộ "luật chơi" — kết quả là output đầu ra đạt ~90% ngay lần đầu, ít phải iterate hơn.
