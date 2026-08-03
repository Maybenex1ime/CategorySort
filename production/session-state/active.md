# Session State

> Cập nhật cuối: 2026-08-03. File này là điểm bàn giao giữa các phiên — đọc trước khi làm gì.

## Đang ở đâu

**Giai đoạn 1b ĐÃ ĐÓNG** — user chơi thử bản Unity và xác nhận *"phần game ok"*.
`design/gdd/game-concept.md` đã chuyển **Approved** (2026-08-03).

Việc đang mở: **chuyển view sang prefab + retained-mode** theo `docs/architecture/view-prefabs.md`.
Mục 1 của doc đã chốt (Q1 retained-mode, Q3 DOTween); **phần còn lại — 5 prefab, scene, phân
công — vẫn chờ user duyệt**. Mọi việc code tiếp đang chờ cái này.

## Luật chơi hiện hành

Không đổi. Nguồn chân lý: `design/gdd/game-concept.md` mục "Luật chơi cốt lõi"; hành vi tham
chiếu: `demo/wordstack-clear-demo.html`. Domain thuần + SelfCheck + solver vẫn nguyên.

## Kế hoạch prefab (tóm tắt `docs/architecture/view-prefabs.md`)

- **5 prefab** tại `Assets/Prefabs/`: `Tile` (TileView) · `Box` (BoxView) · `Stack` (StackView,
  3 lớp lấp ló dựng sẵn) · `Ghost` (GhostView, hằng feel thành SerializeField) · `Hud` (HudView).
- **Scene `Assets/Scenes/Main.unity`**: camera + `Game` (BoardController giữ tham chiếu prefab +
  palette). Xoá cơ chế tự bootstrap `RuntimeInitializeOnLoadMethod`.
- **Q1 = retained-mode** (user chốt, lật lựa chọn ban đầu của doc). `Rebuild()` tách thành
  `PlaceTile` / `RemoveTiles` / `RevealBox` / `RefreshColors` / `RefreshZones`.
- **Q2 = giữ TextMesh** (TMP thiếu glyph tiếng Việt ở font mặc định).
- **Q3 = DOTween** (đã cài, đã adapt view hiện tại).
- Domain **không đổi một dòng**.

**Trình tự (phân công):**
1. User duyệt phần còn lại của thiết kế *(đang chờ)*.
2. Tôi viết 5 script view + sườn BoardController (compile sạch khi chưa có prefab).
3. User dựng 5 prefab + scene bằng tay theo **checklist Mục 6** của doc, gắn script, kéo tham chiếu.
4. Tôi chuyển sang Instantiate, xoá code dựng runtime, nghiệm thu (Play 3 level + SelfCheck pass).

Ba chỗ **phải mang nguyên sang** khi viết script view, đừng viết lại:

- Logic co chữ trong `Label()` — hằng `CharW = 0.085` là số **đo thật** bằng `Renderer.bounds`,
  không phải số đoán; và **không auto-wrap** (ngắt ở mọi khoảng trắng biến câu HUD thành cột dọc).
- **Invariant check** (doc Mục 4a): tập uid GameObject phải bằng đúng tập tile trong top box.
  Retained-mode mất tính "màn hình là hàm thuần của state" mà không test nào trong repo nhìn tới
  lớp view — đây là thứ thay thế.
- Mỗi nước đi làm **HAI hộp** đổi màu, không chỉ hộp đích.

## Đã làm, đã kiểm chứng

| File | Nội dung |
|------|----------|
| `demo/wordstack-clear-demo.html` + `demo/check.mjs` | Bản tham chiếu hành vi + bộ check (`node demo/check.mjs`) |
| `Assets/Prototype/PrototypeDomain.cs` | Luật + validate + beam solver + SelfCheck. **Không import UnityEngine** |
| `Assets/Prototype/PrototypeView.cs` | View runtime hiện hành, animation đã chạy DOTween — sẽ thay theo kế hoạch prefab |
| `Assets/Prototype/Editor/LevelEditorWindow.cs` | Tool xếp level (`WordStack ▸ Level Editor`) |
| `Assets/Prototype/Resources/Levels/lv-00{1,2,3}.json` | 3 level, solver khớp demo (6/6/9/9/2/2 nước) |
| `Assets/Prototype/Resources/Art/*.png` | 12 placeholder **có nhãn**; meta đã commit (GUID ổn định) |
| `Assets/Plugins/Demigiant/` | DOTween 843K, commit vào repo theo quyết định của user |
| `docs/architecture/view-prefabs.md` | Thiết kế prefab — Mục 1 đã chốt, phần còn lại chờ duyệt |

## Hai lệnh kiểm, chạy trước khi mở Unity

```bash
./selfcheck.sh      # luật + validate + solver, ~3s, không cần Unity
./compilecheck.sh   # compile cả 2 assembly C#, vài giây
```

`compilecheck.sh` phải **tách 2 assembly** đúng như Unity (`Assembly-CSharp` vs `-Editor`) vì hai
thế giới reference xung khắc: `DOTween.dll` build theo mscorlib, `UnityEditor.dll` theo netstandard
— nối bằng `Facades/netstandard.dll`.

## Chặn / cần quyết định

- **User duyệt phần còn lại của thiết kế prefab** — mọi việc code tiếp đang chờ.
- Nguồn art thật + license (12 file hiện tại là placeholder có nhãn).

## Ghi chú kỹ thuật

- `PrototypeDomain.cs` **không được import UnityEngine** — mất ràng buộc là mất `./selfcheck.sh`.
  Vì vậy `BoxColorIndices` trả index palette, `Validate` nhận `Predicate<string> hasArt`.

- **DOTween — hai cái bẫy đã gặp:**
  1. Setup **tự thêm define `DOTWEEN_EPO`** dù `DOTweenSettings` ghi `epoOutlineEnabled: 0`.
     Define bật → `DOTweenModuleEPOOutline.cs` compile → tham chiếu asset *Easy Performant Outline*
     không có → **hỏng toàn bộ compile**, Unity im lặng không sinh assembly mới. Đã gỡ khỏi 16
     target và cho settings khớp lại. **Chạy lại Setup có thể thêm lại** — gặp lỗi `Outlinable`
     thì biết ngay là cái này.
  2. `sr.DOFade` nằm trong **module Sprite tuỳ chọn**; dùng `DOTween.ToAlpha` (core) thì không
     phụ thuộc Setup có bật module hay không.
  - `ProjectSettings.asset` **phải commit** — thiếu define `DOTWEEN` là máy khác build ra hành vi khác.

- **Unity không tự compile khi mất focus.** Sau khi sửa `.cs`, phải đợi `Assembly-CSharp.dll` mới
  hơn file nguồn rồi mới verify — nếu không sẽ nghiệm thu nhầm bản cũ (đã dính 2 lần).

- **`Unity_Camera_Capture` của MCP trả ảnh trắng** — nó lấy Scene View. Muốn ảnh game thật thì tự
  render camera ra `RenderTexture` rồi `EncodeToPNG`.

- **MCP Unity bridge** (`com.unity.ai.assistant`): đăng ký theo **project path** nên Unity mở ở repo
  chính không điều khiển được worktree. Rớt mỗi domain reload, đợi vài giây. Package trong
  `manifest.json` cố ý chưa commit (tooling, không phải dependency).

- **Branch dùng chung với phiên Claude khác** — `git fetch` trước khi làm việc dài, không phải lúc
  sắp push. Đã một lần trùng việc vì bỏ qua bước này.

- File `.md` bị git đổi CRLF — normalize LF trước khi sửa bằng công cụ khớp chuỗi.

- Câu hỏi mở Q1-Q14: `docs/wordstack-design-log.md` Mục 7. `Rules.RemoveEmptyNonBottomBox=true`
  là lựa chọn tự do (3 level chạy được cả hai chế độ) — cân nhắc lại nếu làm Undo.
