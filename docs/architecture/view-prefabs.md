# Thiết kế View bằng Prefab — WordStack

> Status: **Draft**. Mục 1 đã chốt: **Q1 retained-mode** và **Q3 DOTween** do user quyết
> 2026-08-03 (Q1 lật ngược lựa chọn ban đầu của doc). Phần còn lại — 5 prefab, scene, phân công —
> vẫn chờ duyệt. Sau khi duyệt: tôi viết script trước, bạn dựng prefab theo checklist Mục 6,
> rồi tôi chuyển controller sang dùng prefab và xoá code dựng runtime.
> Domain (`PrototypeDomain.cs`) **không đổi một dòng** — thiết kế này chỉ đụng lớp view.
>
> **Chặn:** DOTween phải được import tay từ Asset Store trước khi viết script view (xem Q3).

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

`TileView` fields: `bg`, `art`, `label`. API: `Bind(Tile t, Sprite sprite, Color bgColor, int order)` —
tự xử lý 3 trường hợp chỉ-ảnh / chỉ-chữ / cả hai (layout y hệt `BuildTile` hiện tại).

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

`BoxView` fields: `edge`, `slotAnchors[4]`. API: `SetEdge(Color)`, slot mount points.

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

`GhostView` `[SerializeField]`: `followSpeed=25, rotAmount=70, rotSpeed=20, autoTilt=7,
tiltSpeed=12, dragScale=1.15` — khối `// Feel` kéo-thả chuyển hết vào đây, chỉnh trong Inspector.

### `Hud.prefab` — world-space HUD (script `HudView`)

```
Hud                       HudView
├─ Header    quad #6A5BA5 + TextMesh (title · cleared/total · moves)
├─ Help      TextMesh (dòng hướng dẫn dưới bàn)
├─ WinPanel  quad phủ mờ + 2 TextMesh — tắt sẵn
└─ StuckToast quad + TextMesh — tắt sẵn
```

`HudView` fields: các TextMesh + 2 panel. API: `Set(title, cleared, total, moves)`,
`ShowWin()`, `ShowStuck()`, `HideAll()`. Controller vẫn đặt vị trí/bề rộng theo bàn (như `DrawHud`).

## 3. Scene `Assets/Scenes/Main.unity`

Thay cơ chế tự bootstrap (`RuntimeInitializeOnLoadMethod` — sẽ xoá):

```
Main Camera   orthographic, bg #3A2E5F
Game          BoardController (PrototypeView đổi tên dần) — serialized fields:
              stackPrefab, boxPrefab, tilePrefab, ghostPrefab, hudPrefab,
              palette[6] (#F4B740 #5BC98C #EF7C8E #4FA8E8 #B48CE8 #E88C4F),
              flyDur=0.16, clearDur=0.26, clearStagger=0.04, cascadeGap=0.35
```

Thêm scene vào Build Settings. Layout const (BoxSize, PeekStep...) **ở lại code** — chúng là
hằng bố cục gắn với thuật toán đặt lưới, không phải thứ chỉnh bằng mắt.

## 4. Script mới / đổi

| Script | Vai trò |
|--------|---------|
| `TileView`, `BoxView`, `StackView`, `GhostView`, `HudView` | Component mỏng trên prefab: giữ tham chiếu + API bind. **Không chứa luật, không gọi domain** |
| `BoardController` (từ `PrototypeView`) | Giữ: input/zones, cascade `Settle()`, hover. Đổi: `Instantiate(prefab)` thay `new GameObject`; **bỏ `Rebuild()`**, thay bằng API tăng dần (dưới); tween qua DOTween; xoá `Quad()`, `Label()`, `BuildTile()`, `Boot()`, tạo camera |

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

**Tôi (sau khi bạn duyệt doc này):**
1. Viết 5 script view + sườn `BoardController` — compile sạch khi CHƯA có prefab.
2. Sau khi bạn dựng xong prefab: chuyển `BoardController` sang Instantiate, xoá code dựng runtime, xoá bootstrap.
3. Nghiệm thu: Play cả 3 level, SelfCheck vẫn pass (domain không đổi), hành vi khớp demo.

**Bạn (checklist Mục 6):** dựng 5 prefab + scene bằng tay trong Editor, gắn script, kéo tham chiếu.
Sai/thiếu tham chiếu nào `BoardController` sẽ log lỗi nói rõ thiếu field gì.

## 6. Checklist dựng tay (làm SAU khi tôi báo script đã xong)

1. `Assets/Prefabs/`, `Assets/Scenes/` — tạo 2 folder.
2. Mỗi prefab: dựng hierarchy đúng tên/kích thước/màu/offset như Mục 2 (sprite trắng dùng
   `Sprites/Square` có sẵn của Unity), gắn script, kéo đủ tham chiếu vào field, save prefab.
   Số nào không ghi ở Mục 2 (font size, chi tiết chữ) để mặc định — script tự set khi Bind.
3. Scene `Main.unity`: camera + `Game` object gắn `BoardController`, kéo 5 prefab + palette 6 màu.
4. File > Build Settings > Add Open Scenes.
5. Bấm Play — lỗi thiếu gì Console sẽ nói; xong thì báo tôi để tôi rà lại prefab YAML.

## 7. Ngoài phạm vi lần này

Pooling (bàn quá nhỏ để cần), TMP + font asset tiếng Việt, tách HUD sang uGUI Canvas,
Addressables. Mỗi thứ chỉ làm khi có lý do đo được, và sẽ thành ADR riêng.

*(Retained-mode từng nằm ở mục này — đã chuyển vào phạm vi, xem Q1.)*
