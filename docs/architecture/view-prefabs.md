# Thiết kế View bằng Prefab — WordStack

> Status: **Approved** (user duyệt 2026-08-03). Q1 retained-mode · Q2 TextMesh · Q3 DOTween.
> Script view **đã viết xong** (compile sạch, chưa có prefab nào): `Assets/Prototype/Views/*.cs`
> + `Assets/Prototype/BoardController.cs`. Việc kế tiếp là **bạn dựng 5 prefab + scene theo
> checklist Mục 6**; xong thì tôi Play nghiệm thu và xoá `PrototypeView.cs`.
> Domain (`PrototypeDomain.cs`) **không đổi một dòng** — thiết kế này chỉ đụng lớp view.
>
> Ba chỗ lệch so với bản draft, quyết lúc viết code (Mục 8).

## 1. Ba quyết định nền

**Q1 — Giữ nhịp "rebuild toàn bàn sau mỗi nước" hay chuyển retained-mode (instance sống suốt level)?**
→ **Retained-mode.** *(User chốt 2026-08-03, lật lại lựa chọn ban đầu của doc này.)*

Vòng đời object:

| Sự kiện | Việc |
|---|---|
| Load level | Tạo thẻ của **hộp trên cùng** mỗi stack. Thẻ trong hộp ẩn chưa tạo |
| Nước đi hợp lệ | Tween 1 thẻ sang vị trí slot mới. Không tạo, không xoá |
| CLEAR | 4 thẻ co về 0 → Destroy |
| Lộ hộp dưới | Đổi visual hộp, **tạo thẻ của hộp vừa lộ**, giảm 1 lớp lấp ló |

Lý do đổi **không phải hiệu năng** (bàn < 100 object, rebuild vẫn thừa sức) mà là **animation xuyên
trạng thái**: rebuild làm đứt danh tính GameObject nên không tween được thẻ từ slot cũ sang slot mới,
và sau này COLLAPSE (§R4) cần 4 thẻ *bay về tâm hộp* trước khi hoá topic tile — chuyển động đó bắt buộc
instance phải sống xuyên qua thời điểm state đổi. `Tile.Uid` trong domain chính là khoá để giữ instance.

Hai hệ quả phải nhớ khi code:

- **Mỗi nước đi làm HAI hộp đổi màu**, không chỉ hộp đích: hộp nguồn mất thẻ → cặp có thể tan → thẻ còn
  lại về nền trắng. Rebuild cho không thứ này; retained-mode phải gọi lại `BoxColorIndices` cho cả hai hộp.
- Domain để lại **ô trống tại chỗ** khi rút thẻ (`Slots[i] = null`, không dồn), nên các thẻ còn lại
  trong hộp **không phải dịch chuyển**.

Cái mất so với rebuild: view có sổ sách riêng, và sổ sách lệch là họ bug khó nhất (thẻ ma sau CLEAR,
thẻ mới không hiện, màu kẹt giá trị cũ). `SelfCheck` chỉ kiểm domain, demo HTML lại render kiểu rebuild
→ **không có cách tự động nào bắt được view lệch**. Bù bằng invariant check ở Mục 4a.

**Q2 — Chữ dùng TextMesh (như hiện tại) hay chuyển TextMeshPro?**
→ **Giữ TextMesh + font hệ điều hành.** TMP cần font asset tự tạo, mà font asset mặc định
(LiberationSans) **thiếu glyph tiếng Việt** (ạ ế ộ...) — đổi TMP là mở ra một việc mới (generate font asset)
không phục vụ gì cho mục tiêu prefab. TMP thành ADR riêng khi vào production polish.

Logic co chữ trong `Label()` hiện tại (commit `e9565ce`) **mang nguyên sang `TileView`/`HudView`**:
không auto-wrap (ngắt ở mọi khoảng trắng biến câu HUD thành cột dọc), và hằng `CharW = 0.085` là số
**đo thật** bằng `Renderer.bounds` trong Play mode — không phải số đoán, đừng viết lại.

**Q3 — Tween: tự viết lerp hay dùng thư viện?**
→ **DOTween.** *(User chốt 2026-08-03.)*

Lý do: retained-mode làm nhu cầu tween tăng (mỗi nước là một thẻ trượt), và COLLAPSE sau này cần chuỗi
tween nối tiếp có delay + callback (4 thẻ bay về tâm → flash → topic tile scale 0→1) — viết tay chuỗi đó
bằng coroutine thì lộn xộn, `DOTween.Sequence()` là đúng bài. Thêm ngay từ bước prefab để khỏi chuyển đổi lần hai.

Cài đặt: **thủ công qua Asset Store** — DOTween không có trên OpenUPM hay bất kỳ UPM registry công khai
nào vì license cấm redistribute. Thư mục asset (~2MB) **commit vào repo** để build tái lập được;
re-import từ Asset Store là thao tác tay, không tự động hoá được.

Chỗ **không** dùng DOTween: ghost đuổi con trỏ. Đó là bám theo một đích *đang di chuyển* mỗi frame,
không phải tween có điểm đến cố định — giữ `Vector3.Lerp` trong `Update()`.

## 2. Bộ prefab — 5 cái, đặt tại `Assets/Prefabs/`

Nguyên tắc: prefab = **hình thù + tham chiếu**; mọi con số feel thành `[SerializeField]` chỉnh được
trong Inspector. Tile/Box author ở kích thước thật (đơn vị world) để nhìn trong prefab mode là đúng tỷ lệ.

### `Tile.prefab` — 1 thẻ (script `TileView`)

```
Tile                      TileView; root — code scale về SlotSize khi mount vào slot
├─ Bg        SpriteRenderer (sprite trắng 1×1, scale 1×1, màu #DEDEDE, order 3)
├─ Art       SpriteRenderer (order 4) — TileView tự scale sprite cho vừa
└─ Label     TextMesh (order 4, MiddleCenter) — TileView tự co chữ theo bề rộng
```

`TileView` fields: `bg`, `art`, `label`. API: `Bind(Tile, Sprite, Color bgColor, int order, float slotSize)`
— tự xử lý 3 trường hợp chỉ-ảnh / chỉ-chữ / cả hai (layout y hệt `BuildTile` cũ) — `SetColor`, `SetOrder`.
Root **giữ scale 1**, `Bind` co phần con về `slotSize` (xem Mục 8). Chữ để màu #111111 trong prefab.

### `Box.prefab` — hộp trên cùng (script `BoxView`)

```
Box                       BoxView
├─ Edge      SpriteRenderer (1.76×1.76, #A5A5A5, order 0) — highlight đổi màu chính nó
├─ Bg        SpriteRenderer (1.60×1.60, #6B5CA8, order 1)
└─ Slots
   ├─ Slot0  (-0.375, +0.375)  ── mỗi Slot: quad Shadow (0.67×0.67, đen 14%, order 2)
   ├─ Slot1  (+0.375, +0.375)     + là anchor để mount Tile instance
   ├─ Slot2  (-0.375, -0.375)
   └─ Slot3  (+0.375, -0.375)
```

`BoxView` fields: `edge`, `slotAnchors[4]` (kéo 4 object `Slot0..3`). API: `Slot(i)`, `SetEdge(Color)`,
`SetAlpha(0..1)`, `ResetVisual()`. `Bg` + shadow không cần field: `SetAlpha` quét
`GetComponentsInChildren<SpriteRenderer>()` **lúc Awake** — tức trước khi thẻ được mount, nên fade hộp
không kéo theo thẻ nằm trong nó.

### `Stack.prefab` — một vị trí trên lưới (script `StackView`)

```
Stack                     StackView
├─ BoxAnchor              — Box instance mount vào đây
├─ Peek1..Peek3           — 3 lớp "hộp ẩn lấp ló" DỰNG SẴN, mặc định tắt
│    (mỗi lớp: Edge #A5A5A5 + Bg #544593, y = -0.10 × depth, order -11-d/-10-d)
└─ Overflow  TextMesh "+N" (dưới Peek3, tắt sẵn)
```

`StackView` fields: `boxAnchor`, `peekLayers[3]`, `overflow`. API: `ShowDepth(int hidden)` —
bật đúng số lớp + set "+N" khi hidden > 3. Lớp lấp ló dựng sẵn trong prefab thay vì code sinh —
đây là chỗ hand-editing sướng nhất (chỉnh offset/màu nhìn ngay).

### `Ghost.prefab` — thẻ đang kéo (script `GhostView`)

```
Ghost                     GhostView — giữ toàn bộ feel kéo-thả
└─ Tilt
   ├─ Shadow  SpriteRenderer (0.73×0.73, đen 35%, offset (0.05,-0.06), order 98)
   └─ TileAnchor            — Tile instance (order 100) mount vào đây
```

`GhostView` fields: `tilt`, `tileAnchor`, **`shadow`** (kéo chính node Shadow ở trên vào) +
`[SerializeField]` feel `followSpeed=25, rotAmount=70, rotSpeed=20, autoTilt=7, manualTilt=20,
tiltSpeed=12, dragScale=1.15, shadowLift=0.10, shadowSwing=0.40, shadowSwingAt=6,
shadowSwingSmooth=14` — khối `// Feel` kéo-thả chuyển hết vào đây, chỉnh trong Inspector.
API: `Begin(pt)`, `Follow(pt, dt)`.

**Bóng của ghost do một mình `Follow()` đặt vị trí** (2026-08-05). Trước đó `Begin()` tween thẳng
`shadow.localPosition`; từ khi có phần dạt-theo-hướng-kéo thì hai bên ghi cùng một transform sẽ đá
nhau, nên `Begin()` chuyển sang tween biến `lift`, còn `Follow()` mỗi frame dựng lại vị trí bóng từ
ba thành phần: `shadowHome` (giá trị authored trong prefab, chụp lúc `Awake`) + `lift` xuống dưới
(local, nên nghiêng cùng thẻ) + `swing` dạt theo hướng kéo (**world**, để hướng dạt không bị xoay Z
của thẻ bẻ đi, kèm `ClampMagnitude`).

Phần `swing` đo **vận tốc con trỏ** (`Δpt / dt`, world unit/giây), không mượn `moveDelta`. Bản đầu
dùng `moveDelta` cho ít code hơn nhưng hỏng hai đường: hệ số là số nhân mờ nghĩa nên đặt quá nhỏ
(0.5 → dạt ~0.1 unit, mắt không thấy), và `moveDelta` là *độ trễ* vốn tỉ lệ với `dt` nên cùng một
cú kéo, máy 144fps dạt chỉ bằng ~2/5 máy 60fps. Bản hiện tại: `shadowSwing` = dạt **tối đa tính
bằng world unit**, `shadowSwingAt` = tốc độ kéo đạt mức tối đa đó — hai số đều đọc ra nghĩa vật lý,
chỉnh mò được ngay trong Inspector.

### `Hud.prefab` — world-space HUD (script `HudView`)

```
Hud                       HudView
├─ Header    quad #6A5BA5 + TextMesh (title · cleared/total · moves)
├─ Help      TextMesh (dòng hướng dẫn dưới bàn)
├─ WinPanel  quad phủ mờ + 2 TextMesh — tắt sẵn
└─ StuckToast quad + TextMesh — tắt sẵn
```

`HudView` fields: `headerBg` (Transform của quad), `title`, `help`, `winPanel`, `winTitle`, `winHint`,
`stuckPanel`, `stuckBg`, `stuckLabel`. API: `Layout(cx, top, bottom, width)` (controller gọi 1 lần mỗi level;
kéo bề rộng `headerBg` + `stuckBg` theo bàn),
`Set(title, cleared, total, moves)`, `ShowWin()`, `ShowStuck()`, `HideAll()`.

## 3. Scene `Assets/Scenes/Main.unity`

Thay cơ chế tự bootstrap (`RuntimeInitializeOnLoadMethod` — sẽ xoá):

```
Main Camera   orthographic, bg #3A2E5F
Game          BoardController (PrototypeView đổi tên dần) — serialized fields:
              stackPrefab, boxPrefab, tilePrefab, ghostPrefab, hudPrefab,
              palette[6] (#F4B740 #5BC98C #EF7C8E #4FA8E8 #B48CE8 #E88C4F),
              flyDur=0.16, clearDur=0.26, clearStagger=0.04, cascadeGap=0.35
```

Thêm scene vào Build Settings. Layout const (BoxSize, SlotGap...) **ở lại code** — chúng là
hằng bố cục gắn với thuật toán đặt lưới, không phải thứ chỉnh bằng mắt.

Camera: **màu nền + orthographic author trong scene**, code chỉ chỉnh vị trí + `orthographicSize`
cho vừa bàn. Palette và 4 hằng nhịp đã có giá trị mặc định trong `BoardController` — chỉ kéo 5
prefab là chạy, không phải gõ lại 6 màu.

## 4. Script mới / đổi

| Script | Vai trò |
|--------|---------|
| `TileView`, `BoxView`, `StackView`, `GhostView`, `HudView` | Component mỏng trên prefab: giữ tham chiếu + API bind. **Không chứa luật, không gọi domain** |
| `BoardController` (thay `PrototypeView`) | Giữ: input/zones, cascade `Settle()`, hover. Đổi: `Instantiate(prefab)` thay `new GameObject`; **bỏ `Rebuild()`**, thay bằng API tăng dần (dưới); xoá `Quad()`, `Label()`, `BuildTile()`, `Boot()`, tạo camera, và cả `SpawnFly()` (xem Mục 8) |
| `ViewText` (static) | Font HĐH + logic co chữ dùng chung cho Tile/Stack/Hud. Không phải MonoBehaviour |

`Rebuild()` tách thành các thao tác tăng dần:

```
PlaceTile(uid, stack, slot)   tween thẻ sang vị trí slot mới
RemoveTiles(uids)             animate co về 0 rồi Destroy
RevealBox(stack)              đổi visual hộp + tạo thẻ hộp vừa lộ + ShowDepth
RefreshColors(stack)          tô lại theo BoxColorIndices — gọi cho CẢ hộp nguồn lẫn hộp đích
RefreshZones()                tính lại rect hit-test
```

`SettleStep` đã trả sẵn `DoomedUids` + `Stack` nên **domain không đổi một dòng**.

### 4a. Invariant check — bắt buộc, thay cho sự an toàn mà rebuild cho không

Chạy `#if UNITY_EDITOR` sau mỗi lần bàn đứng yên:

> tập `uid` của GameObject thẻ đang sống **phải bằng đúng** tập tile trong các hộp trên cùng của domain,
> và mỗi thẻ phải đứng đúng toạ độ slot của nó.

Lệch → `Debug.LogError` ngay tại nước đi gây ra, thay vì lộ ra sau 20 nước. ~15 dòng. Không có nó thì
view lệch domain là bug im lặng, vì không bộ test nào trong repo nhìn tới lớp view.

Sorting order giữ nguyên bảng hiện tại (peek −13…−10 · box 0-2 · tile 3-4 · hud 5-6 · fly 90 · ghost 98-101 · win 50-51).

## 5. Phân công

**Tôi:**
1. ~~Viết 5 script view + `BoardController`~~ — **xong 2026-08-03**, viết thẳng bản Instantiate
   (không có sườn trung gian, xem Mục 8). `./compilecheck.sh` + `./selfcheck.sh` đều pass.
2. Sau khi bạn dựng xong prefab: Play nghiệm thu cả 3 level, rà lại prefab YAML, xoá `PrototypeView.cs`.
3. `PrototypeView.cs` **còn nguyên và vẫn tự Play được** cho tới bước đó — có cái để đối chiếu hành vi.
   Nó tự nhường sân khi scene đã có `BoardController`, nên `Main.unity` không bị hai bàn chồng nhau.

**Bạn:** bấm menu ở Mục 6 rồi Play. Việc dựng tay đã chuyển thành `PrefabBuilder.cs` — Unity tự
serialize thì GUID/tham chiếu luôn đúng, và dựng lại được khi thiết kế đổi.

## 6. Dựng prefab — bằng menu, không dựng tay

`Assets/Prototype/Editor/PrefabBuilder.cs` chép Mục 2 + 3 thành code Editor:

**Đã chạy 2026-08-03** — 5 prefab + scene + sprite trắng đã sinh và commit. Phần dưới là cách chạy
lại khi thiết kế đổi.

1. Mở project bằng Unity, đợi compile xong.
2. Menu **WordStack ▸ Build Prefabs + Scene** → bấm "Dựng".
3. Nó tạo: `Assets/Prefabs/{Tile,Box,Stack,Ghost,Hud}.prefab` · `Assets/Scenes/Main.unity`
   (camera + `Game` đã kéo sẵn 5 prefab) · `Assets/Prototype/Sprites/white.png` (sprite trắng
   PixelsPerUnit=1, tự sinh để khỏi phụ thuộc GUID asset builtin của Unity) · thêm scene vào
   Build Settings.
4. Bấm Play.

**Chạy lại thì GHI ĐÈ 5 prefab** — hình thù chỉnh tay và số feel chỉnh trong Inspector sẽ mất.
Sau khi đã tinh chỉnh bằng mắt thì đừng chạy lại; muốn đổi hình thù thì sửa doc + `PrefabBuilder`
rồi mới chạy. Prefab sinh ra là asset bình thường, sửa tay tiếp được như mọi prefab khác.

Hai thứ Console sẽ nói khi sai, biết trước cho đỡ mất thời gian:

- `BoardController trên 'Game' thiếu tham chiếu: tilePrefab` — field chưa được kéo (không xảy ra
  nếu dựng bằng menu).
- `View lệch (settle): ...` — invariant Mục 4a bắt được sổ sách view lệch domain. Đây là bug thật
  của tôi, không phải bạn dựng sai; chụp Console gửi tôi.

## 7. Ngoài phạm vi lần này

Pooling (bàn quá nhỏ để cần), TMP + font asset tiếng Việt, tách HUD sang uGUI Canvas,
Addressables. Mỗi thứ chỉ làm khi có lý do đo được, và sẽ thành ADR riêng.

*(Retained-mode từng nằm ở mục này — đã chuyển vào phạm vi, xem Q1.)*

## 8. Lệch so với bản draft (quyết lúc viết code, 2026-08-03)

1. **Thẻ thật bay, không còn object "fly" tạm.** Draft giữ nguyên cơ chế cũ: thẻ thật ẩn đi
   (scale 0), một bản sao tạm bay từ chỗ thả tới slot rồi tự huỷ. Retained-mode làm cái đó thành
   thừa — thẻ thật đổi cha sang slot mới rồi `DOLocalMove` về 0 là xong. Xoá được `SpawnFly()`,
   list `flies`, `ClearFlies()`. Đây đúng là thứ Q1 mua về, tiêu luôn cho sớm.
2. **Root của `Tile` giữ scale 1**, `Bind()` co phần con theo `slotSize`, thay vì scale root về
   `SlotSize` như draft viết. Lý do: hover / nhấc lên / CLEAR đều tween scale — để root ở 1 thì
   `DOScale(0)` và `DOScale(1.07)` đọc thẳng, không phải nhân `SlotSize` ở mọi chỗ gọi.
3. **Bước 2 và 4 của phân công gộp làm một.** Draft định viết "sườn compile sạch" trước rồi mới
   chuyển sang `Instantiate` sau khi có prefab. Nhưng sườn ấy là code vứt đi, mà thiếu prefab thì
   cả hai bản đều không Play được — nên viết thẳng bản thật. `./compilecheck.sh` xác nhận compile
   sạch dù chưa có prefab nào.

## 9. Feel lấy từ `D:\Balatro-Feel` (CardVisual.cs)

Bản `PrototypeView` cũ đã mang sang: lerp đuổi con trỏ · xoay Z theo `movementDelta` · lắc sin/cos ·
scale pop lúc nhấc · bóng · punch khi từ chối. User chốt 2026-08-03 lấy nốt 4 thứ còn thiếu:

| Bên họ | Bên mình |
|---|---|
| `PointerEnter` → `DOPunchRotation(forward × 5)` | `Hover()` giật một cái khi con trỏ vào; `SetId(2)` + `Kill(id, true)` để rê nhanh qua nhiều thẻ không cộng dồn góc |
| `scaleEase = Ease.OutBack` | mọi tween scale hover/nhấc đổi từ `OutQuad` sang `OutBack` |
| `PointerDown` → bóng lùi ra xa | `GhostView.shadowLift = 0.10` lúc `Begin()`; ghost chết lúc thả nên không cần trả về |
| *(thêm ngoài bản gốc)* bóng dạt theo hướng + tốc độ kéo | vận tốc con trỏ (world unit/giây) → offset world space, clamp ở `shadowSwing`. Bản gốc chỉ nhấc bóng, không dạt — user yêu cầu 2026-08-05 |
| `CardTilt` phần manual (`offset × 20`) | cộng vào lắc sin/cos: `movement` (độ trễ so với con trỏ) chính là `offset` mà bản gốc đo bằng `ScreenToWorldPoint` |

Không lấy: `shakeParent` riêng (mình punch thẳng lên hộp), `Swap` punch (không có swap), curve fan
tay bài (không có tay bài). Số của họ tính bằng pixel UI nên **đừng chép thẳng** — `rotAmount` mình
70 so với 20 của họ vì đơn vị world nhỏ hơn nhiều.

Ngoài ra `compilecheck.sh` giờ gom `Assets/Prototype/**/*.cs` bằng `find` (thêm file khỏi phải
sửa script) và mượn `Unity.InputSystem.dll` của repo chính khi worktree chưa có `Library/`.
