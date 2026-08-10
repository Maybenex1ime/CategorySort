# Session State

> Cập nhật cuối: 2026-08-10. File này là điểm bàn giao giữa các phiên — đọc trước khi làm gì.

## Đang ở đâu

**Stack module meta đã vào repo và compile sạch** (2026-08-10): user copy `Assets/_Modules/`
(CheatPanel · Economy Currency/Hearts/Purchase · Inventory · Progression, namespace `LogosMeta.*`)
từ project **aquapark** — bản copy giống hệt nguồn nhưng thiếu `_Modules.meta` (đã bổ sung, GUID
giữ nguyên từ aquapark). Phiên này mang nốt phần chúng phụ thuộc, cũng từ aquapark (cùng Unity
6000.3.8f1): `Assets/_StudioSDK/` Core+Save (bỏ Core/Tests + 2 folder Demo), **Reflex 14.3.0**
embed vào `Packages/`, **R3 1.3.0** + 4 DLL NuGet đi kèm vào `Assets/Packages/`, manifest thêm
`com.unity.addressables 2.3.1` + `com.unity.nuget.newtonsoft-json 3.2.1` (addressables còn sửa
luôn `addressable-importer` embed từ trước bị thiếu dep ngầm). Kiểm: một pass csc riêng
(ref set NetStandard 2.1 shims đúng kiểu Unity Bee) compile cả SDK + 4 module → OK;
`selfcheck.sh` + `compilecheck.sh` vẫn pass. **Chưa làm:** wire runtime (chưa có installer/bootstrap
nào trong `Main.unity` — muốn dùng service phải dựng Reflex scope + `SaveManager` binding);
`compilecheck.sh` chưa gom các assembly mới; Unity cần mở 1 lần (có mạng) để resolve 2 package
registry mới. `Assets/Editor Default Resources/` là đồ user đang làm dở, chưa track.

**COLLAPSE đã land trong domain** (2026-08-06): nhóm có cha (`"group"` trong JSON) gộp đủ 4 thì
sinh 1 thẻ mang mặt nhóm đó, thuộc nhóm cha, chiếm ô trống đầu tiên của CHÍNH hộp vừa gộp — hộp
không bị xoá, hộp dưới không lộ. Chế độ chặt học thêm: hộp rỗng có `HadCollapse` vẫn lùi ra.
`Solver.Encode` đánh dấu `!` cho hộp đã collapse. Spec: `docs/superpowers/specs/2026-08-06-collapse-design.md`,
plan: `docs/superpowers/plans/2026-08-06-collapse.md`. Level mới: lv-004 (không collapse),
lv-005 (1 tầng), lv-006 (2 tầng — nhóm `dog` có art riêng) — **cả 6 level qua solver cả hai chế độ**
(bố cục lv-005/006 phải sắp lại so với demo: bản demo dựa vào luật rộng, không giải được ở chặt).
**Chưa nghiệm thu tay**: animation collapse (`SpawnCollapsedTile` — 4 thẻ co về 0, thẻ mới nở ra).
`.meta` của 3 level mới viết tay (Unity mất focus không import) — GUID đã ghim, Unity mở lại sẽ nhận.

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

Không đổi. Domain thuần + SelfCheck + solver vẫn nguyên.

- **`docs/wordstack-rules.md`** — bản tóm tắt **tự đủ nghĩa**, mô tả luật *đang chạy thật* (đưa
  cho model/người mới đọc một file này là đủ). Viết 2026-08-04.
- `design/gdd/game-concept.md` mục "Luật chơi cốt lõi" — thiết kế gốc. **Lệch một chỗ**: điều kiện
  xoá hộp, xem dưới.
- `demo/wordstack-clear-demo.html` — hành vi tham chiếu.

**Q2 đã chốt 2026-08-04: giữ luật RỘNG** (`Rules.RemoveEmptyNonBottomBox = true`) — hộp trên cùng
không-đáy rỗng vì *bất kỳ* lý do gì (kể cả người chơi kéo hết thẻ ra) cũng lùi ra. GDD đọc chặt thì
chỉ CLEAR mới xoá hộp; chọn rộng vì không có Undo → luật chặt cho phép tự khoá chết bàn. Đảo lại là
một cờ, solver kiểm cả hai chế độ. Mở lại khi làm Undo.

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
| `Assets/Prototype/BoardController.cs` + `Views/*.cs` | View mới: retained-mode, dựng từ prefab. **Đang chạy** trong `Main.unity` |
| `Assets/Prefabs/*.prefab` + `Assets/Scenes/Main.unity` + `Assets/Prototype/Sprites/white.png` | Sinh bằng `PrefabBuilder`, đã commit kèm .meta |
| `Assets/Prototype/Editor/LevelEditorWindow.cs` | Tool xếp level (`WordStack ▸ Level Editor`) |
| `Assets/Prototype/Editor/PrefabBuilder.cs` | `WordStack ▸ Build Prefabs + Scene` — dựng 5 prefab + Main.unity. **Chạy lại ghi đè prefab**, mất chỉnh tay |
| `Assets/Prototype/Resources/Levels/lv-00{1,2,3}.json` | 3 level, solver khớp demo (6/6/9/9/2/2 nước) |
| `Assets/Prototype/Resources/Art/*.png` | 12 placeholder **có nhãn**; meta đã commit (GUID ổn định) |
| `Assets/Plugins/Demigiant/` | DOTween 843K, commit vào repo theo quyết định của user |
| `Assets/Prototype/Editor/BoardTestDriver.cs` | `WordStack ▸ Test ▸ Play 4 moves on lv-001` — chạy chuỗi nước đi qua `BoardController.DebugMove`, ghi `Temp/testdrive.{txt,png}` |
| `docs/architecture/view-prefabs.md` | Thiết kế prefab — **Approved**; Mục 6 = dựng bằng menu, Mục 8 = chỗ lệch draft, Mục 9 = feel lấy từ Balatro-Feel |
| `docs/wordstack-rules.md` | Luật chơi tự đủ nghĩa — đưa cho model/người mới đọc |

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

- **MCP Unity bridge** (`com.unity.ai.assistant`): mỗi Editor ghi một file đăng ký ở
  `~/.unity/mcp/connections/bridge-*.json` (có `project_path` + named pipe). Client chọn instance
  có `project_path` **là tiền tố của thư mục làm việc** — worktree nằm *dưới* `D:\CategorySort` nên
  **Unity mở ở repo chính luôn thắng**, và nó giữ pipe sẵn nên xoá file đăng ký cũng vô ích:
  **phải đóng hẳn instance repo chính** thì bridge mới chuyển sang worktree. Mất khá lâu mới ra.
  Unity cần mở ở worktree `gdd-wordstack-logic-plan-5821c0` — repo chính đang ở commit cũ, không có
  `Assets/Prefabs/` lẫn `Main.unity`. Mở bằng
  `Start-Process "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -ArgumentList @("-projectPath", "<worktree>")`.
  Cách làm việc: commit ở worktree mình → `git merge --ff-only` ở worktree kia (hai
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

- **Git — hai worktree, một dòng commit.** `claude/tiep-tuc-cong-viec-b27c80` (chỗ code) và
  `claude/gdd-wordstack-logic-plan-5821c0` (chỗ Unity mở) luôn trỏ **cùng commit**: commit bên nào
  thì `git merge --ff-only` bên kia. `main` ở `04ef5e6`, **không fast-forward được** (nó có riêng
  commit ignore `Assets/_Recovery`; điểm rẽ `0ec41f2`).

- **PR #1** — <https://github.com/Maybenex1ime/CategorySort/pull/1>, "Rebuild the view as
  retained-mode prefabs", nhánh code → `main`, no conflicts, **chưa merge** (chờ user).
  Push nhánh `claude/gdd-...` từng **treo ở `git-credential-manager get`** (hộp thoại đăng nhập,
  phiên tự động không bấm được) — nội dung không mất vì cùng commit với nhánh đã đẩy.

- File `.md` bị git đổi CRLF — normalize LF trước khi sửa bằng công cụ khớp chuỗi.

- Câu hỏi mở Q1-Q14: `docs/wordstack-design-log.md` Mục 7. **Q2 đã chốt** (xem "Luật chơi hiện
  hành"); các câu còn lại giữ nguyên.
