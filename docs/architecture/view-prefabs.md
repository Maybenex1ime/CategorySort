# Thiết kế View bằng Prefab — WordStack

> Status: **Draft — chờ duyệt**. Sau khi duyệt: tôi viết script trước, bạn dựng prefab theo
> checklist Mục 6, rồi tôi chuyển controller sang dùng prefab và xoá code dựng runtime.
> Domain (`PrototypeDomain.cs`) **không đổi một dòng** — thiết kế này chỉ đụng lớp view.

## 1. Hai quyết định nền (đã chọn, kèm lý do — phản đối thì nói trước khi tôi code)

**Q1 — Giữ nhịp "rebuild toàn bàn sau mỗi nước" hay chuyển retained-mode (instance sống suốt level)?**
→ **Giữ rebuild, thay `new GameObject` bằng `Instantiate(prefab)`.** Toàn bộ logic đang chạy tốt
(zones, cascade, fly-anim) giữ nguyên; bàn chơi < 100 object nên rebuild không thành vấn đề hiệu năng
kể cả mobile. Retained-mode là refactor lớn, chỉ đáng làm nếu profiler kêu — để dành cho production polish.
<!-- ponytail: rebuild O(bàn) mỗi nước; chuyển retained-mode nếu profiler kêu -->

**Q2 — Chữ dùng TextMesh (như hiện tại) hay chuyển TextMeshPro?**
→ **Giữ TextMesh + font hệ điều hành.** TMP cần font asset tự tạo, mà font asset mặc định
(LiberationSans) **thiếu glyph tiếng Việt** (ạ ế ộ...) — đổi TMP là mở ra một việc mới (generate font asset)
không phục vụ gì cho mục tiêu prefab. TMP thành ADR riêng khi vào production polish.

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
| `BoardController` (từ `PrototypeView`) | Giữ nguyên: input/zones, cascade `Settle()`, fly/shake/hover. Đổi: mọi chỗ dựng object → `Instantiate(prefab)`; xoá `Quad()`, `Label()`, `BuildTile()`, `Boot()`, tạo camera |

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
retained-mode instances, Addressables. Mỗi thứ chỉ làm khi có lý do đo được, và sẽ thành ADR riêng.
