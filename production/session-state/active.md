# Session State

> Cập nhật cuối: 2026-08-03. File này là điểm bàn giao giữa các phiên — đọc trước khi làm gì.

## Đang ở đâu

**Giai đoạn 1b ĐÃ ĐÓNG** — user chơi thử bản Unity và xác nhận *"phần game ok"*.
`design/gdd/game-concept.md` đã chuyển **Approved** (2026-08-03).

Việc đang mở: **chuyển view sang prefab + retained-mode** theo `docs/architecture/view-prefabs.md`
(doc đã **Approved**). Script view **đã viết xong** và compile sạch dù chưa có prefab nào.

**Đã dựng và chạy được** (2026-08-03): 5 prefab + `Main.unity` + `white.png` đã sinh bằng
`PrefabBuilder` và **đã commit kèm .meta**. Play lv-001: 4 stack · 8 thẻ · HUD đếm đúng · lớp lấp ló
hiện đúng chỗ · SelfCheck chạy từ `BoardController.Awake` và pass · **không có `View lệch`**.

**Chuỗi nước đi đã nghiệm thu** (2026-08-04, `WordStack ▸ Test ▸ Play 4 moves on lv-001`): 4 nước
đều nhận, CLEAR nổ (`1/3 groups · 4 moves`), hộp rỗng lùi ra, **hộp ẩn lộ ra kèm 4 thẻ mới**, lớp
lấp ló giảm, màu nhóm đúng ở cả hộp nguồn lẫn hộp đích, **không có `View lệch`**.

**Còn thiếu nghiệm thu:** hit-test vùng thả · ghost đuổi con trỏ · hover · feel — chỉ kéo tay mới
kiểm được (xem "Input mô phỏng" dưới). Kéo thử rồi mới xoá `PrototypeView.cs` được.

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
1. ~~User duyệt thiết kế~~ — xong 2026-08-03.
2. ~~Viết script view~~ — xong: `Views/{ViewText,TileView,BoxView,StackView,GhostView,HudView}.cs`
   + `BoardController.cs`. Ba chỗ bắt buộc mang nguyên (co chữ `CharW=0.085` không auto-wrap ·
   invariant check · **hai** hộp đổi màu mỗi nước) đều đã ở trong code.
3. ~~Dựng prefab + scene~~ — xong, chạy qua MCP bridge, đã commit.
4. **User kéo thử 3 level** *(đang chờ)*, rồi tôi xoá `PrototypeView.cs`.

Ba chỗ lệch so với draft, đã ghi ở **Mục 8** của doc: thẻ thật bay (xoá hẳn object "fly" tạm) ·
root Tile giữ scale 1 · gộp bước 2+4 (viết thẳng bản Instantiate, không làm sườn vứt đi).

## Đã làm, đã kiểm chứng

| File | Nội dung |
|------|----------|
| `demo/wordstack-clear-demo.html` + `demo/check.mjs` | Bản tham chiếu hành vi + bộ check (`node demo/check.mjs`) |
| `Assets/Prototype/PrototypeDomain.cs` | Luật + validate + beam solver + SelfCheck. **Không import UnityEngine** |
| `Assets/Prototype/PrototypeView.cs` | View runtime cũ (vẽ bằng code). **Chờ xoá** sau khi bàn prefab nghiệm thu xong; tới lúc đó vẫn Play được để đối chiếu |
| `Assets/Prototype/BoardController.cs` + `Views/*.cs` | View mới: retained-mode, dựng từ prefab. Compile sạch, **chưa Play được vì chưa có prefab** |
| `Assets/Prototype/Editor/LevelEditorWindow.cs` | Tool xếp level (`WordStack ▸ Level Editor`) |
| `Assets/Prototype/Editor/PrefabBuilder.cs` | `WordStack ▸ Build Prefabs + Scene` — dựng 5 prefab + Main.unity. **Chạy lại ghi đè prefab**, mất chỉnh tay |
| `Assets/Prototype/Resources/Levels/lv-00{1,2,3}.json` | 3 level, solver khớp demo (6/6/9/9/2/2 nước) |
| `Assets/Prototype/Resources/Art/*.png` | 12 placeholder **có nhãn**; meta đã commit (GUID ổn định) |
| `Assets/Plugins/Demigiant/` | DOTween 843K, commit vào repo theo quyết định của user |
| `docs/architecture/view-prefabs.md` | Thiết kế prefab — **Approved**; Mục 6 = checklist dựng tay, Mục 8 = chỗ lệch draft |

## Hai lệnh kiểm, chạy trước khi mở Unity

```bash
./selfcheck.sh      # luật + validate + solver, ~3s, không cần Unity
./compilecheck.sh   # compile cả 2 assembly C#, vài giây
```

`compilecheck.sh` gom file bằng `find` (thêm .cs khỏi phải sửa script) và mượn
`Unity.InputSystem.dll` của repo chính khi worktree chưa có `Library/`. Assembly editor giờ gom cả
source runtime vì `PrefabBuilder` đụng tới view — đã thử cách đúng-Unity hơn (tham chiếu `game.dll`)
nhưng dính CS0012: game.dll theo mscorlib, ref set của UnityEditor theo netstandard. Nó vẫn phải
**tách 2 assembly** đúng như Unity (`Assembly-CSharp` vs `-Editor`) vì hai
thế giới reference xung khắc: `DOTween.dll` build theo mscorlib, `UnityEditor.dll` theo netstandard
— nối bằng `Facades/netstandard.dll`.

## Chặn / cần quyết định

- **User kéo thử bàn mới** (Play `Assets/Scenes/Main.unity`) — kéo-thả/cascade/Won/Stuck chưa ai
  chạy qua. Xong mới xoá được `PrototypeView.cs`.
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

- **MCP Unity bridge** (`com.unity.ai.assistant`): đăng ký theo **project path**. Unity thực tế đang
  mở ở worktree `gdd-wordstack-logic-plan-5821c0` — KHÔNG phải repo chính, cũng không phải worktree
  đang code. Cách làm việc: commit ở worktree mình → `git merge --ff-only` ở worktree kia (hai
  branch cùng dòng nên luôn fast-forward) → `AssetDatabase.Refresh()` qua bridge → gọi hàm.
  Bridge **rớt mỗi domain reload**, gọi lại sau vài giây là được. Nó **chặn `System.Reflection`**
  (nên `PrefabBuilder.BuildAll` phải `public`) và treo nếu code gọi hộp thoại modal.
  Package trong `manifest.json` cố ý chưa commit (tooling, không phải dependency).

- **Ảnh game thật:** render `Camera.main` ra `RenderTexture` → `ReadPixels` → `EncodeToPNG` vào
  `Temp/board.png` rồi đọc file. `Unity_Camera_Capture` của MCP vẫn trả Scene View trắng.

- **Input mô phỏng KHÔNG chạy được từ tự động hoá** (đã thử hết, đừng thử lại): `InputSystem.
  QueueStateEvent` lên `Mouse` bị Editor nuốt khi Game View không có focus — kể cả khi đã đặt
  `backgroundBehavior=IgnoreFocus`, `editorInputBehaviorInPlayMode=AllDeviceInputAlwaysGoesToGameView`,
  `canRunInBackground=true`. `Pointer.current` đứng im ở toạ độ chuột thật. Mà terminal chạy lệnh
  luôn giữ foreground nên Game View không bao giờ focus được. Thay bằng
  `BoardController.DebugMove` (editor-only) + `Editor/BoardTestDriver.cs`, vào đúng chỗ `Drop()`
  đi tiếp qua `AfterMove()`. Còn hit-test/ghost/hover/feel thì phải người kéo.

- **Player loop đứng im khi Unity ở background** — `Time.frameCount` không tăng. Bật
  `Application.runInBackground = true` lúc Play trước khi làm bất cứ thứ gì cần frame chạy.

- Lỗi `Burst compilation ... BurstCache/JIT/*.dll, error code 4551` trong Console: của Burst cache
  trong worktree, không liên quan script mình, không chặn compile.

- **Branch dùng chung với phiên Claude khác** — `git fetch` trước khi làm việc dài, không phải lúc
  sắp push. Đã một lần trùng việc vì bỏ qua bước này.

- File `.md` bị git đổi CRLF — normalize LF trước khi sửa bằng công cụ khớp chuỗi.

- Câu hỏi mở Q1-Q14: `docs/wordstack-design-log.md` Mục 7. `Rules.RemoveEmptyNonBottomBox=true`
  là lựa chọn tự do (3 level chạy được cả hai chế độ) — cân nhắc lại nếu làm Undo.
