# Session State

> Cập nhật cuối: 2026-08-03. File này là điểm bàn giao giữa các phiên — đọc trước khi làm gì.

## Đang ở đâu

**Giai đoạn 1b ĐÃ ĐÓNG** — user chơi thử bản Unity và xác nhận *"phần game ok"*.
`design/gdd/game-concept.md` đã chuyển **Approved** (2026-08-03).

Việc đang mở: **chuyển view từ dựng-runtime sang prefab** theo thiết kế
`docs/architecture/view-prefabs.md` (commit `f18e30c`) — đang **chờ user duyệt thiết kế**.
User sẽ tự tay dựng prefab trong Editor; xem phân công bên dưới.

## Luật chơi hiện hành

Không đổi so với phiên trước. Nguồn chân lý: `design/gdd/game-concept.md` mục "Luật chơi cốt lõi";
hành vi tham chiếu: `demo/wordstack-clear-demo.html`. Domain thuần + SelfCheck + solver vẫn nguyên.

## Kế hoạch prefab (tóm tắt `docs/architecture/view-prefabs.md`)

- **5 prefab** tại `Assets/Prefabs/`: `Tile` (TileView) · `Box` (BoxView) · `Stack` (StackView,
  3 lớp lấp ló dựng sẵn) · `Ghost` (GhostView, mọi hằng feel thành SerializeField) · `Hud` (HudView).
- **Scene `Assets/Scenes/Main.unity`**: camera + `Game` (BoardController giữ tham chiếu prefab + palette).
  Xoá cơ chế tự bootstrap `RuntimeInitializeOnLoadMethod`.
- 2 quyết định nền đã ghi trong doc: **giữ nhịp rebuild-toàn-bàn** (chỉ thay `new GameObject` →
  `Instantiate`), **giữ TextMesh** (TMP thiếu glyph tiếng Việt ở font mặc định).
- Domain không đổi một dòng.

**Trình tự (phân công):**
1. User duyệt thiết kế *(đang chờ — chưa duyệt tính đến 2026-08-03)*.
2. Tôi viết 5 script view + sườn BoardController (compile sạch khi chưa có prefab).
3. User dựng 5 prefab + scene bằng tay theo **checklist Mục 6** của doc, gắn script, kéo tham chiếu.
4. Tôi chuyển BoardController sang Instantiate, xoá code dựng runtime, rà prefab YAML, nghiệm thu
   (Play 3 level + SelfCheck pass + hành vi khớp demo).

## Đã làm, đã kiểm chứng

| File | Nội dung |
|------|----------|
| `demo/wordstack-clear-demo.html` + `demo/check.mjs` | Bản tham chiếu hành vi + bộ check (`node demo/check.mjs`) |
| `Assets/Prototype/PrototypeDomain.cs` | Luật + validate + beam solver + SelfCheck. **Không import UnityEngine** |
| `Assets/Prototype/PrototypeView.cs` | View runtime hiện hành — sẽ được thay dần theo kế hoạch prefab |
| `Assets/Prototype/Editor/LevelEditorWindow.cs` | Tool xếp level (`WordStack ▸ Level Editor`) |
| `Assets/Prototype/Resources/Levels/lv-00{1,2,3}.json` | 3 level, solver khớp demo (6/6/9/9/2/2 nước) |
| `Assets/Prototype/Resources/Art/*.png` (+`.meta`) | 12 placeholder có nhãn; meta đã commit (GUID ổn định) |
| `docs/architecture/view-prefabs.md` | Thiết kế prefab — chờ duyệt |

## Chặn / cần quyết định

- **User duyệt thiết kế prefab** (bước 1 ở trên) — mọi việc code tiếp đang chờ cái này.
- Nguồn art thật + license (Twemoji/OpenMoji cần user duyệt vì phải tải file).

## Ghi chú kỹ thuật (giữ từ phiên trước — vẫn đúng)

- **`./selfcheck.sh`** chạy SelfCheck ngoài Unity (~3s), tự đọc version từ `ProjectVersion.txt`,
  dùng Roslyn của Unity Hub. Bẫy đã né: đường dẫn có dấu cách → `-r` qua response file;
  build vào `%TEMP%` bị Application Control chặn → build vào `Temp/` của project.
- `PrototypeDomain.cs` **không được import UnityEngine** — mất ràng buộc là mất `./selfcheck.sh`.
  Vì vậy `BoxColorIndices` trả index palette, `Validate` nhận `Predicate<string> hasArt`.
- **MCP Unity bridge** (`com.unity.ai.assistant`): đăng ký theo project path, rớt mỗi domain reload
  (đợi vài giây). Package trong manifest cố ý chưa commit (tooling, không phải dependency).
- File `.md` bị git đổi CRLF — normalize LF trước khi sửa bằng công cụ khớp chuỗi.
- Câu hỏi mở Q1-Q14: `docs/wordstack-design-log.md` Mục 7. `Rules.RemoveEmptyNonBottomBox=true`
  là lựa chọn tự do (3 level chạy được cả hai chế độ) — cân nhắc lại nếu làm Undo.
