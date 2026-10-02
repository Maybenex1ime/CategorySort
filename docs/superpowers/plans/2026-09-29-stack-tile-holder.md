# Stack một hộp + Tile Holder — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stack chỉ vẽ một hộp + tối đa 5 Tile Holder; khi hộp trên cùng bị xoá, thẻ mini của holder trên cùng bay vào hộp thành thẻ thật và holder nhấc đi — mọi chuyển động là `LitMotionAnimation`.

**Architecture:** Luật (`Domain/`) chỉ đổi một chỗ: Magnet xoá hộp chôn bị hút rỗng. View vẽ holder hoàn toàn từ `(consumed, danh sách hộp)` — `consumed` là lịch sử do `BoardController` giữ theo stack (sống qua `RebuildBoardViews`). Animation do tool Editor dựng sẵn trên `Stack.prefab` / `Tile.prefab`; code chỉ gán đích bay, `Stop`/`Play` và chờ `IsPlaying`.

**Tech Stack:** Unity 6000.3.8f1 (URP 2D), C#, LitMotion 2.0.2 + LitMotion.Animation 2.0.2, NUnit (EditMode), Roslyn check ngoài Unity (`compilecheck.sh`, `.git/sdd/testcheck.sh`, `selfcheck.sh`).

**Spec:** `docs/superpowers/specs/2026-09-29-stack-tile-holder-design.md` — plan này chỉnh 3 điểm của spec (cập nhật lại spec ở Task 7):
`consumed` sống trong `BoardController` (không phải `StackView`); không có `StackView.Abort()` và không có danh sách
"hộp chôn đã xoá" trong `MagnetResult` (holder vẽ lại từ dữ liệu); tool sửa thẳng asset prefab (hoàn tác bằng git, không Undo).

## Global Constraints

- Holder và lộ mặt thẻ **không tween bằng script**: chỉ `LitMotionAnimation` do tool dựng; code chỉ gán `Destination`, `Stop()`, `Play()`, đọc `IsPlaying`.
- `Assets/_Game/Board/Domain/` **không import UnityEngine** (selfcheck compile cả thư mục bằng csc thuần).
- **Không sửa `.prefab` / `.unity` trên đĩa khi Unity đang mở chính asset đó** (Prefab Mode / scene chưa save) — Unity reimport, preview LitMotionAnimation trỏ Transform đã huỷ → `MissingReferenceException`.
- Tối đa **5 hộp chôn** mỗi stack (`StackView.HolderCount = 5`); vượt thì cảnh báo, không chặn.
- Thứ tự vẽ (sorting layer `Default`): hộp Tray Bottom 5 / Bg 6 / Shadow 7, thẻ thật Bg 10 / Art 11; Peek1 nền **4** / mini **8**; Peek2 −1 / 0; Peek3 −3 / −2; Peek4 −5 / −4; Peek5 −7 / −6; `Background_0` **−100**.
- Thời lượng mặc định: bay 0.3 s, lệch 0.05 s/thẻ, OutQuad; nhấc holder +0.25 Y trong 0.35 s, OutCubic; lộ art 0.25 s + nảy 0.1.
- Không stage file đang sửa dở của user: 3 font TMP (`Assets/_Game/Art/Fonts/*.asset`), `SO_LevelCatalog.asset`, `Default Local Group.asset`.
- Commit kết thúc bằng dòng: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Lệnh chạy từ gốc repo `D:\CategorySort` bằng Git Bash.

**Lệnh kiểm dùng chung:**
- Compile: `./compilecheck.sh` → mong đợi `game.dll OK`, `editor.dll OK`, `meta.dll OK`.
- Compile test: `bash .git/sdd/testcheck.sh` (chạy SAU compilecheck) → `board-tests.dll OK`, `meta-tests.dll OK`.
- Luật: bộ level thật đang đỏ vì content có sẵn (`lv-008` thiếu art `airplane.png`, `lv-002` không giải được ở chế độ chặt) — chạy trên bản sao `lv-009` + `lv-010`:
  ```bash
  D="$TEMP/claude/lv-ok" && rm -rf "$D" && mkdir -p "$D" && cp Assets/_Game/Content/Levels/lv-009.json Assets/_Game/Content/Levels/lv-010.json "$D/" && ./selfcheck.sh "$(cygpath -w "$D")"
  ```
  → mong đợi dòng cuối `SelfCheck OK - 2 level, ...`.
- Test EditMode chạy trong Unity: **Window ▸ General ▸ Test Runner ▸ EditMode**, lọc theo tên class.

## File Structure

| File | Trách nhiệm |
|---|---|
| `Assets/_Game/Board/Domain/GameMagnet.cs` (sửa) | Magnet xoá hộp chôn bị hút rỗng, giữ hộp đáy |
| `Assets/_Game/Board/Domain/SelfCheck.cs` (sửa) | Assert cho luật trên |
| `Assets/_Game/Board/Views/FlyToTargetAnimation.cs` (mới) | Component LitMotion: bay từ vị trí lúc Play tới `Destination` gán lúc chạy |
| `Assets/_Game/Board/Views/StackView.cs` (viết lại) | 5 holder: vẽ từ `(consumed, boxes)`, ánh xạ holder ↔ hộp, bắt đầu/kết thúc Fill/Lift, cảnh báo giới hạn |
| `Assets/_Game/Board/Views/HorizontalSpriteLayout.cs` (sửa comment) | Không đổi hành vi |
| `Assets/_Game/Board/Views/TileView.cs` (sửa) | `revealAnim`, `PlayReveal()`, `IsRevealing` |
| `Assets/_Game/Board/Views/BoxView.cs` (sửa) | Bỏ `LiftAway` |
| `Assets/_Game/Board/Views/BoardController.cs` (sửa) | `holderConsumed[]`, `RevealFromHolder`, Undo, thẻ Magnet từ thẻ mini, cảnh báo nạp level |
| `Assets/_Game/Board/Editor/AnimationBuildKit.cs` (mới) | Hàm ghi `LitMotionAnimation` bằng `SerializedObject` dùng chung |
| `Assets/_Game/Board/Editor/CountLockAnimationBuilder.cs` (sửa) | Dùng `AnimationBuildKit` thay bản riêng |
| `Assets/_Game/Board/Editor/StackHolderAnimationBuilder.cs` (mới) | Tool `Tools ▸ WordStack ▸ Build Stack Holder Animations` |
| `Assets/_Game/Board/Tests/FlyToTargetAnimationTests.cs` (mới) | Test component bay |
| `Assets/_Game/Board/Tests/StackViewHolderTests.cs` (mới) | Test ánh xạ holder, thẻ mini, cảnh báo giới hạn |
| `Assets/_Game/Board/Tests/StackHolderAnimationBuilderTests.cs` (mới) | Test tool trên bản mở tạm của prefab |
| `Assets/Scenes/Main.unity` (sửa) | `Background_0` order −100 |
| `Assets/Prefabs/Stack.prefab`, `Assets/Prefabs/Tile.prefab` | Tool ghi (Task 7) |

---

### Task 1: Magnet xoá hộp chôn bị hút rỗng

**Files:**
- Modify: `Assets/_Game/Board/Domain/GameMagnet.cs` (trong `ApplyMagnet(string gid)`, sau vòng xoá thẻ; thêm hàm mới cạnh `FirstFreeAfterPull`)
- Test: `Assets/_Game/Board/Domain/SelfCheck.cs` (mục `// 8f. Booster tránh blocker`)

**Interfaces:**
- Produces: `Game.ApplyMagnet(gid)` — sau khi trả về, mọi hộp chôn (chỉ số ≥ 1) có thẻ bị hút và thành rỗng đã bị xoá khỏi `Stacks[s].Boxes`; hộp cuối mỗi stack có `IsBottom = true`. `MagnetResult.Picks[].Box` giữ chỉ số **trước** khi xoá. Hộp trên cùng rỗng vẫn còn (để `Settle` xử lý).

- [ ] **Step 1: Viết assert (đang fail)**

Trong `SelfCheck.cs`, ngay sau dòng `Ok(g2.FindMagnetTarget() != u1, "nhóm có thẻ trong hộp đóng không được nam châm hút");` thêm:

```csharp
                // Magnet hút rỗng hộp chôn thì xoá hộp đó ngay (spec stack-tile-holder Mục 7). RulesLv
                // stack 0: trên [c1,c2], chôn [c3,c4] — cả 4 là nhóm ga.
                var gm = load(true);
                var mr = gm.ApplyMagnet("ga");
                Ok(mr.Ok, "Magnet hút được nhóm ga");
                Ok(mr.Picks.Count(p => p.Stack == 0 && p.Box == 1) == 2, "Picks giữ chỉ số hộp TRƯỚC khi xoá");
                Ok(gm.Stacks[0].Boxes.Count == 1, "hộp chôn bị hút rỗng bị xoá ngay");
                Ok(Game.IsEmpty(gm.TopBox(0)), "hộp trên cùng rỗng vẫn còn, để Settle xử lý");
                Ok(gm.TopBox(0).IsBottom, "xoá hộp đáy thì hộp ngay trên thành đáy");
                gm.Settle(true);
                Ok(gm.Stacks[0].Boxes.Count == 1, "hộp đáy rỗng không bị Settle xoá — stack không mất sạch hộp");
```

(`SelfCheck.cs` đã `using System.Linq;` — dùng cho `Count`.)

- [ ] **Step 2: Chạy selfcheck, xác nhận fail**

Run: lệnh "Luật" ở Global Constraints.
Expected: `SELFCHECK FAIL: hộp chôn bị hút rỗng bị xoá ngay`

- [ ] **Step 3: Cài luật**

Trong `GameMagnet.cs`, `ApplyMagnet(string gid)`, đổi:

```csharp
            for (int k = 0; k < picks.Count; k++)
                Stacks[picks[k].Stack].Boxes[picks[k].Box].Slots[picks[k].Slot] = null;

            Cleared++;
```

thành:

```csharp
            for (int k = 0; k < picks.Count; k++)
                Stacks[picks[k].Stack].Boxes[picks[k].Box].Slots[picks[k].Slot] = null;

            RemoveEmptiedBuriedBoxes(picks);
            Cleared++;
```

và thêm hàm ngay trước `// Ô trống SAU khi hút: ...` (hàm `FirstFreeAfterPull`):

```csharp
        // Hộp chôn (chỉ số ≥ 1) vừa bị hút rỗng: xoá ngay, không để nó thành hộp rỗng chờ lộ ra (spec
        // stack-tile-holder Mục 7) — trên màn chỉ holder cuối tắt. Hộp trên cùng rỗng để Settle xử lý như
        // cũ. Duyệt ngược để chỉ số chưa xét không bị dời. Xoá hộp đáy thì hộp ngay trên thành đáy: không
        // có dòng này Settle xoá nốt hộp trên (rỗng, không phải đáy) và stack mất sạch hộp.
        // Chạy TRƯỚC khi đặt thẻ cha: PickCollapseHost chỉ dùng hộp trên cùng, không đụng chỉ số chôn.
        void RemoveEmptiedBuriedBoxes(List<MagnetPick> picks)
        {
            for (int s = 0; s < Stacks.Count; s++)
            {
                List<Box> boxes = Stacks[s].Boxes;
                bool removed = false;
                for (int b = boxes.Count - 1; b >= 1; b--)
                {
                    int bb = b, ss = s;
                    if (!IsEmpty(boxes[b]) || !picks.Exists(p => p.Stack == ss && p.Box == bb)) continue;
                    boxes.RemoveAt(b);
                    removed = true;
                }
                if (removed) boxes[boxes.Count - 1].IsBottom = true;
            }
        }
```

Lưu ý: dòng gọi đặt **trước** khối `if (collapses && hostStack >= 0)` — hostStack/hostSlot đã chốt trước đó bằng `PickCollapseHost` (chỉ số hộp trên cùng không đổi khi xoá hộp chôn).

- [ ] **Step 4: Chạy selfcheck, xác nhận pass**

Run: lệnh "Luật".
Expected: `SelfCheck OK - 2 level, luật khớp demo/check.mjs` (demo HTML không có Magnet — không phải sửa).

- [ ] **Step 5: Compile + commit**

Run: `./compilecheck.sh` → 3 dòng OK.

```bash
git add Assets/_Game/Board/Domain/GameMagnet.cs Assets/_Game/Board/Domain/SelfCheck.cs
git commit -m "Magnet removes buried boxes it empties

Buried boxes (index >= 1) that the magnet pulls empty are removed right away
instead of waiting to surface; the top box is still left to Settle. When the
bottom box goes, the box above becomes the bottom so a stack never loses all
its boxes. Picks keep their pre-removal box index for the view.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Component `FlyToTargetAnimation`

**Files:**
- Create: `Assets/_Game/Board/Views/FlyToTargetAnimation.cs`
- Test: `Assets/_Game/Board/Tests/FlyToTargetAnimationTests.cs`

**Interfaces:**
- Produces: `public sealed class FlyToTargetAnimation : LitMotionAnimationComponent` (namespace `WordStack.Board`), menu `Custom/Fly To Target`. Field serialize (tên dùng trong `SerializedProperty` ở Task 5): `target` (Transform — thẻ mini), `slot` (int — ô đích 0..3), `duration` (float, 0.3), `delay` (float), `ease` (`LitMotion.Ease`, OutQuad). Public: `int Slot { get; }`, `Transform Destination { get; set; }` (không serialize). `Play()` trả `default` khi thiếu `target` hoặc `Destination`.

- [ ] **Step 1: Viết test (đang fail)**

```csharp
// FlyToTargetAnimation: bay từ vị trí lúc Play tới vị trí world của Destination gán lúc chạy; Stop trả chỗ cũ.
using System.Reflection;
using LitMotion;
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Board.Tests
{
    public class FlyToTargetAnimationTests
    {
        static void Set(object o, string field, object value)
        {
            o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, value);
        }

        [Test]
        public void Play_ReachesDestination_Stop_RestoresStart()
        {
            var mini = new GameObject("mini").transform;
            var dst = new GameObject("dst").transform;
            try
            {
                mini.position = new Vector3(1f, 2f, 0f);
                dst.position = new Vector3(-3f, 5f, 0f);
                var fly = new FlyToTargetAnimation();
                Set(fly, "target", mini);
                Set(fly, "slot", 2);
                Assert.AreEqual(2, fly.Slot);

                fly.Destination = dst;
                var h = fly.Play();
                Assert.IsTrue(h.IsActive(), "có đích thì phải có motion");
                h.Complete();
                Assert.That(Vector3.Distance(dst.position, mini.position), Is.LessThan(1e-4f), "tới đúng đích");

                fly.OnStop();
                Assert.That(Vector3.Distance(new Vector3(1f, 2f, 0f), mini.position), Is.LessThan(1e-4f), "Stop trả chỗ cũ");

                fly.Destination = null;
                Assert.IsFalse(fly.Play().IsActive(), "không đích thì không bay");
            }
            finally
            {
                Object.DestroyImmediate(mini.gameObject);
                Object.DestroyImmediate(dst.gameObject);
            }
        }
    }
}
```

- [ ] **Step 2: Chạy compile test, xác nhận fail**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `board-tests FAIL` với lỗi `CS0246: The type or namespace name 'FlyToTargetAnimation' could not be found`.

- [ ] **Step 3: Viết component**

```csharp
// Thẻ mini bay vào ô của hộp (Stack Tile Holder — spec 2026-09-29-stack-tile-holder-design Mục 5).
// Component LitMotion tự viết (cùng kiểu SpriteGroupAlphaAnimation) vì đích là ô của hộp — hộp sống ngoài
// Stack.prefab, nên StackView gán Destination ngay trước khi Play, không lưu vào prefab. Play chụp vị trí
// hiện tại (sau khi HorizontalSpriteLayout đã căn giữa) rồi bay tới vị trí world của đích; Stop trả về chỗ
// đã chụp. Thiếu target hoặc đích thì không bay.
using System;
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

namespace WordStack.Board
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Custom/Fly To Target")]
    public sealed class FlyToTargetAnimation : LitMotionAnimationComponent
    {
        [SerializeField] Transform target;          // thẻ mini
        [SerializeField] int slot;                  // ô đích trong hộp (0..3) — StackView tra Destination theo số này
        [SerializeField] float duration = 0.3f;
        [SerializeField] float delay;
        [SerializeField] Ease ease = Ease.OutQuad;

        Vector3 from;
        bool captured;

        public int Slot => slot;
        public Transform Destination { get; set; }

        public override MotionHandle Play()
        {
            if (target == null || Destination == null) return default;
            from = target.position;
            captured = true;
            return LMotion.Create(from, Destination.position, duration)
                          .WithDelay(delay).WithEase(ease).WithCancelOnError()
                          .Bind(target, (p, t) => t.position = p);
        }

        public override void OnStop()
        {
            if (captured && target != null) target.position = from;
            captured = false;
        }
    }
}
```

- [ ] **Step 4: Compile test, xác nhận pass; chạy test trong Unity**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → `board-tests.dll OK`.
Unity Test Runner ▸ EditMode ▸ `FlyToTargetAnimationTests` → PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Views/FlyToTargetAnimation.cs Assets/_Game/Board/Views/FlyToTargetAnimation.cs.meta Assets/_Game/Board/Tests/FlyToTargetAnimationTests.cs Assets/_Game/Board/Tests/FlyToTargetAnimationTests.cs.meta
git commit -m "Add FlyToTargetAnimation LitMotion component

Flies its target from where it stands at Play to a Destination assigned at
runtime (the box slot, which lives outside Stack.prefab); Stop puts it back.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

(File `.meta` do Unity sinh khi Editor import. Chưa có `.meta` thì mở Unity cho nó import rồi mới `git add`.)

---

### Task 3: `StackView` dạng holder + `BoardController` giữ `holderConsumed`

**Files:**
- Modify (viết lại): `Assets/_Game/Board/Views/StackView.cs`
- Modify: `Assets/_Game/Board/Views/HorizontalSpriteLayout.cs:4` (comment)
- Modify: `Assets/_Game/Board/Views/BoardController.cs` (field mới, `Load`, `BuildBoard`, `RevealBox`, `UndoAnimation`, bỏ `TilesInSecondBox`)
- Test: `Assets/_Game/Board/Tests/StackViewHolderTests.cs`

**Interfaces:**
- Consumes: `FlyToTargetAnimation.Slot`, `FlyToTargetAnimation.Destination` (Task 2).
- Produces (`WordStack.Board.StackView`):
  - `public const int HolderCount = 5;`
  - `[Serializable] public class Holder { public GameObject root; public HorizontalSpriteLayout layout; public GameObject[] minis = new GameObject[4]; public LitMotionAnimation fill; public LitMotionAnimation lift; }` — field serialize `holders` (mảng `Holder`), `boxAnchor`.
  - `public static int HolderIndexOf(int consumed, int d)` — hộp chôn thứ `d` (1 = ngay dưới hộp trên cùng) nằm ở holder chỉ số `consumed + d − 1`; ngoài 0..4 hoặc `d < 1` → `-1`.
  - `public void ShowHolders(int consumed, IReadOnlyList<Box> boxes)` — `boxes[0]` là hộp trên cùng.
  - `public Transform MiniOf(int consumed, int d, int slot)` — thẻ mini **đang hiện** hoặc `null`.
  - `public bool BeginFill(int k, IReadOnlyList<Transform> slots)`, `public void HideMinis(int k)`, `public void BeginLift(int k)`, `public bool IsAnimating(int k)`, `public void EndHolder(int k)`.
  - `public static List<string> LimitWarnings(IReadOnlyList<Stack> stacks)`.
- Produces (`BoardController`): field `int[] holderConsumed` (tạo ở `Load`, không reset ở `BuildBoard`).

- [ ] **Step 1: Viết test (đang fail)**

```csharp
// StackView holder: vẽ hoàn toàn từ (consumed, danh sách hộp). Holder bị tiêu thụ từ trên xuống; Magnet xoá
// hộp chôn thì hộp sau dồn lên, holder cuối tắt; thẻ mini i bật ⇔ ô i có thẻ.
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Board.Tests
{
    public class StackViewHolderTests
    {
        GameObject go;
        StackView sv;
        StackView.Holder[] hs;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("Stack");
            sv = go.AddComponent<StackView>();
            hs = new StackView.Holder[StackView.HolderCount];
            for (int k = 0; k < hs.Length; k++)
            {
                var h = new StackView.Holder { root = new GameObject("Peek" + (k + 1)) };
                h.root.transform.SetParent(go.transform, false);
                for (int i = 0; i < h.minis.Length; i++)
                {
                    h.minis[i] = new GameObject("Mini " + i);
                    h.minis[i].transform.SetParent(h.root.transform, false);
                }
                hs[k] = h;
            }
            typeof(StackView).GetField("holders", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sv, hs);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(go); }

        static Box B(params bool[] filled)
        {
            var b = new Box();
            for (int i = 0; i < filled.Length; i++) if (filled[i]) b.Slots[i] = new Tile { Uid = "t" + i };
            return b;
        }

        bool[] On() { var r = new bool[hs.Length]; for (int k = 0; k < hs.Length; k++) r[k] = hs[k].root.activeSelf; return r; }

        [Test]
        public void HolderIndexOf_MapsBuriedBoxToHolder()
        {
            Assert.AreEqual(0, StackView.HolderIndexOf(0, 1));
            Assert.AreEqual(2, StackView.HolderIndexOf(1, 2));
            Assert.AreEqual(-1, StackView.HolderIndexOf(3, 3), "vượt 5 holder");
            Assert.AreEqual(-1, StackView.HolderIndexOf(0, 0), "d = 0 là hộp trên cùng, không có holder");
        }

        [Test]
        public void ShowHolders_ConsumedTopDown_MagnetDropsLast()
        {
            var top = B(true, true, false, false);
            var b1 = B(true, false, false, true);
            var b2 = B(false, true, true, false);
            var b3 = B(true, true, true, true);

            sv.ShowHolders(0, new List<Box> { top, b1, b2, b3 });   // dựng bàn: 3 hộp chôn
            CollectionAssert.AreEqual(new[] { true, true, true, false, false }, On());
            Assert.IsTrue(hs[0].minis[0].activeSelf && !hs[0].minis[1].activeSelf && hs[0].minis[3].activeSelf, "Peek1 = b1");

            sv.ShowHolders(1, new List<Box> { b1, b2, b3 });        // clear: b1 lên trên, Peek1 đi
            CollectionAssert.AreEqual(new[] { false, true, true, false, false }, On());
            Assert.IsTrue(!hs[1].minis[0].activeSelf && hs[1].minis[1].activeSelf && hs[1].minis[2].activeSelf, "Peek2 = b2");

            sv.ShowHolders(1, new List<Box> { b1, b3 });            // Magnet xoá b2: b3 dồn lên Peek2, Peek3 tắt
            CollectionAssert.AreEqual(new[] { false, true, false, false, false }, On());
            Assert.IsTrue(hs[1].minis[0].activeSelf && hs[1].minis[3].activeSelf, "Peek2 = b3");
        }

        [Test]
        public void MiniOf_ReturnsOnlyVisibleMini()
        {
            sv.ShowHolders(0, new List<Box> { B(), B(true, false, false, false) });
            Assert.AreSame(hs[0].minis[0].transform, sv.MiniOf(0, 1, 0));
            Assert.IsNull(sv.MiniOf(0, 1, 1), "ô trống → mini tắt");
            Assert.IsNull(sv.MiniOf(0, 2, 0), "không có hộp chôn thứ 2");
        }

        [Test]
        public void LimitWarnings_TooDeepOrBuriedBlocker()
        {
            var ok = new Stack(); for (int i = 0; i < 6; i++) ok.Boxes.Add(B());
            var deep = new Stack(); for (int i = 0; i < 7; i++) deep.Boxes.Add(B());
            var locked = new Stack(); locked.Boxes.Add(B()); locked.Boxes.Add(B());
            locked.Boxes[1].Lock = new Lock { Kind = LockKind.Clears, Need = 2 };

            Assert.AreEqual(0, StackView.LimitWarnings(new List<Stack> { ok }).Count);
            Assert.AreEqual(1, StackView.LimitWarnings(new List<Stack> { deep }).Count);
            Assert.AreEqual(1, StackView.LimitWarnings(new List<Stack> { locked }).Count);
        }
    }
}
```

- [ ] **Step 2: Chạy compile test, xác nhận fail**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `board-tests FAIL` — `CS0426`/`CS0117`: `Holder`, `HolderCount`, `ShowHolders`... chưa có.

- [ ] **Step 3: Viết lại `StackView.cs`**

```csharp
// Một vị trí trên lưới: hộp trên cùng + tối đa 5 Tile Holder bên dưới. Mỗi holder đại diện một hộp chôn
// và mang thẻ mini của nó (spec docs/superpowers/specs/2026-09-29-stack-tile-holder-design.md).
// Holder bị tiêu thụ từ trên xuống: `consumed` holder đầu đã bay đi (BoardController giữ số này — nó phải
// sống qua RebuildBoardViews), hộp chôn thứ d nằm trên holder (consumed + d − 1). View không nhớ gì: mọi
// lần vẽ đều suy từ (consumed, danh sách hộp). Animation fill/lift do Tools ▸ WordStack ▸ Build Stack Holder
// Animations dựng; code chỉ gán đích bay, Stop/Play và đọc IsPlaying.
using System;
using System.Collections.Generic;
using LitMotion.Animation;
using UnityEngine;

namespace WordStack.Board
{
    public class StackView : MonoBehaviour
    {
        public const int HolderCount = 5;

        [Serializable]
        public class Holder
        {
            public GameObject root;                    // Peek k
            public HorizontalSpriteLayout layout;      // TileMarkerHolder — dồn thẻ mini đang bật vào giữa
            public GameObject[] minis = new GameObject[Rules.BoxCapacity];   // ứng với ô 0..3 của hộp
            public LitMotionAnimation fill;            // thẻ mini bay vào ô + to lên cỡ thẻ thật
            public LitMotionAnimation lift;            // holder nhấc lên + mờ
        }

        [SerializeField] Transform boxAnchor;
        [SerializeField] Holder[] holders = new Holder[HolderCount];

        public Transform BoxAnchor => boxAnchor;

        /// <summary>Hộp chôn thứ d (1 = ngay dưới hộp trên cùng) nằm ở holder nào; -1 = không có holder.</summary>
        public static int HolderIndexOf(int consumed, int d)
        {
            int k = consumed + d - 1;
            return d >= 1 && k >= 0 && k < HolderCount ? k : -1;
        }

        Holder At(int k) { return holders != null && k >= 0 && k < holders.Length ? holders[k] : null; }

        /// <summary>Vẽ lại mọi holder. boxes[0] là hộp trên cùng; holder không mang hộp nào thì tắt.</summary>
        public void ShowHolders(int consumed, IReadOnlyList<Box> boxes)
        {
            for (int k = 0; k < HolderCount; k++)
            {
                var h = At(k);
                if (h == null || h.root == null) continue;
                int d = k - consumed + 1;
                bool on = d >= 1 && d < boxes.Count;
                h.root.SetActive(on);
                for (int i = 0; i < h.minis.Length; i++)
                    if (h.minis[i] != null) h.minis[i].SetActive(on && boxes[d].Slots[i] != null);
            }
        }

        /// <summary>Thẻ mini ô `slot` của hộp chôn thứ d nếu đang hiện, null nếu không — điểm xuất phát thẻ Magnet.</summary>
        public Transform MiniOf(int consumed, int d, int slot)
        {
            var h = At(HolderIndexOf(consumed, d));
            if (h == null || h.root == null || !h.root.activeSelf || slot < 0 || slot >= h.minis.Length) return null;
            var m = h.minis[slot];
            return m != null && m.activeSelf ? m.transform : null;
        }

        /// <summary>
        /// Thẻ mini của holder k bay vào ô: slots[i] là ô i của hộp. Tắt layout — nó ghi đè vị trí mỗi
        /// LateUpdate. False = holder không hiện hoặc chưa dựng animation (bên gọi hiện thẻ ngay như cũ).
        /// </summary>
        public bool BeginFill(int k, IReadOnlyList<Transform> slots)
        {
            var h = At(k);
            if (h == null || h.root == null || !h.root.activeSelf || h.fill == null) return false;
            if (h.layout != null) h.layout.enabled = false;
            foreach (var c in h.fill.Components)
                if (c is FlyToTargetAnimation fly)
                    fly.Destination = fly.Slot >= 0 && fly.Slot < slots.Count ? slots[fly.Slot] : null;
            h.fill.Stop();
            h.fill.Play();
            return true;
        }

        /// <summary>Thẻ thật thay chỗ thẻ mini.</summary>
        public void HideMinis(int k)
        {
            var h = At(k);
            if (h == null) return;
            foreach (var m in h.minis) if (m != null) m.SetActive(false);
        }

        public void BeginLift(int k)
        {
            var h = At(k);
            if (h == null || h.lift == null) return;
            h.lift.Stop();
            h.lift.Play();
        }

        public bool IsAnimating(int k)
        {
            var h = At(k);
            return h != null && ((h.fill != null && h.fill.IsPlaying) || (h.lift != null && h.lift.IsPlaying));
        }

        /// <summary>Stop trả holder + thẻ mini về vị trí/alpha gốc (OnStop từng component), bật lại layout, tắt holder.</summary>
        public void EndHolder(int k)
        {
            var h = At(k);
            if (h == null) return;
            if (h.lift != null) h.lift.Stop();
            if (h.fill != null) h.fill.Stop();
            if (h.layout != null) h.layout.enabled = true;
            if (h.root != null) h.root.SetActive(false);
        }

        /// <summary>Cảnh báo lúc nạp level: stack sâu hơn 6 lớp, hoặc hộp chôn có blocker (spec Mục 11).</summary>
        public static List<string> LimitWarnings(IReadOnlyList<Stack> stacks)
        {
            var w = new List<string>();
            for (int s = 0; s < stacks.Count; s++)
            {
                var boxes = stacks[s].Boxes;
                if (boxes.Count - 1 > HolderCount)
                    w.Add("stack " + s + " có " + boxes.Count + " lớp — tối đa " + (HolderCount + 1) +
                          " (1 hộp + " + HolderCount + " Tile Holder); hộp thứ " + (HolderCount + 2) + " trở đi không có holder.");
                for (int b = 1; b < boxes.Count; b++)
                    if (boxes[b].Lock.Kind != LockKind.None)
                        w.Add("stack " + s + " hộp chôn " + b + " có blocker " + boxes[b].Lock.Kind + " — blocker chỉ đặt ở hộp trên cùng.");
            }
            return w;
        }
    }
}
```

- [ ] **Step 4: Sửa comment `HorizontalSpriteLayout.cs` dòng 4**

Đổi `// bật/tắt marker (StackView.ShowDepth) tự đẩy layout ở LateUpdate frame đó.` thành
`// bật/tắt thẻ mini (StackView.ShowHolders) tự đẩy layout ở LateUpdate frame đó. StackView tắt component này trong lúc thẻ mini bay (fill).`

- [ ] **Step 5: Nối `BoardController` sang `ShowHolders`**

(a) Thêm field ngay sau dòng `readonly List<MotionHandle> running = new List<MotionHandle>();`:

```csharp

        // Số Tile Holder đã bay đi của từng stack (spec stack-tile-holder Mục 3.2) — lịch sử của view, không
        // suy được từ bàn chơi. Sống ở đây chứ không ở StackView vì RebuildBoardViews (Magnet/Shuffle/Undo) dựng
        // StackView mới. Tạo mới khi nạp level; +1 khi hộp trên cùng bị xoá lộ hộp kế, −1 khi Undo trả hộp về.
        int[] holderConsumed;
```

(b) Trong `Load()`, ngay sau `g.UndoEnabled = true;` (vẫn trong khối `try`) thêm:

```csharp
                holderConsumed = new int[g.Stacks.Count];
                foreach (var w in StackView.LimitWarnings(g.Stacks)) Debug.LogWarning("[Level] " + w);
```

(c) Trong `BuildBoard()`, đổi `sv.ShowDepth(st.Boxes.Count - 1, TilesInSecondBox(st));` thành:

```csharp
                sv.ShowHolders(holderConsumed[s], st.Boxes);
```

(d) Trong `RevealBox(int s)`, đổi `stackViews[s].ShowDepth(g.Stacks[s].Boxes.Count - 1, TilesInSecondBox(g.Stacks[s]));` thành:

```csharp
            holderConsumed[s]++;
            stackViews[s].ShowHolders(holderConsumed[s], g.Stacks[s].Boxes);
```

(e) Trong `UndoAnimation(Game prev)`, đổi `stackViews[s].ShowDepth(g.Stacks[s].Boxes.Count - 1, TilesInSecondBox(g.Stacks[s]));` thành:

```csharp
                // Hộp cũ quay về = holder vừa bay đi hiện lại (spec Mục 9).
                holderConsumed[s] = Mathf.Max(0, holderConsumed[s] - (g.Stacks[s].Boxes.Count - prev.Stacks[s].Boxes.Count));
                stackViews[s].ShowHolders(holderConsumed[s], g.Stacks[s].Boxes);
```

(f) Xoá hàm `TilesInSecondBox` cùng 2 dòng comment ngay trên nó (`// Ruột hộp nằm dưới không bao giờ đổi...` và `// nên chỉ cần tính ở đúng 2 chỗ gọi ShowDepth...`).

- [ ] **Step 6: Compile + compile test, xác nhận pass; chạy test trong Unity**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → 3 dòng OK + `board-tests.dll OK`, `meta-tests.dll OK`.
Unity Test Runner ▸ EditMode ▸ `StackViewHolderTests` → 4 PASS.

Lưu ý: tới hết task này `Stack.prefab` chưa nối `holders` (field cũ `peekLayers`/`nextTileMarkers` bỏ) — trong game holder chưa hiện cho tới Task 7 (chạy tool). Hộp vẫn nhấc đi như cũ (Task 6 mới thay).

- [ ] **Step 7: Commit**

```bash
git add Assets/_Game/Board/Views/StackView.cs Assets/_Game/Board/Views/HorizontalSpriteLayout.cs Assets/_Game/Board/Views/BoardController.cs Assets/_Game/Board/Tests/StackViewHolderTests.cs Assets/_Game/Board/Tests/StackViewHolderTests.cs.meta
git commit -m "StackView draws up to 5 tile holders from (consumed, boxes)

Holders are consumed top-down: buried box d sits on holder consumed + d - 1,
each mini tile shows iff its slot is filled. BoardController keeps the
per-stack consumed count so it survives RebuildBoardViews, and warns on
levels deeper than 6 layers or with blockers on buried boxes.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: `TileView.revealAnim`

**Files:**
- Modify: `Assets/_Game/Board/Views/TileView.cs` (field + 2 thành viên public)

**Interfaces:**
- Produces: field serialize `revealAnim` (`LitMotionAnimation`); `public void PlayReveal()` (Stop rồi Play; không có anim thì không làm gì); `public bool IsRevealing { get; }`.

- [ ] **Step 1: Thêm field + API**

Thêm `using LitMotion.Animation;` vào đầu file (nếu chưa có). Ngay sau dòng `[SerializeField] string blockers;` thêm:

```csharp

        [Tooltip("Lộ mặt khi thẻ mini vừa bay vào hộp: art mờ dần hiện + nảy. Dựng bằng Tools ▸ WordStack ▸ Build Stack Holder Animations")]
        [SerializeField] LitMotionAnimation revealAnim;

        /// <summary>Thẻ vừa thay chỗ thẻ mini: chạy reveal (art 0 → 1 + nảy). Không có anim thì thẻ hiện nguyên như cũ.</summary>
        public void PlayReveal()
        {
            if (revealAnim == null) return;
            revealAnim.Stop();
            revealAnim.Play();
        }

        public bool IsRevealing => revealAnim != null && revealAnim.IsPlaying;
```

- [ ] **Step 2: Compile**

Run: `./compilecheck.sh` → 3 dòng OK.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Board/Views/TileView.cs
git commit -m "TileView: reveal animation hook for tiles landing from a holder

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Tool `Build Stack Holder Animations`

**Files:**
- Create: `Assets/_Game/Board/Editor/AnimationBuildKit.cs`
- Modify: `Assets/_Game/Board/Editor/CountLockAnimationBuilder.cs` (dùng kit, xoá bản riêng)
- Create: `Assets/_Game/Board/Editor/StackHolderAnimationBuilder.cs`
- Test: `Assets/_Game/Board/Tests/StackHolderAnimationBuilderTests.cs`

**Interfaces:**
- Consumes: `FlyToTargetAnimation` field `target`/`slot`/`duration`/`delay`/`ease` (Task 2); `StackView` field `boxAnchor`, `holders[k].root/layout/minis/fill/lift` (Task 3); `TileView` field `revealAnim` (Task 4); `BoxView.Slot(int)`.
- Produces: `public static class StackHolderAnimationBuilder` (namespace `WordStack.Board.Editor`) với `public static int BgOrder(int k)`, `public static int MiniOrder(int k)` (k = 1..5), `public static string BuildStack(StackView sv, BoxView boxPrefab, TileView tilePrefab)`, `public static void BuildTileReveal(TileView tv)`; menu `Tools/WordStack/Build Stack Holder Animations`.

- [ ] **Step 1: Viết test (đang fail)**

```csharp
// Tool dựng animation Stack Tile Holder — chạy trên bản mở tạm của prefab thật, KHÔNG lưu.
using System.Linq;
using FigmaMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordStack.Board.Editor;

namespace WordStack.Board.Tests
{
    public class StackHolderAnimationBuilderTests
    {
        [Test]
        public void Orders_FollowSpecTable()
        {
            int[] bg = { 4, -1, -3, -5, -7 }, mini = { 8, 0, -2, -4, -6 };
            for (int k = 1; k <= 5; k++)
            {
                Assert.AreEqual(bg[k - 1], StackHolderAnimationBuilder.BgOrder(k), "nền Peek" + k);
                Assert.AreEqual(mini[k - 1], StackHolderAnimationBuilder.MiniOrder(k), "mini Peek" + k);
            }
        }

        [Test]
        public void BuildStack_WiresFiveHoldersWithFillAndLift()
        {
            var box = AssetDatabase.LoadAssetAtPath<BoxView>("Assets/Prefabs/Box.prefab");
            var tile = AssetDatabase.LoadAssetAtPath<TileView>("Assets/Prefabs/Tile.prefab");
            var stack = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Stack.prefab");
            try
            {
                var sv = stack.GetComponent<StackView>();
                StackHolderAnimationBuilder.BuildStack(sv, box, tile);

                var holders = new SerializedObject(sv).FindProperty("holders");
                Assert.AreEqual(StackView.HolderCount, holders.arraySize);
                for (int k = 0; k < holders.arraySize; k++)
                {
                    var h = holders.GetArrayElementAtIndex(k);
                    var fill = (LitMotionAnimation)h.FindPropertyRelative("fill").objectReferenceValue;
                    var lift = (LitMotionAnimation)h.FindPropertyRelative("lift").objectReferenceValue;
                    Assert.AreEqual(8, fill.Components.Count, "Peek" + (k + 1) + ": 4 × (bay + scale)");
                    Assert.AreEqual(4, fill.Components.OfType<FlyToTargetAnimation>().Count());
                    Assert.AreEqual(2, lift.Components.Count, "nhấc + mờ");
                    var minis = h.FindPropertyRelative("minis");
                    for (int i = 0; i < 4; i++)
                    {
                        var m = (GameObject)minis.GetArrayElementAtIndex(i).objectReferenceValue;
                        Assert.AreEqual("Mini " + i, m.name);
                        Assert.AreEqual(StackHolderAnimationBuilder.MiniOrder(k + 1), m.GetComponent<SpriteRenderer>().sortingOrder);
                    }
                    var root = (GameObject)h.FindPropertyRelative("root").objectReferenceValue;
                    Assert.AreEqual(StackHolderAnimationBuilder.BgOrder(k + 1), root.transform.Find("Bg").GetComponent<SpriteRenderer>().sortingOrder);
                }
                Assert.IsNull(sv.BoxAnchor.GetComponentInChildren<BoxView>(true), "hộp đo tạm đã xoá");
            }
            finally { PrefabUtility.UnloadPrefabContents(stack); }
        }

        [Test]
        public void BuildTileReveal_FadesArtAndPunches()
        {
            var tile = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Tile.prefab");
            try
            {
                var tv = tile.GetComponent<TileView>();
                StackHolderAnimationBuilder.BuildTileReveal(tv);
                var reveal = (LitMotionAnimation)new SerializedObject(tv).FindProperty("revealAnim").objectReferenceValue;
                Assert.IsNotNull(reveal);
                Assert.AreEqual(2, reveal.Components.Count);
                Assert.IsTrue(reveal.Components[0] is SpriteGroupAlphaAnimation);
                Assert.IsTrue(reveal.Components[1] is TransformScalePunchAnimation);
            }
            finally { PrefabUtility.UnloadPrefabContents(tile); }
        }
    }
}
```

- [ ] **Step 2: Chạy compile test, xác nhận fail**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `board-tests FAIL` — `StackHolderAnimationBuilder` không tồn tại.

- [ ] **Step 3: Tách hàm dùng chung — tạo `AnimationBuildKit.cs`**

```csharp
// Hàm dùng chung cho các tool dựng LitMotionAnimation bằng SerializedObject (Count Lock, Stack Holder):
// ghi component, đặt tên/target/relative, đặt thời lượng.
using System;
using LitMotion;
using LitMotion.Animation;
using UnityEditor;
using Object = UnityEngine.Object;
using Transform = UnityEngine.Transform;

namespace WordStack.Board.Editor
{
    static class AnimationBuildKit
    {
        public static void Common(SerializedProperty c, string name, Object target, bool relative)
        {
            c.FindPropertyRelative("displayName").stringValue = name;
            c.FindPropertyRelative("target").objectReferenceValue = target;
            c.FindPropertyRelative("relative").boolValue = relative;
        }

        public static void Timing(SerializedProperty s, float duration, float delay, Ease ease)
        {
            s.FindPropertyRelative("duration").floatValue = duration;
            s.FindPropertyRelative("delay").floatValue = delay;
            s.FindPropertyRelative("ease").intValue = (int)ease;
            s.FindPropertyRelative("loops").intValue = 1;
        }

        // Ghi đè toàn bộ component của anim: Parallel, không tự chạy (code gọi Play).
        public static void Write(LitMotionAnimation anim, string undoName,
                                 params (LitMotionAnimationComponent comp, Action<SerializedProperty> fill)[] parts)
        {
            Undo.RecordObject(anim, undoName);
            var so = new SerializedObject(anim);
            // version = 1 + playOnAwake = false: né migration trong OnAfterDeserialize ghi đè autoPlayMode.
            so.FindProperty("version").intValue = 1;
            so.FindProperty("playOnAwake").boolValue = false;
            so.FindProperty("autoPlayMode").enumValueIndex = 0;    // None
            so.FindProperty("animationMode").enumValueIndex = 0;   // Parallel
            var arr = so.FindProperty("components");
            arr.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++) arr.GetArrayElementAtIndex(i).managedReferenceValue = parts[i].comp;
            so.ApplyModifiedProperties();   // phải có instance rồi mới có property con để ghi
            so.Update();
            for (int i = 0; i < parts.Length; i++) parts[i].fill(arr.GetArrayElementAtIndex(i));
            so.ApplyModifiedProperties();
        }

        public static Transform FindDeep(Transform t, string name)
        {
            foreach (Transform c in t)
            {
                if (c.name == name) return c;
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
```

- [ ] **Step 4: Cho `CountLockAnimationBuilder` dùng kit**

Trong `CountLockAnimationBuilder.cs`:
- Thêm `using static WordStack.Board.Editor.AnimationBuildKit;` sau các `using`.
- Xoá 4 hàm riêng: `static void Common(...)`, `static void Timing(...)`, `static void Write(...)` (cả comment `// Ghi đè toàn bộ component của anim...` phía trên), `static Transform FindDeep(...)`.
- Đổi `Write(step,` thành `Write(step, "Build Count Lock Animations",` và `Write(unlock, parts.ToArray());` thành `Write(unlock, "Build Count Lock Animations", parts.ToArray());`.

- [ ] **Step 5: Viết `StackHolderAnimationBuilder.cs`**

```csharp
// Dựng animation Stack Tile Holder (spec docs/superpowers/specs/2026-09-29-stack-tile-holder-design.md Mục 6):
//   Stack.prefab — mỗi Peek k: đổi tên thẻ mini thành Mini 0..3 theo thứ tự con (HorizontalSpriteLayout xếp con
//                  đầu bên trái → thứ tự con = thứ tự ô), thứ tự vẽ theo bảng, hai LitMotionAnimation
//                  fill (FlyToTarget + scale lên cỡ thẻ thật) và lift (nhấc + mờ); nối 5 holder vào StackView.
//   Tile.prefab  — reveal (art mờ dần hiện + nảy), nối vào TileView.revealAnim.
// Tools ▸ WordStack ▸ Build Stack Holder Animations. Sửa thẳng asset (LoadPrefabContents → SaveAsPrefabAsset):
// ĐÓNG Prefab Mode của Stack/Tile trước khi chạy. Chạy lại = dựng lại từ đầu, ghi đè số đã chỉnh trong
// Inspector. Hoàn tác bằng git.
using System.Collections.Generic;
using System.Linq;
using FigmaMotion;
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using UnityEditor;
using UnityEngine;
using static WordStack.Board.Editor.AnimationBuildKit;

namespace WordStack.Board.Editor
{
    public static class StackHolderAnimationBuilder
    {
        const string StackPath = "Assets/Prefabs/Stack.prefab";
        const string BoxPath = "Assets/Prefabs/Box.prefab";
        const string TilePath = "Assets/Prefabs/Tile.prefab";
        const string UndoName = "Build Stack Holder Animations";

        const float FlyDur = 0.3f, FlyStagger = 0.05f;
        const float LiftDur = 0.35f, Lift = 0.25f;
        const float RevealDur = 0.25f, RevealPunch = 0.1f;

        // Thứ tự vẽ (spec Mục 3.4): Peek1 nền 4 / mini 8; Peek k ≥ 2 nền −(2k − 3) / mini −(2k − 4).
        public static int BgOrder(int k) { return k == 1 ? 4 : -(2 * k - 3); }
        public static int MiniOrder(int k) { return k == 1 ? 8 : -(2 * k - 4); }

        [MenuItem("Tools/WordStack/Build Stack Holder Animations")]
        static void Menu()
        {
            var boxPrefab = AssetDatabase.LoadAssetAtPath<BoxView>(BoxPath);
            var tilePrefab = AssetDatabase.LoadAssetAtPath<TileView>(TilePath);
            if (boxPrefab == null || tilePrefab == null) { Debug.LogError("[StackHolder] Không thấy " + BoxPath + " / " + TilePath + "."); return; }

            var stack = PrefabUtility.LoadPrefabContents(StackPath);
            try
            {
                string report = BuildStack(stack.GetComponent<StackView>(), boxPrefab, tilePrefab);
                PrefabUtility.SaveAsPrefabAsset(stack, StackPath);
                Debug.Log("[StackHolder] " + StackPath + ": " + report);
            }
            finally { PrefabUtility.UnloadPrefabContents(stack); }

            var tile = PrefabUtility.LoadPrefabContents(TilePath);
            try
            {
                BuildTileReveal(tile.GetComponent<TileView>());
                PrefabUtility.SaveAsPrefabAsset(tile, TilePath);
                Debug.Log("[StackHolder] " + TilePath + ": đã dựng reveal.");
            }
            finally { PrefabUtility.UnloadPrefabContents(tile); }
        }

        /// <summary>Dựng 5 holder trên Stack đang mở (LoadPrefabContents hoặc Prefab Mode). Trả dòng tóm tắt.</summary>
        public static string BuildStack(StackView sv, BoxView boxPrefab, TileView tilePrefab)
        {
            var so = new SerializedObject(sv);
            var anchor = (Transform)so.FindProperty("boxAnchor").objectReferenceValue;
            if (anchor == null) throw new System.InvalidOperationException("StackView chưa nối Box Anchor.");

            // Đo cỡ thẻ thật trong ô: tạm dựng hộp + thẻ dưới BoxAnchor (Awake không chạy trong Edit mode).
            var box = Object.Instantiate(boxPrefab, anchor, false);
            float tileWidth;
            try
            {
                var tile = Object.Instantiate(tilePrefab, box.Slot(0), false);
                tile.transform.localPosition = Vector3.zero;
                tileWidth = tile.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "Bg").bounds.size.x;
            }
            finally { Object.DestroyImmediate(box.gameObject); }

            var holders = so.FindProperty("holders");
            holders.arraySize = StackView.HolderCount;
            so.ApplyModifiedPropertiesWithoutUndo();   // chốt kích thước mảng trước khi ghi từng phần tử
            for (int k = 1; k <= StackView.HolderCount; k++)
            {
                var peek = FindDeep(sv.transform, "Peek" + k);
                if (peek == null) throw new System.InvalidOperationException("Không thấy Peek" + k + ".");
                var layout = peek.GetComponentInChildren<HorizontalSpriteLayout>(true);
                if (layout == null || layout.transform.childCount != Rules.BoxCapacity)
                    throw new System.InvalidOperationException("Peek" + k + ": TileMarkerHolder phải có đúng " + Rules.BoxCapacity + " thẻ mini.");

                var minis = new List<Transform>();
                for (int i = 0; i < Rules.BoxCapacity; i++)
                {
                    var m = layout.transform.GetChild(i);
                    m.name = "Mini " + i;
                    m.GetComponent<SpriteRenderer>().sortingOrder = MiniOrder(k);
                    minis.Add(m);
                }
                peek.Find("Bg").GetComponent<SpriteRenderer>().sortingOrder = BgOrder(k);

                // Scale đích: thẻ mini to lên bằng bề ngang thẻ thật.
                float miniWidth = minis[0].GetComponent<SpriteRenderer>().bounds.size.x;
                var fullScale = minis[0].localScale * (tileWidth / miniWidth);

                var anims = peek.GetComponents<LitMotionAnimation>();
                var fill = anims.Length > 0 ? anims[0] : peek.gameObject.AddComponent<LitMotionAnimation>();
                var lift = anims.Length > 1 ? anims[1] : peek.gameObject.AddComponent<LitMotionAnimation>();

                var parts = new List<(LitMotionAnimationComponent, System.Action<SerializedProperty>)>();
                for (int i = 0; i < minis.Count; i++)
                {
                    var m = minis[i];
                    int slot = i;
                    float delay = i * FlyStagger;
                    parts.Add((new FlyToTargetAnimation(), c =>
                    {
                        c.FindPropertyRelative("displayName").stringValue = m.name + " · bay vào ô " + slot;
                        c.FindPropertyRelative("target").objectReferenceValue = m;
                        c.FindPropertyRelative("slot").intValue = slot;
                        c.FindPropertyRelative("duration").floatValue = FlyDur;
                        c.FindPropertyRelative("delay").floatValue = delay;
                        c.FindPropertyRelative("ease").intValue = (int)Ease.OutQuad;
                    }));
                    var from = m.localScale;
                    parts.Add((new TransformScaleAnimation(), c =>
                    {
                        Common(c, m.name + " · to lên", m, false);
                        var s = c.FindPropertyRelative("settings");
                        s.FindPropertyRelative("startValue").vector3Value = from;
                        s.FindPropertyRelative("endValue").vector3Value = fullScale;
                        Timing(s, FlyDur, delay, Ease.OutQuad);
                    }));
                }
                Write(fill, UndoName, parts.ToArray());

                Write(lift, UndoName,
                    (new TransformPositionAnimation(), c =>
                    {
                        Common(c, peek.name + " · nhấc lên", peek, true);
                        var s = c.FindPropertyRelative("settings");
                        s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                        s.FindPropertyRelative("endValue").vector3Value = new Vector3(0f, Lift, 0f);
                        Timing(s, LiftDur, 0f, Ease.OutCubic);
                    }),
                    (new SpriteGroupAlphaAnimation(), c =>
                    {
                        Common(c, peek.name + " · mờ", peek, false);
                        var s = c.FindPropertyRelative("settings");
                        s.FindPropertyRelative("startValue").floatValue = 1f;
                        s.FindPropertyRelative("endValue").floatValue = 0f;
                        Timing(s, LiftDur, 0f, Ease.OutCubic);
                    }));

                var h = holders.GetArrayElementAtIndex(k - 1);
                h.FindPropertyRelative("root").objectReferenceValue = peek.gameObject;
                h.FindPropertyRelative("layout").objectReferenceValue = layout;
                var ms = h.FindPropertyRelative("minis");
                ms.arraySize = Rules.BoxCapacity;
                for (int i = 0; i < Rules.BoxCapacity; i++) ms.GetArrayElementAtIndex(i).objectReferenceValue = minis[i].gameObject;
                h.FindPropertyRelative("fill").objectReferenceValue = fill;
                h.FindPropertyRelative("lift").objectReferenceValue = lift;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return StackView.HolderCount + " holder · thẻ thật rộng " + tileWidth.ToString("0.###") + " (world).";
        }

        /// <summary>Tile.prefab: art 0 → 1 + nảy nhẹ, nối vào TileView.revealAnim.</summary>
        public static void BuildTileReveal(TileView tv)
        {
            var art = tv.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "Art");
            var anims = tv.GetComponents<LitMotionAnimation>();
            var reveal = anims.Length > 0 ? anims[0] : tv.gameObject.AddComponent<LitMotionAnimation>();
            Write(reveal, UndoName,
                (new SpriteGroupAlphaAnimation(), c =>
                {
                    Common(c, "Art · hiện", art.transform, false);
                    var s = c.FindPropertyRelative("settings");
                    s.FindPropertyRelative("startValue").floatValue = 0f;
                    s.FindPropertyRelative("endValue").floatValue = 1f;
                    Timing(s, RevealDur, 0f, Ease.OutQuad);
                    s.FindPropertyRelative("immediateBind").boolValue = true;   // alpha 0 ngay frame dựng thẻ, không nháy
                }),
                (new TransformScalePunchAnimation(), c =>
                {
                    Common(c, "Thẻ · nảy", tv.transform, true);
                    var s = c.FindPropertyRelative("settings");
                    s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                    s.FindPropertyRelative("endValue").vector3Value = Vector3.one * RevealPunch;
                    Timing(s, RevealDur, 0f, Ease.Linear);
                    s.FindPropertyRelative("options").FindPropertyRelative("Frequency").intValue = 8;
                    s.FindPropertyRelative("options").FindPropertyRelative("DampingRatio").floatValue = 2.4f;
                }));
            var so = new SerializedObject(tv);
            so.FindProperty("revealAnim").objectReferenceValue = reveal;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
```

- [ ] **Step 6: Compile + compile test, xác nhận pass; chạy test trong Unity**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.
Unity Test Runner ▸ EditMode ▸ `StackHolderAnimationBuilderTests` (3 test) → PASS. Các test này chỉ mở tạm prefab, không lưu — `git status` sau khi chạy không được có `Stack.prefab`/`Tile.prefab` đổi.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Game/Board/Editor/AnimationBuildKit.cs Assets/_Game/Board/Editor/AnimationBuildKit.cs.meta Assets/_Game/Board/Editor/CountLockAnimationBuilder.cs Assets/_Game/Board/Editor/StackHolderAnimationBuilder.cs Assets/_Game/Board/Editor/StackHolderAnimationBuilder.cs.meta Assets/_Game/Board/Tests/StackHolderAnimationBuilderTests.cs Assets/_Game/Board/Tests/StackHolderAnimationBuilderTests.cs.meta
git commit -m "Tool: Build Stack Holder Animations

Builds fill (mini tiles fly into the slots and grow to tile size, staggered)
and lift (holder rises and fades) on Peek1..5 of Stack.prefab, sets the
holder sorting orders, wires the holders into StackView, and builds the tile
reveal (art fades in + punch) on Tile.prefab. Shared SerializedObject helpers
move into AnimationBuildKit, also used by the count-lock tool.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Luồng `RevealFromHolder`, Magnet từ thẻ mini, bỏ `LiftAway`

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs` (`Settle`, `LiftAwayBox`, `RevealBox`, `SpawnTiles`, `MagnetAnimation`, comment field `running`)
- Modify: `Assets/_Game/Board/Views/BoxView.cs` (xoá `LiftAway`, sửa tooltip `unlockDur`)

**Interfaces:**
- Consumes: `StackView.HolderIndexOf`, `BeginFill`, `HideMinis`, `BeginLift`, `IsAnimating`, `EndHolder`, `ShowHolders`, `MiniOf` (Task 3); `TileView.PlayReveal`, `IsRevealing` (Task 4); `holderConsumed` (Task 3); `MagnetResult.Picks[].Box` là chỉ số trước khi xoá (Task 1).
- Produces: `IEnumerator RevealFromHolder(int s)`; `List<TileView> SpawnTiles(int s)`.

- [ ] **Step 1: `SpawnTiles` trả danh sách thẻ vừa dựng**

Đổi hàm `void SpawnTiles(int s)` thành:

```csharp
        List<TileView> SpawnTiles(int s)
        {
            var spawned = new List<TileView>();
            var box = g.TopBox(s);
            if (box == null) return spawned;
            pairOrdinals.Remove(s);   // hộp mới (dựng bàn / lộ hộp dưới) → sticky làm lại từ đầu
            var counts = GroupCountsIn(box);
            var ordinals = PairOrdinalsFor(s, box, counts);
            for (int i = 0; i < box.Slots.Length; i++)
            {
                var t = box.Slots[i];
                if (t == null) continue;
                var tv = Instantiate(tilePrefab, boxViews[s].Slot(i), false);
                tv.transform.localPosition = Vector3.zero;
                tv.Bind(t, ArtOf(t));
                tv.SetMatchState(counts[t.GroupId], OrdinalOf(ordinals, t.GroupId));
                tiles[t.Uid] = tv;
                spawned.Add(tv);
            }
            return spawned;
        }
```

- [ ] **Step 2: Thay `LiftAwayBox` + `RevealBox` bằng `RevealFromHolder`**

Xoá nguyên hai hàm `LiftAwayBox(int s)` (cùng 2 dòng comment `// Hộp rỗng bị xoá (GDD §9.3 ...` phía trên) và `RevealBox(int s)`, đặt vào chỗ `RevealBox`:

```csharp
        // Hộp trên cùng vừa bị xoá, hộp dưới lộ lên (spec stack-tile-holder Mục 4). Hộp đứng yên:
        // thẻ mini của holder trên cùng bay vào ô → thẻ thật hiện art ∥ holder nhấc đi → dọn.
        // Không có holder (sâu hơn 6 lớp) hoặc holder chưa dựng animation → hiện thẻ ngay như cũ.
        // Chạy trong Settle; Load() cắt nó bằng StopAllCoroutines, RebuildBoardViews không chen vào
        // (Magnet/Shuffle/Undo chỉ chạy khi bàn đứng yên).
        IEnumerator RevealFromHolder(int s)
        {
            var sv = stackViews[s];
            var bv = boxViews[s];
            bv.ResetVisual();

            int k = StackView.HolderIndexOf(holderConsumed[s], 1);
            var slots = new Transform[Rules.BoxCapacity];
            for (int i = 0; i < slots.Length; i++) slots[i] = bv.Slot(i);

            bool animated = k >= 0 && sv.BeginFill(k, slots);
            if (animated)
            {
                while (sv.IsAnimating(k)) yield return null;
                sv.HideMinis(k);
            }

            var spawned = SpawnTiles(s);
            if (animated)
            {
                sv.BeginLift(k);
                foreach (var tv in spawned) tv.PlayReveal();
                while (sv.IsAnimating(k) || spawned.Exists(tv => tv != null && tv.IsRevealing)) yield return null;
                sv.EndHolder(k);
            }

            holderConsumed[s]++;
            sv.ShowHolders(holderConsumed[s], g.Stacks[s].Boxes);
            RefreshBlockerVisuals();                       // hộp vừa lộ có thể đang khoá
        }
```

Trong `Settle`, đổi:

```csharp
                if (ev.BoxRemoved)
                {
                    yield return LiftAwayBox(ev.Stack);
                    RevealBox(ev.Stack);
                }
```

thành:

```csharp
                if (ev.BoxRemoved) yield return RevealFromHolder(ev.Stack);
```

(Lưu ý: bước (d) của Task 3 đã đặt `holderConsumed[s]++` trong `RevealBox` — hàm đó bị xoá ở bước này, số đếm giờ tăng trong `RevealFromHolder`.)

Trong comment của field `running`, đổi `// MergeTiles, LiftAwayBox, SpawnCollapsedTile...)` thành `// MergeTiles, SpawnCollapsedTile...)`.

- [ ] **Step 3: Thẻ Magnet từ hộp chôn bay ra từ thẻ mini**

Trong `MagnetAnimation`, đổi:

```csharp
                    tv = Instantiate(tilePrefab, root, false);
                    tv.transform.position = boxViews[p.Stack].transform.position;
```

thành:

```csharp
                    tv = Instantiate(tilePrefab, root, false);
                    // Hộp chôn có holder → bay ra từ đúng thẻ mini (spec Mục 7.2); Picks giữ chỉ số hộp TRƯỚC khi
                    // Magnet xoá hộp chôn rỗng, holder còn đang vẽ trạng thái đó. Không có thì từ giữa hộp như cũ.
                    var mini = p.Box > 0 ? stackViews[p.Stack].MiniOf(holderConsumed[p.Stack], p.Box, p.Slot) : null;
                    tv.transform.position = mini != null ? mini.position : boxViews[p.Stack].transform.position;
                    if (mini != null) mini.gameObject.SetActive(false);
```

- [ ] **Step 4: Bỏ `BoxView.LiftAway`**

Trong `BoxView.cs`: xoá hàm `public MotionHandle LiftAway()` cùng khối `/// <summary> Hộp rỗng bị xoá: ...` phía trên. Đổi tooltip của `unlockDur` thành:
`[Tooltip("Mở khoá group lock đi đường dự phòng (không qua OpenGroupLock): trượt nhẹ lên + mờ dần (giây)")]`
(`unlockDur` / `unlockLift` / `unlockEase` giữ — `Unlock` vẫn dùng.)

- [ ] **Step 5: Compile + kiểm luật**

Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh` → tất cả OK.
Run: lệnh "Luật" → `SelfCheck OK - 2 level, ...`.
Run: `grep -rn "LiftAway\|ShowDepth\|TilesInSecondBox\|RevealBox" Assets/_Game --include=*.cs` → không còn kết quả.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Views/BoardController.cs Assets/_Game/Board/Views/BoxView.cs
git commit -m "Reveal the next box from its tile holder

When the top box is removed the box stays: the top holder's mini tiles fly
into the slots, real tiles fade their art in while the holder lifts away,
then the holder is consumed. Magnet tiles pulled from buried boxes start at
their mini tile. BoxView.LiftAway and BoardController.LiftAwayBox are gone.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Background −100, chạy tool, kiểm trong Unity, cập nhật spec

**Files:**
- Modify: `Assets/Scenes/Main.unity` (SpriteRenderer của `Background_0`)
- Modify (tool ghi): `Assets/Prefabs/Stack.prefab`, `Assets/Prefabs/Tile.prefab`
- Modify: `docs/superpowers/specs/2026-09-29-stack-tile-holder-design.md`

- [ ] **Step 1: Hạ `Background_0` xuống −100**

Điều kiện: `Main.unity` đang mở trong Unity phải đã Save (hoặc mở scene khác). `Main.unity` là scene duy nhất bật trong Build Settings.

```bash
python - <<'EOF'
import io, re
p = 'Assets/Scenes/Main.unity'
s = io.open(p, encoding='utf-8', newline='').read()
go = re.search(r'(?m)^--- !u!1 &(-?\d+)\n(?:(?!^--- ).)*?m_Name: Background_0\n', s, re.S).group(1)
m = re.search(r'(?m)^--- !u!212 &-?\d+\n(?:(?!^--- ).)*?m_GameObject: \{fileID: ' + go + r'\}(?:(?!^--- ).)*', s, re.S)
blk = m.group(0)
assert blk.count('m_SortingOrder: 0') == 1, 'Background_0 không ở order 0'
s = s[:m.start()] + blk.replace('m_SortingOrder: 0', 'm_SortingOrder: -100') + s[m.end():]
io.open(p, 'w', encoding='utf-8', newline='').write(s)
print('Background_0 -> -100')
EOF
git diff --stat Assets/Scenes/Main.unity
```

Expected: `Background_0 -> -100`, diff 1 dòng đổi.

- [ ] **Step 2: Chạy tool**

Trong Unity: đóng Prefab Mode của `Stack.prefab` / `Tile.prefab` nếu đang mở → **Tools ▸ WordStack ▸ Build Stack Holder Animations**.
Expected Console: `[StackHolder] Assets/Prefabs/Stack.prefab: 5 holder · thẻ thật rộng … (world).` và `[StackHolder] Assets/Prefabs/Tile.prefab: đã dựng reveal.`

Kiểm: mở `Stack.prefab` → `StackView` có 5 holder đủ `root/layout/minis/fill/lift`; mỗi Peek có 2 `LitMotionAnimation`; `TileMarkerHolder` có `Mini 0..3`. Mở `Tile.prefab` → `TileView.Reveal Anim` đã nối.

- [ ] **Step 3: Chạy toàn bộ test EditMode**

Unity Test Runner ▸ EditMode ▸ Run All → PASS (gồm `FlyToTargetAnimationTests`, `StackViewHolderTests`, `StackHolderAnimationBuilderTests`, `FigmaMotionImporterTests`, …).

- [ ] **Step 4: Kiểm bằng mắt trong Play mode**

Dùng Cheat panel ô LEVEL để nhảy màn (catalog: màn số thứ tự theo `SO_LevelCatalog`):
1. `lv-002` hoặc `lv-008` (4 hộp mỗi stack): đầu màn mỗi stack hiện Peek1-2-3; thẻ mini lớp dưới bị nền lớp trên che.
2. Clear hộp trên cùng: thẻ mini Peek1 bay lệch nhịp vào đúng ô, to lên cỡ thẻ; thẻ thật hiện art + nảy; Peek1 nhấc lên + mờ rồi tắt; thẻ mini Peek2 lộ ra. Không lớp nào chìm dưới background.
3. Magnet trên nhóm có thẻ ở hộp chôn: thẻ bay ra từ thẻ mini; hộp chôn bị hút rỗng → holder cuối tắt ngay.
4. Kéo thẻ cuối ra khỏi một hộp (hộp bị xoá không qua clear) → Undo: holder vừa bay đi hiện lại kèm thẻ mini.
5. Shuffle: thẻ mini cập nhật tức thì.
6. Bấm chơi lại (Retry) giữa lúc holder đang bay/nhấc: bàn dựng lại sạch, không lỗi Console.

Chỉnh số trong Inspector nếu cần (thời lượng, delay, độ nhấc) — nhớ chạy lại tool là ghi đè.

- [ ] **Step 5: Cập nhật spec cho khớp code**

Trong `docs/superpowers/specs/2026-09-29-stack-tile-holder-design.md`:
- Mục 3.1: bỏ gạch đầu dòng `consumed (int) — ...` khỏi `StackView`; thêm câu: "`consumed` do `BoardController` giữ theo stack (`holderConsumed[]`), tạo khi nạp level — `RebuildBoardViews` sau Magnet/Shuffle/Undo dựng `StackView` mới."
- Mục 3.2: đổi câu cuối thành "`consumed` là lịch sử của view (`BoardController.holderConsumed`), không suy ra được từ bàn chơi."
- Mục 6: đổi "Chạy trong `Stack.prefab`, chọn object gốc (có `StackView`). Undo được." thành "Sửa thẳng asset `Stack.prefab` + `Tile.prefab` (đóng Prefab Mode trước); hoàn tác bằng git." và bước 1 thành "đổi tên con của `TileMarkerHolder` thành `Mini 0..3` theo **thứ tự con** (layout xếp con đầu bên trái)".
- Mục 7.1: bỏ câu "`MagnetResult` thêm danh sách hộp chôn đã xoá ..."; thêm "Xoá hộp đáy thì hộp ngay trên thành đáy (`IsBottom`)."
- Mục 10: thay bằng "Holder bị huỷ cùng `StackView` khi `DestroyBoard` (animation dừng theo `OnDestroy`); `Load()` cắt `RevealFromHolder` bằng `StopAllCoroutines`. Không có `Abort()`."

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Main.unity Assets/Prefabs/Stack.prefab Assets/Prefabs/Tile.prefab docs/superpowers/specs/2026-09-29-stack-tile-holder-design.md
git commit -m "Build stack holder animations; background below holders

Background_0 moves to order -100 so holder layers can sit below the box.
Stack.prefab and Tile.prefab carry the holder fill/lift and tile reveal
animations built by the tool. Spec updated to match the implementation.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

(Kiểm `git status` trước khi add: chỉ stage đúng 4 file trên — không stage font TMP, `SO_LevelCatalog.asset`, `Default Local Group.asset`.)
