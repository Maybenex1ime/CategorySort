# Animation booster — Nam châm, Xáo, Undo

Ngày chốt: 2026-09-14 · Trạng thái: đã duyệt thiết kế, code đã vào `main` (`2e08c0d` + commit dời Shuffle), **chưa nghiệm thu mắt trong Unity**.

## 1. Mục tiêu

Ba booster đã chạy đúng luật từ 2026-09-03 nhưng hai cái còn là placeholder chờ giây (`MagnetAnimation` 0.6s, `UndoAnimation` 0.3s) — bấm xong bàn "nhảy" sang trạng thái mới. Bản này cho mỗi booster một chuỗi hình đọc được bằng mắt, và gom **mọi thông số nhịp/ease/vị trí của cả ba** về một asset để chỉnh trong Inspector mà không mở scene, không sửa code.

Nguyên tắc chi phối, giữ từ các spec trước:
- **Domain đi trước, view diễn sau.** `Game` đã mutate xong trước khi animation bắt đầu; animation chỉ là màn trình diễn của diff, kết thúc bằng `RebuildBoardViews()` để view khớp `g` tuyệt đối dù animation có lệch.
- **Không thêm loại tín hiệu meta.** Vẫn mượn `RaiseMoveCommitted` + `Settle()` như hai booster đang làm (khoá hai vế: `locked` chặn kéo, phase rời Playing chặn HUD).
- **Không phá vật cản** (spec blocker 4.3) — animation không được gợi ý điều ngược lại.

## 2. Quyết định đã chốt

| # | Câu hỏi | Chốt | Lý do |
|---|---|---|---|
| 1 | Undo có lùi được nước vừa gây CLEAR/COLLAPSE không? | **Không.** `SettleStep` vứt ảnh chụp ngay khi `Cleared++`. | Lùi là trả lại tiến độ đã đạt — người chơi gom đi gom lại một nhóm để "thử" miễn phí. **Đảo quyết định 1 của spec Undo 2026-08-28.** Hệ quả phụ: diff Undo chỉ còn 1 thẻ đổi ô (+ có thể 1 hộp đứng lại), animation rẻ hẳn. |
| 2 | Undo khi nước trước làm hộp rỗng lùi ra, lộ hộp dưới (không CLEAR nên vẫn undo được)? | **Hộp cũ hiện lại: trượt từ trên xuống + hiện dần**, đè lên hộp vừa lộ; thẻ trong nó nở ra; rồi thẻ vừa kéo bay ngược về ô cũ. Vị trí xuất phát chỉnh được. | Thứ tự "nắp về trước, thẻ về sau" đọc ra là tua ngược. Thẻ của hộp vừa lộ biến ngay vì bị che, không ai thấy. |
| 3 | Nam châm: 4 thẻ bay đi đâu? | **Về một điểm theo toạ độ viewport** (mặc định 0.5, 0.5 = giữa màn hình), khựng rồi nổ về 0. Không bay về hộp đích. | User chọn. Điểm cố định theo màn hình nên không phụ thuộc bố cục bàn; chỉnh được trong asset. |
| 4 | Thẻ đang chôn (không có view) diễn thế nào? | Dựng **thẻ tạm** ngay giữa hộp che, nở ra (OutBack) rồi bay như ba thẻ kia. Hộp che đứng yên. | `StackView` chỉ vẽ lớp lấp ló trừu tượng. Không "xé" hộp che — lúc Rebuild lớp lấp ló tự cập nhật. |
| 5 | Thông số sống ở đâu? | **Một ScriptableObject `SO_BoosterAnim`** (`BoosterAnimSettings`) cho cả ba booster. `BoardController` chỉ giữ tham chiếu; thiếu asset thì dùng mặc định trong class. | User yêu cầu file data. Shuffle dời theo để một chỗ. |
| 6 | Có sửa domain không? | **Chỉ** một dòng `ClearUndo()` trong `SettleStep` (quyết định 1). Mọi thứ khác ở view. | Diff Undo tính ở view bằng cách so `prev` với bàn khôi phục; mặt thẻ Nam châm giữ ở view trước khi `ApplyMagnet`. |
| 7 | Nam châm hút nhóm con (COLLAPSE) — thẻ cha hiện ra thế nào? | **Nở tại điểm gộp rồi bay về ô** domain đặt trong hộp hội tụ (chốt 2026-09-15). | Nối liền hình "4 thẻ gộp thành 1" với chỗ thẻ cha thật sự nằm. Thay bản cũ `BloomTile` (nở tại chỗ sau Rebuild — đứt mạch với điểm gộp giữa màn). |

## 3. Chuỗi hình từng booster

### 3.1 Nam châm (`MagnetAnimation`)

```
OnMagnetRequested
  gid = FindMagnetTarget()          — chốt nhóm TRƯỚC
  faces = Tile của 4 thẻ nhóm đó    — giữ mặt thẻ, vì ApplyMagnet xoá khỏi Slots
  r = ApplyMagnet(gid) · ClearUndo()
MagnetSequence
  khoá 2 vế → MagnetAnimation(r, faces) → RebuildBoardViews → Settle()
```

Mỗi pick, lệch pha `magnetStagger`:
1. Thẻ ở hộp trên: dùng view thật, tách khỏi `tiles`, reparent lên `root`. Thẻ chôn: `Instantiate(tilePrefab)` tại `boxViews[stack].position`, scale 0 → 1 trong `magnetRevealDur`.
2. Phồng `magnetPopScale` trong `magnetPopDur`.
3. `DOMove` về điểm hội tụ (`cam.ViewportToWorldPoint(magnetGatherViewport)`), `magnetFlyDur` + `magnetFlyEase`; đồng thời đổi cỡ về `magnetGatherScale` (> 1 = to dần khi vào tâm); xoay `magnetSpin` nếu ≠ 0.
4. Khựng `magnetHold`, rồi co về 0 trong `magnetBurstDur` (`magnetBurstEase`), `Destroy`.

Nhóm có cha (COLLAPSE, `r.NewTileUid != null`) — `AppendParentFlight`, cùng một Sequence:
5. Đúng lúc thẻ cuối bắt đầu nổ, một **thẻ cha tạm** nở ra **tại điểm gộp**: 0 → `magnetGatherScale` (OutBack, `magnetParentBloomDur`), xoay `mergeSpin` về 0.
6. Khựng `magnetParentHold`, rồi **bay về ô domain đã đặt nó** trong hộp `r.NewTileStack` (`magnetParentFlyDur` / `magnetParentFlyEase`), co về 1.
7. Thẻ tạm nằm dưới `root` nên đứng yên ở ô đó tới khi `RebuildBoardViews` huỷ nó và dựng thẻ thật đúng chỗ — không có khung hình ô trống.

### 3.2 Xáo (`ShuffleAnimation` — không đổi, chỉ dời số)

Pha vào: pivot tại tâm bàn xoay `shuffleTurns` vòng, mọi thẻ lớp trên bay về tâm co còn `shuffleGatherScale`; Rebuild; pha ra ngược lại về ô mới. Thẻ tự quay ngược cùng ease với pivot nên luôn thẳng.

### 3.3 Undo (`UndoAnimation(prev)`)

```
OnUndoRequested
  prev = g · g = g.ApplyUndo()
UndoSequence(prev)
  khoá 2 vế → UndoAnimation(prev) → RebuildBoardViews → Settle()
```

1. `TopPositions(prev)` và `TopPositions(g)` (uid → ô, chỉ hộp trên cùng). Thẻ có trong `g` mà ô khác `prev` = thẻ vừa kéo (`movedUid`, `movedTo`); ưu tiên thẻ có mặt ở cả hai bàn hơn thẻ mới lộ.
2. Stack nào `g` sâu hơn `prev` = hộp cũ đứng lại: huỷ view thẻ của hộp vừa lộ; `boxViews[s]` reset, `SetLock(false)`, `ShowDepth` theo `g`; alpha 0 + `localPosition = undoBoxSlideFrom` → tween về 0 / alpha 1 trong `undoBoxSlideDur` (`undoBoxSlideEase`). Rồi thẻ của hộp cũ (trừ `movedUid`) nở 0 → 1 trong `undoBoxTilePopDur`.
3. Thẻ vừa kéo: reparent vào ô đích của `g`, giữ world pos, phồng `undoPopScale`/`undoPopDur`, rồi `DOLocalMove` về 0 trong `undoFlyDur` (`undoFlyEase`) + scale về 1.

Không có ca "thẻ đã nổ hiện lại" — quyết định 1 loại nó.

## 4. Asset `SO_BoosterAnim`

`Assets/_Game/Content/SO_BoosterAnim.asset` · class `WordStack.Board.BoosterAnimSettings` (`Assets/_Game/Board/Views/BoosterAnimSettings.cs`) · gắn vào `BoardController.animSettings` trong `Main.unity`. Menu tạo: `WordStack ▸ Booster Anim Settings`.

| Nhóm | Field | Mặc định | Ý nghĩa |
|---|---|---|---|
| Nền | `backdropFadeIn` / `backdropFadeOut` | 0.15 / 0.15 | Panel `BoardController.boosterBackdrop` mờ vào/ra (cần CanvasGroup) |
| Nam châm | `magnetGatherViewport` | (0.5, 0.5) | điểm hội tụ theo viewport camera |
| | `magnetRevealDur` | 0.15 | thẻ chôn nhô lên |
| | `magnetPopScale` / `magnetPopDur` | 1.12 / 0.10 | phồng trước khi bị hút |
| | `magnetFlyDur` / `magnetFlyEase` | 0.45 / InOutCubic | bay về điểm hội tụ |
| | `magnetStagger` | 0.05 | lệch pha 4 thẻ |
| | `magnetGatherScale` | 1.6 | cỡ khi tới nơi (> 1 = to dần khi vào tâm) |
| | `magnetSpin` | 0 | độ xoay trên đường bay (0 = tắt) |
| | `magnetHold` | 0.08 | khựng trước khi nổ |
| | `magnetBurstDur` / `magnetBurstEase` | 0.20 / InBack | nổ về 0 |
| | `magnetParentBloomDur` / `magnetParentHold` | 0.25 / 0.12 | COLLAPSE: thẻ cha nở tại điểm gộp, khựng |
| | `magnetParentFlyDur` / `magnetParentFlyEase` | 0.35 / InOutCubic | thẻ cha bay về ô trong hộp |
| Xáo | `shuffleInDur` / `shuffleOutDur` | 1.9 / 0.9 (asset) | hai pha xoáy — asset chép số user đã chỉnh trong scene, mặc định class là 1.1 / 0.55 |
| | `shuffleTurns` | 3 (asset) / 2 | số vòng |
| | `shuffleGatherScale` | 0.4 | cỡ ở tâm |
| | 5 ease `shuffleSpin/MoveIn/MoveOut/ScaleIn/ScaleOut` | OutCubic / InBack / OutBack / InQuad / OutQuad | |
| Undo thẻ | `undoPopScale` / `undoPopDur` | 1.10 / 0.08 | phồng trước khi bay ngược |
| | `undoFlyDur` / `undoFlyEase` | 0.22 / OutCubic | bay về ô cũ |
| Undo hộp | `undoBoxSlideFrom` | (0, 0.6) | xuất phát so với chỗ đứng (world unit) |
| | `undoBoxSlideDur` / `undoBoxSlideEase` | 0.25 / OutCubic | trượt + hiện dần |
| | `undoBoxTilePopDur` | 0.12 | thẻ trong hộp cũ nở |

Thông số CLEAR/COLLAPSE/cascade (`flyDur`, `clearDur`, `merge*`, `cascadeGap`) **ở lại `BoardController`** — chúng là nhịp luật chơi, không phải booster.

## 5. Ràng buộc kỹ thuật

- Thẻ bay phải `SetFlying(true)` (sorting 90) và trả về khi hạ cánh; thẻ tạm thì `Destroy`.
- Mọi tween `SetLink(gameObject)` — Rebuild huỷ object giữa chừng thì tween chết theo, không ném lỗi.
- `cloneState`/`Clone()` không mang `UndoEnabled`; `ApplyUndo` tự trao lại cờ (không đổi).
- `BoxView.SetAlpha` chỉ phủ phần thân hộp (renderer bắt lúc Awake) — thẻ trong hộp không mờ theo. Vì thế thẻ hộp cũ nở **sau** khi hộp trượt xong, không nở cùng lúc.
- `compilecheck.sh` là cổng duy nhất kiểm được phần này ngoài Unity; `selfcheck.sh` phủ quyết định 1 (SelfCheck mục 7).

## 6. Nghiệm thu

Chưa có kiểm tự động cho view. Checklist tay trong plan `docs/superpowers/plans/2026-09-14-booster-anim.md` Task 3. Cổng máy: `./compilecheck.sh` 3 dll OK · `./selfcheck.sh <lv-001 + lv-010>` OK.

## 7. Ngoài phạm vi

- Tia từ trường từ nút HUD tới thẻ (M3), afterimage khi Undo (U3) — không làm.
- Băng đếm ngược "tick lên" khi Undo — số băng tự đúng sau Rebuild, không có flourish riêng.
- Animation cho nút HUD (icon rung, viền sáng) — thuộc tầng meta, không ở bàn.
- Tool web `demo/wordstack.html` không có booster nên không liên quan.
