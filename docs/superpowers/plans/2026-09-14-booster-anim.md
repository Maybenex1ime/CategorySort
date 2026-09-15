# Animation booster — kế hoạch thực thi

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ba booster có animation thật (Nam châm bay về điểm viewport rồi nổ; Undo tua ngược kể cả hộp cũ đứng lại; Xáo giữ xoáy), mọi thông số chỉnh từ một asset `SO_BoosterAnim`, và được nghiệm thu bằng mắt trong Unity trên `lv-010`.

**Architecture:** Domain mutate xong trước, view diễn diff rồi `RebuildBoardViews()` để khớp tuyệt đối. Thông số sống ở `BoosterAnimSettings` (ScriptableObject), `BoardController` đọc qua property `A` (fallback mặc định trong class khi asset chưa gán). Domain chỉ thêm một dòng: `SettleStep` vứt ảnh chụp Undo khi `Cleared++`.

**Tech Stack:** Unity 6000.3.8f1 · DOTween (core, không module Sprite) · C# view trong assembly `WordStack.Board` · cổng máy `./compilecheck.sh` + `./selfcheck.sh`.

**Spec:** `docs/superpowers/specs/2026-09-14-booster-anim-design.md`

## Global Constraints

- KHÔNG import UnityEngine trong `Assets/_Game/Board/Domain/` (selfcheck compile cả thư mục).
- Thẻ bay: `SetFlying(true)` lúc cất cánh, `SetFlying(false)` hoặc `Destroy` lúc xong; mọi tween `SetLink(gameObject)`.
- Không dùng `sr.DOFade` (module Sprite tuỳ chọn) — mờ hộp bằng `DOTween.To(BoxView.SetAlpha)`.
- Không thêm tín hiệu meta mới: booster mượn `RaiseMoveCommitted(g.Moves)` + `Settle()`.
- Thông số CLEAR/COLLAPSE/cascade ở lại `BoardController`; chỉ booster vào SO.
- Level ship đang đỏ vì content (lv-002 chặt, lv-008 thiếu ảnh) — chạy `./selfcheck.sh` với thư mục riêng gồm lv-001 + lv-010.
- Commit qua PowerShell `git -C D:\CategorySort ...` khi Bash bị chặn; message tiếng Anh mệnh lệnh, kết thúc `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`; push do user làm.
- Unity mất focus không compile — sau khi sửa `.cs` phải focus Editor, đợi `Library/ScriptAssemblies/WordStack.Board.dll` mới hơn file nguồn rồi mới Play.

---

## Vòng phản hồi

| Lệnh | Phủ gì | Đầu ra mong đợi |
|---|---|---|
| `./compilecheck.sh` | view + editor + meta compile ngoài Unity | `game.dll OK` `editor.dll OK` `meta.dll OK` |
| `./selfcheck.sh Temp/sc-lv010` (thư mục có lv-001 + lv-010) | luật domain, gồm SelfCheck mục 7 (Undo mất khi CLEAR) | `SelfCheck OK - 2 level` |
| Play `Main.unity` → CHEAT → Level 5 (lv-010) | nghiệm thu mắt | xem Task 3 |

---

### Task 1: Undo không lùi nước CLEAR + animation Nam châm/Undo + SO_BoosterAnim — **ĐÃ XONG**

Commit `72e9032` (domain + SelfCheck mục 7 + rules.md) và `2e08c0d` (view + SO + scene). Giữ lại đây để người đọc plan biết mặt cắt hiện tại.

**Files:**
- Modify: `Assets/_Game/Board/Domain/Game.cs` — `SettleStep`: `ClearUndo()` ngay sau `Cleared++`.
- Modify: `Assets/_Game/Board/Domain/GameUndo.cs` — comment `ClearUndo` ghi chỗ gọi thứ hai.
- Modify: `Assets/_Game/Board/Domain/SelfCheck.cs` — mục 7: ca ép CLEAR phải `!CanUndo`; ca nước thường lùi về đúng `Solver.Encode` trước đó.
- Create: `Assets/_Game/Board/Views/BoosterAnimSettings.cs` (+ `.meta`) — class `BoosterAnimSettings : ScriptableObject`, `[CreateAssetMenu("WordStack/Booster Anim Settings")]`.
- Create: `Assets/_Game/Content/SO_BoosterAnim.asset` (+ `.meta`).
- Modify: `Assets/_Game/Board/Views/BoardController.cs` — field `animSettings`, property `A`, `OnMagnetRequested` giữ `faces`, `MagnetAnimation(r, faces)`, `AppendParentFlight`, `OnUndoRequested` giữ `prev`, `UndoAnimation(prev)`, `TopPositions`.
- Modify: `Assets/Scenes/Main.unity` — `animSettings: {fileID: 11400000, guid: 226ee7bdb0844b2aafc095127171a1d5, type: 2}` trên BoardController; bỏ `magnetAnimDur`/`undoAnimDur`.
- Modify: `docs/wordstack-rules.md` Mục 11 — câu luật Undo.

**Interfaces (cho task sau):**
- `BoosterAnimSettings A { get; }` trong `BoardController` — luôn khác null.
- `IEnumerator MagnetAnimation(MagnetResult r, Dictionary<string, Tile> faces)`.
- `IEnumerator UndoAnimation(Game prev)` · `static Dictionary<string, SlotRef> TopPositions(Game game)`.
- `void AppendParentFlight(Sequence seq, MagnetResult r, Vector3 center, float at)` — thay `BloomTile` từ 2026-09-15.

- [x] Step 1–6: đã làm, kiểm `./selfcheck.sh Temp/sc-lv010` OK · `./compilecheck.sh` 3 OK.

---

### Task 2: Dời Shuffle sang SO_BoosterAnim — **ĐÃ XONG**

**Files:**
- Modify: `Assets/_Game/Board/Views/BoosterAnimSettings.cs` — nhóm `[Header("Xáo …")]` 9 field `shuffleInDur … shuffleScaleOutEase`.
- Modify: `Assets/_Game/Board/Views/BoardController.cs` — xoá 9 `[SerializeField]`, `Vortex`/`ShuffleAnimation` đọc `A.shuffle*`.
- Modify: `Assets/_Game/Content/SO_BoosterAnim.asset` — chép số scene đang dùng: `shuffleInDur: 1.9` · `shuffleOutDur: 0.9` · `shuffleTurns: 3` · `shuffleGatherScale: 0.4` · ease 9/26/27/5/6.
- Modify: `Assets/Scenes/Main.unity` — bỏ 9 dòng `shuffle*` trên BoardController.

- [x] Step 1: sửa 4 file như trên.
- [x] Step 2: `./compilecheck.sh` → 3 dll OK.
- [x] Step 3: commit `refactor(booster): shuffle timings move into SO_BoosterAnim as well`.

---

### Task 3: Nghiệm thu mắt trong Unity (người làm: user, có Editor)

Không có kiểm tự động cho view. Mỗi bước là một thứ phải NHÌN THẤY; thấy khác thì ghi lại vào Task 4 thay vì sửa tại chỗ.

**Files:** không sửa. Đọc: `Assets/_Game/Content/SO_BoosterAnim.asset` (Inspector).

- [ ] **Step 1: Chuẩn bị**

  1. Mở Unity ở `D:\CategorySort`, đợi compile xong (Console không đỏ; `Library/ScriptAssemblies/WordStack.Board.dll` mới hơn `BoardController.cs`).
  2. Chọn `Assets/_Game/Content/SO_BoosterAnim` — Inspector phải hiện 3 nhóm Nam châm / Xáo / Undo với đúng số trong spec Mục 4. Không hiện → script chưa compile hoặc `.meta` lệch GUID (`ea9497839f534db9b0fa4583100f2453`).
  3. Chọn `Game` trong `Main.unity` → `BoardController ▸ Booster ▸ Anim Settings` phải trỏ `SO_BoosterAnim`. Trống → kéo asset vào (code vẫn chạy bằng mặc định, nhưng chỉnh asset sẽ vô tác dụng).
  4. `WordStack ▸ Build Level Catalog` (lv-010 chưa Addressable) → Play → CHEAT → Level 5 "Blocker showcase".

- [ ] **Step 2: Nam châm — nhóm toàn ở lớp trên**

  lv-010 mọi hộp đều đơn tầng, nên nhóm `dog` (poodle · corgi · husky · chihuahua, 3 hộp hàng 0) là ứng viên đầu (`onTop` cao nhất).
  Bấm Magnet. Phải thấy, theo thứ tự: 4 thẻ dog phồng nhẹ → bay lệch pha về **giữa màn hình** → to dần tới ~1.6 → khựng → nổ về 0. Progress `1/5`. Không thẻ nào còn sót ở hộp cũ sau khi bàn đứng yên. Nút Undo **xám** (Magnet xoá ảnh chụp).
  Đổi `magnetGatherViewport` thành (0.5, 0.85) trong Inspector (lúc Play được, SO sửa runtime giữ tới khi thoát Play) → R nạp lại → Magnet: điểm hội tụ phải lên gần mép trên. Trả về (0.5, 0.5).

- [ ] **Step 3: Nam châm — có thẻ chôn**

  Cần màn có hộp nhiều tầng: CHEAT → Level 1 (lv-001, stack (0,0) và (1,1) có 2 hộp). Kéo vài nước cho nhóm nào đó có 1 thẻ chôn mà nhóm vẫn đủ 4 hút được (Magnet chỉ sáng khi có nhóm đủ điều kiện — nút sáng là đủ).
  Bấm Magnet. Thẻ chôn phải **nở ra từ giữa hộp đang che** (scale 0 → 1, OutBack) rồi mới bay; hộp che **không** biến mất trong lúc đó. Sau khi bàn đứng yên, lớp lấp ló của stack đó giảm đúng một nấc (hoặc hộp che lùi ra nếu rỗng).

- [ ] **Step 4: Nam châm — nhóm có cha (COLLAPSE)**

  lv-001: `dog/cat/bird` là con của `pet`. Khi Magnet hút một nhóm con, đúng lúc 4 thẻ nổ, **thẻ cha nở ra tại điểm gộp** (giữa màn, cỡ ~1.6, xoay về thẳng), khựng một nhịp rồi **bay về ô của nó trong hộp hội tụ**, co về cỡ thường — `AppendParentFlight`. Bàn đứng yên thì thẻ cha nằm đúng ô đó; không nháy, không có khung hình ô trống lúc Rebuild.

- [ ] **Step 5: Undo — nước thường**

  Kéo một thẻ sang hộp khác (không gom đủ 4). Bấm Undo. Phải thấy: thẻ phồng ~1.1 rồi **bay ngược về đúng ô cũ** (OutCubic, 0.22s), HUD nước đi giảm 1, nút Undo xám sau đó. Thẻ băng nào vừa giảm nấc thì số băng **tăng lại 1** sau khi bàn đứng yên (lv-010: kéo `blue` đi rồi Undo — `bengal` 🧊2 phải về 2 nếu đã tụt 1).

- [ ] **Step 6: Undo — nước gây CLEAR**

  Dựng một nhóm 3 thẻ trong một hộp + thẻ thứ 4 kề bên, kéo thẻ thứ 4 vào → CLEAR. Nút Undo phải **xám ngay** khi bàn đứng yên; bấm (nếu bấm được qua cheat) không có gì xảy ra, Console log `[Undo] chưa có nước nào để lùi`.

- [ ] **Step 7: Undo — nước làm hộp rỗng lùi ra (quyết định 2)**

  lv-001: chọn stack 2 hộp có hộp trên **còn đúng 1 thẻ** (kéo bớt trước nếu cần), kéo thẻ cuối đó ra → hộp trên rỗng lùi ra, hộp dưới lộ (không CLEAR). Nút Undo **sáng**. Bấm Undo, phải thấy theo thứ tự:
  1. Thẻ của hộp vừa lộ biến mất ngay.
  2. Hộp cũ **trượt từ trên xuống** (xuất phát cao hơn 0.6 world unit) và **hiện dần** đè lên chỗ đó; lớp lấp ló của stack tăng lại một nấc.
  3. Thẻ còn lại trong hộp cũ nở ra (ở ca này hộp cũ chỉ có đúng thẻ vừa kéo, nên bước này trống).
  4. Thẻ vừa kéo bay ngược vào ô cũ trong hộp cũ.
  Đổi `undoBoxSlideFrom` sang (−0.8, 0) → lặp lại: hộp phải trượt từ **trái** vào. Trả về (0, 0.6).

- [ ] **Step 8: Xáo — không đổi hình**

  Bấm Shuffle trên lv-010: xoáy 3 vòng, pha vào 1.9s, pha ra 0.9s — y như trước khi dời SO. Đổi `shuffleTurns` = 1 → R → Shuffle: chỉ 1 vòng. Trả về 3.

- [ ] **Step 9: Chặn input suốt animation**

  Trong lúc Magnet/Undo đang diễn: click kéo thẻ phải **không** nhấc được; nút HUD booster xám. Xong animation nút sáng lại đúng trạng thái (`RefreshBoosterAvailability`).

---

### Task 4: Tinh chỉnh thông số theo kết quả Task 3

Chỉ sửa **asset**, không sửa code, trừ khi hình sai cấu trúc (thẻ sót, sai thứ tự) — khi đó mở issue mới, không vá tại chỗ.

**Files:**
- Modify: `Assets/_Game/Content/SO_BoosterAnim.asset` (qua Inspector, KHÔNG sửa YAML tay khi Unity đang mở).

- [ ] **Step 1: Chốt số** — mỗi dòng Task 3 "thấy khác" → đối chiếu bảng dưới, chỉnh, Play lại tới khi ưng.

| Triệu chứng | Field |
|---|---|
| Nam châm nhanh quá, không đọc kịp | `magnetFlyDur` ↑ (0.6), `magnetStagger` ↑ (0.08) |
| 4 thẻ dính thành một khối lúc bay | `magnetStagger` ↑, `magnetSpin` 20–40 |
| Nổ "khan", thiếu nhịp cộp | `magnetHold` ↑ (0.12), `magnetBurstEase` InBack giữ |
| Thẻ chôn nhô lên khó thấy | `magnetRevealDur` ↑ (0.25) |
| Điểm hội tụ đè lên HUD | `magnetGatherViewport.y` 0.45 |
| Undo bay quá chậm so với kéo tay (`flyDur` 0.16) | `undoFlyDur` 0.18 |
| Hộp cũ hiện lại như teleport | `undoBoxSlideDur` ↑ (0.35), `undoBoxSlideFrom.y` ↑ (0.9) |
| Hộp cũ trượt xong thẻ nở giật | `undoBoxTilePopDur` ↑ (0.18) |

- [ ] **Step 2: Đo lại tổng thời gian** — Magnet ≈ pop + fly + hold + burst + stagger×3 (mặc định ≈ 0.98s), ngang `shuffleInDur`; Undo ≈ 0.3s (nước thường) / ≈ 0.7s (có hộp cũ). Ba booster phải cùng "trọng lượng": Undo nhanh nhất, Magnet ≈ Shuffle.

- [ ] **Step 3: Commit asset**

```powershell
git -C D:\CategorySort add Assets/_Game/Content/SO_BoosterAnim.asset
git -C D:\CategorySort commit -m "content(booster-anim): tune magnet and undo timings after the first playtest" -m "Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 5: Ghi log phiên + trạng thái bàn giao — **ĐÃ XONG** (log `2026-09-14-14.md`, Task 3 đánh dấu chưa chạy)

**Files:**
- Create: `docs/session-log/2026-09-14-<giờ>.md` — theo khuôn `2026-09-11-08.md`: trạng thái git · quyết định (6 dòng spec Mục 2) · gotcha mới (đánh số nối tiếp 31) · backlog delta · trạng thái cổng kiểm.
- Modify: `production/session-state/active.md` — đoạn "Đang ở đâu": thêm dòng trỏ spec + plan này, ghi rõ "Undo không lùi CLEAR (đảo spec 08-28)", "ba booster đọc SO_BoosterAnim", và trạng thái nghiệm thu Task 3 (đã/chưa).

- [x] **Step 1: Viết log** với ít nhất các gotcha đã gặp trong phiên này:
  - 31. Lệnh xoá đứng chung `D:\CategorySort` bị harness chặn (đã có ở log 09-11-08 đợt 2 — chỉ trỏ tới).
  - 32. `ViewText.Apply` ép `fontSize 64` + đổi font — TextMesh nào author cỡ chữ riêng trong prefab (Lock Text 170) thì chỉ gán `.text`.
  - 33. Commit thẳng trên `main` trong khi worktree có commit riêng → `--ff-only` từ chối; gộp bằng `git merge main` ở worktree rồi ff (repo cấm rebase).
  - 34. Heredoc bash chứa script Python dài bị vỡ quoting — ghi script ra file scratchpad rồi chạy.
  - 35. Tool web hiện 🔑 dưới chữ "ben" ở thẻ vừa băng vừa chìa khi không có emoji icon — một dòng CSS, chưa sửa.
- [x] **Step 2: Cập nhật `active.md`** đoạn đầu.
- [x] **Step 3: Commit**

```powershell
git -C D:\CategorySort add docs/session-log/2026-09-14-*.md production/session-state/active.md
git -C D:\CategorySort commit -m "docs(session): booster animations, SO_BoosterAnim, undo no longer replays a clear" -m "Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

## Self-review (đã chạy lúc viết)

- **Phủ spec:** Mục 2 QĐ1 → Task 1 · QĐ2 → Task 1 + Task 3 Step 7 · QĐ3/4 → Task 1 + Task 3 Step 2–4 · QĐ5 → Task 1 + Task 2 · QĐ6 → Task 1. Mục 4 bảng field → Task 3 Step 1 kiểm Inspector, Task 4 chỉnh. Mục 5 ràng buộc → Global Constraints. Mục 6 nghiệm thu → Task 3.
- **Placeholder:** không có TBD; mọi step nghiệm thu nêu đúng màn, đúng thẻ, đúng thứ phải thấy.
- **Tên nhất quán:** `A`, `MagnetAnimation(r, faces)`, `UndoAnimation(prev)`, `AppendParentFlight`, `TopPositions` khớp code đã commit; field asset khớp `BoosterAnimSettings.cs`.
