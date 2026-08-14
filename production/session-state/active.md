# Session State

> Cập nhật cuối: 2026-08-14. File này là điểm bàn giao giữa các phiên — đọc trước khi làm gì.

## Đang ở đâu

**Port hệ meta aquapark HOÀN TẤT — `main` = `8dfb879`, đã push** (4 commit 13–14/08:
`92f771c` AppFlow FSM + BoosterModule → `473c7de` HUD + level pipeline + cheat →
`cf23c99` audio + Settings/Pause → `8dfb879` NoHeartsPopup + kinh tế tim).
**Nguồn chi tiết hiện hành: `docs/session-log/2026-08-14-17.md`** — kiến trúc (FSM 5 state,
level pipeline, kinh tế tim 3 điểm trừ, popup, audio, booster, DI layout), 8 gotcha, backlog 10 mục.
Các đoạn dưới của mục này là lịch sử đến 2026-08-10 — riêng đoạn "KHÔNG bê sang: AppFlow/UIManager/
màn hình" đã LỖI THỜI (đã bê đủ trong 4 commit trên).

**Dọn sau port (2026-08-14, phiên đọc-log):** xoá nút Force Lose khỏi `GameplayHudView` (dead code,
chưa từng gắn vào GameObject nào — cheat panel ép Win/Lose thay); sửa 2 gate lệch sau port:
`PrototypeSelfCheckMain` trỏ art về `Assets/Prototype/Resources/Art` (runtime vẫn
`Resources.Load("Art/…")` dù level đã dời sang `_Game/Content/Levels`), `compilecheck.sh` target
meta gom thêm `Assets/BoosterModule` + ref `Unity.Addressables.Editor` (LevelCatalogBuilder cần).
`./selfcheck.sh` + `./compilecheck.sh` (game/editor/meta) đều PASS trở lại.

**Luồng mua booster khi hết lượt (2026-08-14, cùng phiên):** click nút booster count = 0 → view bắn
`PurchaseRequestedEvent` (4 view đã bắn sẵn từ trước) → **`BoosterPurchaseFlow`** (mới,
`Currency/UI/Impl/`, đăng ký **Eager** trong AppFlowInstaller — không ai inject, việc nằm ở ctor
đăng ký bus) → mở **`BoosterPurchasePopup`** (port aquapark giữ GUID: prefab + Args + 6 sprite
`Art/UI/revive/` + font baloo Blue Border). Chưa có hệ mua: Price = 0 hiện "—", nút Mua **log stub**
không trừ coin/cộng booster; giao dịch ngoài booster (heart) log warn bỏ qua. Count > 0: armable
lên nòng như cũ; instant giờ **chỉ log** "logic chưa chốt, chưa trừ lượt" (không RequestUse — người
chơi khỏi mất booster cho hiệu ứng rỗng). Fix kèm: `BoosterManager` KeyNotFoundException khi
BoosterUseEvent bắn id chưa có trong inventory. Utility mới **WordStack ▸ Build UI Addressables**
(quét `_Shared/Prefab/Popup|Screen`, address = tên file — đóng footgun address≠class).
**CÒN 1 BƯỚC EDITOR:** focus Unity cho compile rồi chạy menu đó để đăng ký address
`BoosterPurchasePopup` (bridge chết cả phiên: pipe stale PID 25724 đã xoá, instance sống không
trả lời — nghi mất focus/modal).

**Stack module meta đã vào repo và compile sạch** (2026-08-10): user copy `Assets/_Modules/`
(CheatPanel · Economy Currency/Hearts/Purchase · Inventory · Progression, namespace `LogosMeta.*`)
từ project **aquapark** — bản copy giống hệt nguồn nhưng thiếu `_Modules.meta` (đã bổ sung, GUID
giữ nguyên từ aquapark). Phiên này mang nốt phần chúng phụ thuộc, cũng từ aquapark (cùng Unity
6000.3.8f1): `Assets/_StudioSDK/` Core+Save (bỏ Core/Tests + 2 folder Demo), **Reflex 14.3.0**
embed vào `Packages/`, **R3 1.3.0** + 4 DLL NuGet đi kèm vào `Assets/Packages/`, manifest thêm
`com.unity.addressables 2.3.1` + `com.unity.nuget.newtonsoft-json 3.2.1` (addressables còn sửa
luôn `addressable-importer` embed từ trước bị thiếu dep ngầm). Kiểm: một pass csc riêng
(ref set NetStandard 2.1 shims đúng kiểu Unity Bee) compile cả SDK + 4 module → OK;
`selfcheck.sh` + `compilecheck.sh` vẫn pass. **Đã wire runtime xong** (cùng ngày): `Assets/Resources/ReflexSettings.asset`
trỏ tới `Assets/Prefabs/ProjectScope.prefab` (ContainerScope + SaveSystemInstaller +
GameSaveInstaller + CurrencyInstaller + HeartInstaller); `Main.unity` thêm object `SceneScope`
(ContainerScope + MetaSaveTrigger). Code meta ở `Assets/_Game/`, asmdef **riêng**
`WordStack.Meta`(+`.Editor`) nên Assembly-CSharp không đổi. Nghiệm thu bằng menu
**WordStack ▸ Test ▸ Meta save round-trip** — dựng container từ chính prefab thật, +37 coin,
ghi đĩa, dựng container mới đọc lại: đã chạy PASS (`currency.json` đúng số, tự trả về giá trị cũ).
`compilecheck.sh` nay có target thứ ba `meta` (netstandard2.1, tách khỏi 2 target cũ vì R3/Reflex
xung khắc ref set mscorlib) — cả 3 OK. **Hai bẫy đã ghi trong code, đừng lặp lại:** (1) Reflex
KHÔNG instantiate prefab root scope, nên MonoBehaviour đặt trên đó không có callback vòng đời —
aquapark để `OnApplicationQuit → SaveAll` trong `GameSaveInstaller` là **bug ngầm**, ở đây trigger
nằm ở `MetaSaveTrigger` trong scene; (2) `ISaveManager.Save<T>()` chỉ đánh dấu dirty, phải
`SaveAll()` mới ghi file (HeartService dùng `SaveImmediate` nên tự lo, CurrencyService thì không).
Service bind `Resolution.Lazy` — Eager sẽ chạy trước khi domain đăng ký và mất hết dữ liệu.
**Luồng meta đã nối vào gameplay** (bê từ aquapark, cùng ngày): gameplay báo qua
`LevelSignals` (assembly `WordStack.Contracts`, **không phụ thuộc gì**) → `MetaSession` (scene)
trừ tim, gọi `ProgressionService.ReportResult`, và **chuyển tiếp lên `Bus.Global`** → `CoinRewardService`
nghe bus mà cộng coin — đúng hình dạng aquapark (service nghe bus, không ai gọi thẳng).
`BoardController` chỉ thêm 2 dòng: `RaiseStarted` trong `Load()`, `RaiseFinished` trong `RefreshHud()`
có cờ `resultReported` (RefreshHud chạy nhiều lần/màn, thiếu cờ là cộng coin mỗi khung hình).
**Vì sao không cho gameplay dùng thẳng `Bus.Global`:** `Core.EventBus` dùng `ValueTask`, kiểu không
có trong ref set `4.7.1-api` mà target `game`/`editor` của compilecheck dùng → gameplay sẽ compile
được trong Unity nhưng gãy cổng kiểm. Contracts là assembly đệm để tránh đúng chuyện đó.
**Lệch aquapark một điểm CÓ CHỦ Ý:** hết tim aquapark chặn vào màn + bật popup; WordStack chưa có
popup nên chỉ ghi log, vẫn cho chơi (chặn mà không giải thích thì thành game đứng im).
**KHÔNG bê sang:** AppFlow 8 state + 11 trigger, UIManager/NavigationService, màn hình
MainMenu/LevelSelect/Result — chúng cần `LogosSDK.UI` (chưa copy) + scene Addressables + art,
mà WordStack chỉ có 1 scene và chưa có UI framework.
**Đã gắn asset và NGHIỆM THU trong Unity thật** (bridge nối lại): `ProjectScope` mang 8 installer
(Save · GameSave · Currency · Heart · Progression · UIInstaller · UIAnimationInstaller), `SceneScope`
mang ContainerScope + MetaSaveTrigger + MetaSession. Hai menu đều PASS:
`FLOW CHECK OK — coin 0 → 50 (+50) · màn 0 → 1 · tim 4 → 3` và
`META CHECK OK — coin 0 → 37 qua đĩa`.

**Tầng UI của SDK đã bê sang** (`Assets/_StudioSDK/UI/`, 29 file — Core `UIManager`/`NavigationService`,
Base `ScreenBase`/`PopupBaseTArgs`, Animation, Transitions, Components, Installers; bỏ Tests + Editor rỗng).
Khai báo `DOTweenPro.Scripts` trong asmdef đã **xoá** — đo thật: 30 file không dùng một type DOTweenPro
nào, chỉ `DG.Tweening` lõi, nên DOTween free là đủ. Mọi ref còn lại project đã có sẵn. `compilecheck.sh`
target `meta` phải thêm `Unity.InputSystem` và bộ define `UNITY_2018_1_OR_NEWER;NET_STANDARD_2_0`
(DOTweenModuleUnityVersion giấu `AsyncWaitForCompletion` sau đúng hai cờ đó, LogosSDK.UI await nó).

**Còn lại để dùng được UI** (việc trong Editor, chưa làm): [3] tạo Addressables Settings —
`UIManager` nạp UI bằng `Addressables.InstantiateAsync(type.Name, root)` nên **mỗi popup phải là
prefab Addressable có address ĐÚNG BẰNG tên class**, sai là lỗi runtime chứ không phải lỗi compile;
[4] dựng Canvas + EventSystem + `UIManager` trong `Main.unity` rồi trỏ `UIInstaller._uiManager` vào nó
(hiện đang null nên UIInstaller tự thoát, vô hại); [6] dựng prefab popup đầu tiên.
`UIAnimationInstaller` cần `UIAnimationSettingsSO` nhưng null thì tự thoát, để trống được.
Screens/Popups của aquapark (`_Game/UI/`) **không copy được** — chúng tham chiếu BoosterModule,
LevelCatalog, các lớp `*Args` riêng; phải viết lại trên `ScreenBase`/`PopupBaseTArgs`.

**Chưa làm:** Inventory/Purchase/CheatPanel chưa bind (`LevelService` mới là thứ cần `ILevelCatalog`
và giẫm lên hệ level JSON — `ProgressionService` thì tự đủ, đã bind); `ProjectInstaller` của SDK cố tình KHÔNG gắn
(nó bind EventBus + AddressableAssetService, WordStack chưa dùng). `Assets/Editor Default Resources/` là đồ user đang làm dở, chưa track.

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
