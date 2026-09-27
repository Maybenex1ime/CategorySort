# Mở hộp khoá nhóm (Lock Root) theo Figma — kế hoạch thực thi

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng lại trọn timeline mở hộp khoá nhóm mới nhất trong Figma (Group 547, 8 node chuyển động) lên `Lock Root` của `Box.prefab` bằng LitMotion.Animation, chỉnh + preview được trong Inspector.

**Architecture:** Một tool Editor đọc hierarchy `Lock Root`, dựng một `LitMotionAnimation` (Parallel, không tự chạy) gồm các component Position/Scale có sẵn của package và một component custom `SpriteGroupAlphaAnimation` cho opacity cả khối. Số liệu Figma nằm trong tool; curve ease dựng bằng helper runtime `FigmaEase` (có test). `BoxView.OpenGroupLock` chỉ còn: Key Tile mờ → `LitMotionAnimation.Play()` chờ hết → tắt root, trả trạng thái author.

**Tech Stack:** Unity 6000.3.8f1 · LitMotion 2.0.2 + LitMotion.Animation 2.0.2 (git URL, commit `ab6e92bfe78ff911def2fd3c4e9c79bb5a186946`) · DOTween (chỉ còn bước mờ Key Tile) · NUnit EditMode · `./compilecheck.sh`.

## Global Constraints

- **Nguồn:** Figma `webZuDwy5Y6uGxGt0psafX`, node `373:1245` (Group 547). Cohort 2000 ms `loop` trong Figma; trong game **chạy một lần**. Đọc lại bằng `get_motion_context(recursive=true)` nếu nghi số đã đổi.
- **Đơn vị:** 1 px Figma = **0.005** unit trong không gian `Lock Root` (art xuất 2x, PPU 400; `Blocker_Box_LitBack.png` 158 px = `Lit` 79 px Figma). Trục y Figma hướng xuống → +N px Figma = **−N × 0.005** trên Unity.
- **Ease-out Figma** = CSS `cubic-bezier(0, 0, 0.58, 1)`. Giá trị chuẩn: t=0.25 → 0.3781, t=0.5 → 0.6846, t=0.75 → 0.9065.
- **Bảng timeline (giây = % × 2 s):**

| Figma node | Object trong `Lock Root` | Track | Delay | Duration | Giá trị cuối (relative) | Ease |
|---|---|---|---|---|---|---|
| Rectangle 309 (×2) | `Lit`, `Lit (1)` | X | 0 | 0.458 | −37 px (tấm phải +37) | 2 đoạn ease-out: −30 px @0.229, −37 @0.458 |
| Rectangle 309 (×2) | `Lit`, `Lit (1)` | scaleX | 0.0577 | 0.4003 | −scale.x | ease-out |
| Upper Layer (×2) | `UpperLit` (×2) | X | 0 | 0.458 | −39 px (tấm phải +39) | 8 mẫu nội suy thẳng (xem Task 3) |
| Upper Layer (×2) | `UpperLit` (×2) | scaleX | 0.063 | 0.395 | −scale.x | ease-out |
| Group 558 (thanh trên của Middle) | `Middle/Top` | Y | 0.459 | 0.030 | +5 px Figma → −0.025 | ease-out |
| Group 558 | `Middle/Top` | scaleY | 0.459 | 0.030 | −scale.y | ease-out |
| Group 557 (thanh dưới của Middle) | `Middle/Bottom` | Y | 0.459 | 0.030 | −0.025 | ease-out |
| Group 557 | `Middle/Bottom` | scaleY | 0.459 | 0.030 | −scale.y | ease-out |
| Upper | `Upper` | Y | 0.46 | 0.030 | +20 px Figma → −0.1 | ease-out |
| Group 547 | `Lock Root` | opacity | 0.49 | 0.030 | 1 → 0 (tuyệt đối) | ease-out |
| Group 547 | `Lock Root` | scale | 0.492 | 0.030 | ×0.9 → −0.1 × scale.x/y | ease-out |

  Tổng thời lượng 0.522 s. Không có motion: `MiddleNoChanges`, `Lower` (sprite của chính `Lock Root`), `Lowest Layer` (ẩn trong Figma). Ba đoạn cuối chỉ dài 30 ms (≈2 frame ở 60 fps) — đúng như Figma, không tự kéo dài.
- **Hướng tấm cửa:** Figma cho cả hai tấm trượt trái; user đã chốt tấm bên phải tâm (`localPosition.x > 0`) **lật dấu X** để gập về mép phải.
- **Scale quanh tâm:** mọi sprite được scale phải để pivot **Center** (transform-origin mặc định của Figma).
- **Không thêm dependency.** Chỉ LitMotion + LitMotion.Animation đã có trong `Packages/manifest.json`.
- **Cổng máy:** `./compilecheck.sh` in `game.dll OK`, `editor.dll OK`, `meta.dll OK`. Test EditMode chạy trong Unity: *Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All* (batchmode bị chặn trên máy này).
- **Commit:** chỉ `git add` đúng file của task; nhánh đang có file dirty không liên quan (font, `Build.rar`, `ProjectSettings`…) — không đụng. Mỗi commit kết thúc bằng dòng `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.

---

## File Structure

| File | Trách nhiệm |
|---|---|
| `Assets/_Game/Board/Views/FigmaEase.cs` (mới) | Dựng AnimationCurve từ keyframe Figma: ease-out weighted (đúng cubic-bezier), nội suy thẳng. Không biết gì về lock box. |
| `Assets/_Game/Board/Views/SpriteGroupAlphaAnimation.cs` (mới) | Component custom LitMotion.Animation: nhân hệ số lên alpha author của mọi SpriteRenderer dưới target (opacity cả group). |
| `Assets/_Game/Board/Editor/LockOpenAnimationBuilder.cs` (mới, thay `LockShutterAnimationBuilder.cs`) | Tool: số Figma + tra object trong `Lock Root` + tách `Middle` + ghi `LitMotionAnimation`. |
| `Assets/_Game/Board/Views/BoxView.cs` (sửa) | `OpenGroupLock`: Key Tile mờ → chạy animation → trả trạng thái. Bỏ bước lồng trượt lên/mờ cũ. |
| `Assets/_Game/Board/Tests/FigmaEaseTests.cs`, `SpriteGroupAlphaAnimationTests.cs` (mới) | Test EditMode. |
| `Assets/_Game/Board/Tests/WordStack.Board.Tests.asmdef` (sửa) | Thêm reference `LitMotion`, `LitMotion.Animation`. |
| `Assets/_Game/Art/Blocker/Blocker_Box/Blocker_Box_Middle.png.meta` (Unity sửa) | Cắt thành 2 sprite. |
| `Assets/Prefabs/Box.prefab` (Unity sửa) | Kết quả chạy tool. |

---

### Task 0: Chốt baseline hiện tại

Phiên trước đã cài LitMotion, dựng cửa chớp và chuyển `OpenGroupLock` sang coroutine nhưng **chưa commit** trên nhánh này. Commit trước để các task sau có diff sạch.

**Files:** `Packages/manifest.json`, `Packages/packages-lock.json`, `compilecheck.sh`, `Assets/_Game/Board/WordStack.Board.asmdef`, `Assets/_Game/Board/Editor/WordStack.Board.Editor.asmdef`, `Assets/_Game/Board/Views/BoxView.cs`, `Assets/_Game/Board/Views/BoardController.cs`, `Assets/_Game/Board/Editor/LockShutterAnimationBuilder.cs` (+ `.meta`)

- [ ] **Step 1: Kiểm compile**

Run: `./compilecheck.sh`
Expected: ba dòng `game.dll OK`, `editor.dll OK`, `meta.dll OK`.

- [ ] **Step 2: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json compilecheck.sh \
  Assets/_Game/Board/WordStack.Board.asmdef Assets/_Game/Board/Editor/WordStack.Board.Editor.asmdef \
  Assets/_Game/Board/Views/BoxView.cs Assets/_Game/Board/Views/BoardController.cs \
  Assets/_Game/Board/Editor/LockShutterAnimationBuilder.cs Assets/_Game/Board/Editor/LockShutterAnimationBuilder.cs.meta
git commit -m "feat(blocker): group lock opens with tiles flying in + LitMotion shutter

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 1: `FigmaEase` — curve đúng keyframe Figma

**Files:**
- Create: `Assets/_Game/Board/Views/FigmaEase.cs`
- Test: `Assets/_Game/Board/Tests/FigmaEaseTests.cs`

**Interfaces:**
- Produces: `public static class FigmaEase` (namespace `WordStack.Board`) với
  - `AnimationCurve EaseOut(params (float t, float v)[] keys)` — mỗi đoạn là ease-out Figma
  - `AnimationCurve CssEaseOut01()` — `EaseOut((0,0),(1,1))`
  - `AnimationCurve Linear(params (float t, float v)[] keys)` — nội suy thẳng

- [ ] **Step 1: Viết test fail**

```csharp
// Curve dựng từ keyframe Figma phải trùng số Figma, không xấp xỉ.
// Số chuẩn của cubic-bezier(0, 0, 0.58, 1) giải số x(u) = t (plan 2026-09-27, Global Constraints).
using NUnit.Framework;

namespace WordStack.Board.Tests
{
    public class FigmaEaseTests
    {
        [TestCase(0.25f, 0.3781f)]
        [TestCase(0.5f, 0.6846f)]
        [TestCase(0.75f, 0.9065f)]
        public void CssEaseOut01_MatchesCubicBezier(float t, float expected)
        {
            Assert.AreEqual(expected, FigmaEase.CssEaseOut01().Evaluate(t), 0.005f);
        }

        [Test]
        public void EaseOut_MultiSegment_HitsEveryKeyAndEasesEachSegment()
        {
            var c = FigmaEase.EaseOut((0f, 0f), (0.5f, 0.8f), (1f, 1f));
            Assert.AreEqual(0.8f, c.Evaluate(0.5f), 1e-4f);
            Assert.AreEqual(1f, c.Evaluate(1f), 1e-4f);
            Assert.AreEqual(0.8f * 0.6846f, c.Evaluate(0.25f), 0.005f);   // giữa đoạn 1
        }

        [Test]
        public void Linear_InterpolatesStraightBetweenSamples()
        {
            var c = FigmaEase.Linear((0f, 0f), (0.2f, 0.5f), (1f, 1f));
            Assert.AreEqual(0.25f, c.Evaluate(0.1f), 1e-4f);
            Assert.AreEqual(0.75f, c.Evaluate(0.6f), 1e-4f);
        }
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Unity: Test Runner ▸ EditMode ▸ Run All.
Expected: không compile — `The name 'FigmaEase' does not exist in the current context`.

- [ ] **Step 3: Viết `FigmaEase`**

```csharp
// Dựng AnimationCurve từ keyframe Figma để curve trong Inspector trùng số Figma.
//
// Ease-out của Figma là CSS cubic-bezier(0, 0, 0.58, 1). Key weighted biến mỗi đoạn Hermite thành
// đúng cubic bezier đó: handle ra của key đầu dài ~0 (P1 ≡ P0), handle vào của key sau nằm ngang và
// dài 0.42 đoạn (P2 ở 58 % thời gian, đã tới giá trị đích).
using UnityEngine;

namespace WordStack.Board
{
    public static class FigmaEase
    {
        const float EaseOutInWeight = 0.42f;
        const float ZeroWeight = 0.0001f;   // 0 tuyệt đối để Unity tự xử; gần 0 là đủ trùng P1 ≡ P0

        /// <summary>Mỗi đoạn giữa hai key kề nhau là ease-out Figma.</summary>
        public static AnimationCurve EaseOut(params (float t, float v)[] keys)
        {
            var k = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                k[i] = new Keyframe(keys[i].t, keys[i].v, 0f, 0f, EaseOutInWeight, ZeroWeight) { weightedMode = WeightedMode.Both };
            return new AnimationCurve(k);
        }

        /// <summary>Ease-out Figma chuẩn hoá 0..1 — dùng làm Custom Ease Curve của LitMotion.</summary>
        public static AnimationCurve CssEaseOut01() { return EaseOut((0f, 0f), (1f, 1f)); }

        /// <summary>Nội suy thẳng giữa các mẫu (Figma bake sẵn): tangent hai phía = độ dốc đoạn kề.</summary>
        public static AnimationCurve Linear(params (float t, float v)[] keys)
        {
            var k = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                float inT = i > 0 ? Slope(keys[i - 1], keys[i]) : Slope(keys[i], keys[i + 1]);
                float outT = i < keys.Length - 1 ? Slope(keys[i], keys[i + 1]) : inT;
                k[i] = new Keyframe(keys[i].t, keys[i].v, inT, outT);
            }
            return new AnimationCurve(k);
        }

        static float Slope((float t, float v) a, (float t, float v) b) { return (b.v - a.v) / (b.t - a.t); }
    }
}
```

- [ ] **Step 4: Chạy test, xác nhận pass**

Unity: Test Runner ▸ EditMode ▸ Run All. Expected: 5 test `FigmaEaseTests` xanh (3 TestCase + 2 Test).
Run: `./compilecheck.sh` → 3 dòng `OK`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Views/FigmaEase.cs Assets/_Game/Board/Views/FigmaEase.cs.meta \
  Assets/_Game/Board/Tests/FigmaEaseTests.cs Assets/_Game/Board/Tests/FigmaEaseTests.cs.meta
git commit -m "feat(anim): FigmaEase builds curves that match Figma keyframes

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 2: `SpriteGroupAlphaAnimation` — opacity cả group

**Files:**
- Create: `Assets/_Game/Board/Views/SpriteGroupAlphaAnimation.cs`
- Test: `Assets/_Game/Board/Tests/SpriteGroupAlphaAnimationTests.cs`
- Modify: `Assets/_Game/Board/Tests/WordStack.Board.Tests.asmdef` (thêm reference)

**Interfaces:**
- Produces: `public sealed class SpriteGroupAlphaAnimation : LitMotion.Animation.FloatPropertyAnimationComponent<Transform>` (menu *Custom/Sprite Group Alpha*), serialized field kế thừa: `target` (Transform), `settings` (`startValue`/`endValue` là hệ số alpha), `relative`.
- Produces: `public static void ApplyAlpha(SpriteRenderer[] rs, float[] baseAlpha, float k)`.

- [ ] **Step 1: Thêm reference cho test asmdef**

Trong `Assets/_Game/Board/Tests/WordStack.Board.Tests.asmdef`, mảng `references` thành:

```json
    "references": [
        "WordStack.Board",
        "LitMotion",
        "LitMotion.Animation",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
```

- [ ] **Step 2: Viết test fail**

```csharp
// Opacity cả group = alpha author của TỪNG renderer × một hệ số chung (không ghi đè về cùng một alpha).
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Board.Tests
{
    public class SpriteGroupAlphaAnimationTests
    {
        [Test]
        public void ApplyAlpha_ScalesEachRendererFromItsOwnAuthorAlpha()
        {
            var a = new GameObject("a").AddComponent<SpriteRenderer>();
            var b = new GameObject("b").AddComponent<SpriteRenderer>();
            try
            {
                var rs = new[] { a, b };
                var baseAlpha = new[] { 1f, 0.5f };

                SpriteGroupAlphaAnimation.ApplyAlpha(rs, baseAlpha, 0.5f);
                Assert.AreEqual(0.5f, a.color.a, 1e-5f);
                Assert.AreEqual(0.25f, b.color.a, 1e-5f);

                SpriteGroupAlphaAnimation.ApplyAlpha(rs, baseAlpha, 1f);   // về lại author
                Assert.AreEqual(1f, a.color.a, 1e-5f);
                Assert.AreEqual(0.5f, b.color.a, 1e-5f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a.gameObject);
                UnityEngine.Object.DestroyImmediate(b.gameObject);
            }
        }
    }
}
```

- [ ] **Step 3: Chạy test, xác nhận fail**

Unity: Test Runner ▸ EditMode ▸ Run All.
Expected: không compile — `The type or namespace name 'SpriteGroupAlphaAnimation' could not be found`.

- [ ] **Step 4: Viết component**

```csharp
// Figma cho opacity theo cả group (Group 547 mờ về 0 cuối timeline), SpriteRenderer thì không có
// alpha theo nhóm. Component này nhân một hệ số lên alpha author của mọi SpriteRenderer dưới target.
// Dùng như component có sẵn của LitMotion.Animation: Start/End Value là hệ số (1 = author, 0 = tắt),
// để Relative tắt.
using System;
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

namespace WordStack.Board
{
    [Serializable]
    [LitMotionAnimationComponentMenu("Custom/Sprite Group Alpha")]
    public sealed class SpriteGroupAlphaAnimation : FloatPropertyAnimationComponent<Transform>
    {
        SpriteRenderer[] renderers;
        float[] baseAlpha;
        float current = 1f;

        // Chụp lại alpha author mỗi lần Play: giữa hai lần mở, bên ngoài có thể đã đổi alpha
        // (BoxView mờ Key Tile trước khi gọi Play).
        public override MotionHandle Play()
        {
            renderers = null;
            current = 1f;
            return base.Play();
        }

        protected override float GetValue(Transform target)
        {
            Cache(target);
            return current;
        }

        protected override void SetValue(Transform target, in float value)
        {
            Cache(target);
            current = value;
            ApplyAlpha(renderers, baseAlpha, value);
        }

        void Cache(Transform target)
        {
            if (renderers != null || target == null) return;
            renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            baseAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseAlpha[i] = renderers[i].color.a;
        }

        /// <summary>alpha = alpha author × k cho từng renderer. Static để test không cần chạy motion.</summary>
        public static void ApplyAlpha(SpriteRenderer[] rs, float[] baseAlpha, float k)
        {
            if (rs == null) return;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                var c = rs[i].color;
                c.a = baseAlpha[i] * k;
                rs[i].color = c;
            }
        }
    }
}
```

- [ ] **Step 5: Chạy test, xác nhận pass**

Unity: Test Runner ▸ EditMode ▸ Run All. Expected: `ApplyAlpha_ScalesEachRendererFromItsOwnAuthorAlpha` xanh, `FigmaEaseTests` vẫn xanh.
Run: `./compilecheck.sh` → 3 dòng `OK`.
Unity: trên một GameObject bất kỳ có `LitMotionAnimation`, nút *Add Component* của package liệt kê *Custom ▸ Sprite Group Alpha*.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Board/Views/SpriteGroupAlphaAnimation.cs Assets/_Game/Board/Views/SpriteGroupAlphaAnimation.cs.meta \
  Assets/_Game/Board/Tests/SpriteGroupAlphaAnimationTests.cs Assets/_Game/Board/Tests/SpriteGroupAlphaAnimationTests.cs.meta \
  Assets/_Game/Board/Tests/WordStack.Board.Tests.asmdef
git commit -m "feat(anim): SpriteGroupAlphaAnimation fades a whole sprite group

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 3: Tool `Build Lock Open Animation` + tách `Middle`

**Files:**
- Create: `Assets/_Game/Board/Editor/LockOpenAnimationBuilder.cs`
- Delete: `Assets/_Game/Board/Editor/LockShutterAnimationBuilder.cs` (+ `.meta`)
- Modify (trong Unity): `Assets/_Game/Art/Blocker/Blocker_Box/Blocker_Box_Middle.png.meta`

**Interfaces:**
- Consumes: `FigmaEase.EaseOut / CssEaseOut01 / Linear` (Task 1), `SpriteGroupAlphaAnimation` (Task 2).
- Produces: menu `Tools/WordStack/Build Lock Open Animation`; trên `Lock Root` một `LitMotionAnimation` 15 component (8 cửa chớp + 4 Middle + 1 Upper + 2 Lock Root) và hai con mới `Middle/Top`, `Middle/Bottom`.
- Tên object tool tra (đúng hierarchy hiện tại của `Box.prefab`): con tên bắt đầu `Lit` (không phải `UpperLit`) = tấm nền; con tên bắt đầu `UpperLit` = lớp nan (ghép với tấm nền gần nhất theo x); `Upper`; `Middle`.

- [ ] **Step 1: Cắt `Blocker_Box_Middle.png` thành 2 sprite**

Figma tách `Middle` thành hai mảnh chạy riêng (Group 558 thanh trên, Group 557 thanh dưới); texture hiện gộp cả hai.
Unity: chọn `Assets/_Game/Art/Blocker/Blocker_Box/Blocker_Box_Middle.png` ▸ Inspector *Sprite Mode = Multiple* ▸ Apply ▸ *Open Sprite Editor* ▸ *Slice ▸ Type Automatic, Pivot Center* ▸ Slice ▸ Apply.

Run: `grep -c "name: Blocker_Box_Middle_" Assets/_Game/Art/Blocker/Blocker_Box/Blocker_Box_Middle.png.meta`
Expected: `2`. Khác 2 (bóng đổ dính liền hay tách vụn) → cắt tay trong Sprite Editor thành đúng hai rect trên/dưới, pivot Center.

- [ ] **Step 2: Viết tool**

```csharp
// Dựng LitMotionAnimation "mở hộp khoá nhóm" trên Lock Root từ timeline Figma
// (Logos-Project › Group 547, node 373:1245 — plan docs/superpowers/plans/2026-09-27-lock-box-open-animation.md).
// Mở Box.prefab, chọn Lock Root, Tools ▸ WordStack ▸ Build Lock Open Animation, Save prefab. Sau đó
// chỉnh + preview trong Inspector của LitMotionAnimation.
//
// Tool làm, đều Undo được: gỡ script mất; tách UpperLit ra ngang hàng Lit (Figma để hai lớp là anh
// em); tách Middle thành Top/Bottom (texture đã cắt 2 sprite); ghi đè LitMotionAnimation (Parallel,
// không tự chạy). Chạy lại = dựng lại từ số Figma, chỉnh tay trước đó mất.
using System.Collections.Generic;
using System.Linq;
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using UnityEditor;
using UnityEngine;

namespace WordStack.Board.Editor
{
    static class LockOpenAnimationBuilder
    {
        const float PxToUnit = 0.005f;   // 1 px Figma trong Lock Root: art 2x, PPU 400
        const string MiddleTexture = "Assets/_Game/Art/Blocker/Blocker_Box/Blocker_Box_Middle.png";

        // Figma (giây = % × 2 s)
        const float ShutterEnd = 0.458f, BackHold = 0.0577f, UpperHold = 0.063f;
        const float BarsAt = 0.459f, UpperDropAt = 0.46f, FadeAt = 0.49f, ShrinkAt = 0.492f, Snap = 0.030f;

        enum Kind { Position, Scale, GroupAlpha }

        struct Track
        {
            public string name; public Transform target; public Kind kind;
            public Vector3 end; public float delay, duration; public AnimationCurve ease;
        }

        [MenuItem("Tools/WordStack/Build Lock Open Animation")]
        static void Build()
        {
            var root = Selection.activeGameObject;
            if (root == null) { Debug.LogWarning("[LockOpen] Chọn Lock Root (trong Box.prefab) trước."); return; }

            Undo.SetCurrentGroupName("Build Lock Open Animation");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(root, "Remove missing scripts");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

            var rt = root.transform;
            var tracks = new List<Track>();
            if (!AddShutters(rt, tracks)) return;

            var middle = rt.Find("Middle");
            if (middle == null) { Debug.LogWarning("[LockOpen] Không thấy con \"Middle\".", root); return; }
            if (!EnsureMiddleSplit(middle, out var top, out var bottom)) return;
            foreach (var bar in new[] { top, bottom })
            {
                tracks.Add(T("Middle " + bar.name + " · Y", bar, Kind.Position, new Vector3(0f, -5f * PxToUnit, 0f), BarsAt, Snap));
                tracks.Add(T("Middle " + bar.name + " · scaleY", bar, Kind.Scale, new Vector3(0f, -bar.localScale.y, 0f), BarsAt, Snap));
            }

            var upper = rt.Find("Upper");
            if (upper == null) { Debug.LogWarning("[LockOpen] Không thấy con \"Upper\".", root); return; }
            tracks.Add(T("Upper · Y", upper, Kind.Position, new Vector3(0f, -20f * PxToUnit, 0f), UpperDropAt, Snap));

            tracks.Add(T("Lock Root · alpha", rt, Kind.GroupAlpha, Vector3.zero, FadeAt, Snap));
            var s = rt.localScale;
            tracks.Add(T("Lock Root · scale 0.9", rt, Kind.Scale, new Vector3(-0.1f * s.x, -0.1f * s.y, 0f), ShrinkAt, Snap));

            Write(root, tracks);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[LockOpen] {tracks.Count} component trên {root.name}. Nhớ Save prefab.", root);
        }

        static Track T(string name, Transform target, Kind kind, Vector3 end, float delay, float duration, AnimationCurve ease = null)
        {
            return new Track { name = name, target = target, kind = kind, end = end, delay = delay, duration = duration,
                               ease = ease ?? FigmaEase.CssEaseOut01() };
        }

        // Tấm nền = con tên bắt đầu "Lit"; lớp nan = "UpperLit" (con của nền thì tách ra cùng cha, giữ
        // vị trí world), ghép với nền gần nhất theo x. Tấm bên phải tâm lật dấu X (gập về mép phải).
        static bool AddShutters(Transform root, List<Track> tracks)
        {
            var backs = new List<Transform>();
            var uppers = new List<Transform>();
            foreach (Transform c in root)
            {
                if (c.name.StartsWith("UpperLit")) { uppers.Add(c); continue; }
                if (!c.name.StartsWith("Lit")) continue;
                backs.Add(c);
                foreach (Transform g in c) if (g.name.StartsWith("UpperLit")) uppers.Add(g);
            }
            foreach (var u in uppers)
                if (u.parent != root) Undo.SetTransformParent(u, root, "Flatten shutter slats");
            if (backs.Count == 0 || backs.Count != uppers.Count)
            {
                Debug.LogWarning($"[LockOpen] Cần số Lit bằng số UpperLit, đang {backs.Count}/{uppers.Count}.", root);
                return false;
            }

            // 8 mẫu Figma của lớp nan (giây, px), chuẩn hoá theo 0.458 s và 39 px.
            var upperSamples = new[] { (0f, 0f), (0.1f, 18.268f), (0.2f, 30.007f), (0.245f, 31.976f),
                                       (0.25f, 32.275f), (0.3f, 34.725f), (0.4f, 38.237f), (0.458f, 39f) }
                .Select(p => (p.Item1 / ShutterEnd, p.Item2 / 39f)).ToArray();

            foreach (var b in backs)
            {
                var u = uppers.OrderBy(x => Mathf.Abs(x.localPosition.x - b.localPosition.x)).First();
                uppers.Remove(u);
                float dir = b.localPosition.x > 0f ? -1f : 1f;
                tracks.Add(T(b.name + " · nền X", b, Kind.Position, new Vector3(dir * -37f * PxToUnit, 0f, 0f), 0f, ShutterEnd,
                             FigmaEase.EaseOut((0f, 0f), (0.5f, 30f / 37f), (1f, 1f))));
                tracks.Add(T(b.name + " · nền scaleX", b, Kind.Scale, new Vector3(-b.localScale.x, 0f, 0f), BackHold, ShutterEnd - BackHold));
                tracks.Add(T(b.name + " · nan X", u, Kind.Position, new Vector3(dir * -39f * PxToUnit, 0f, 0f), 0f, ShutterEnd,
                             FigmaEase.Linear(upperSamples)));
                tracks.Add(T(b.name + " · nan scaleX", u, Kind.Scale, new Vector3(-u.localScale.x, 0f, 0f), UpperHold, ShutterEnd - UpperHold));
            }
            return true;
        }

        // Middle cũ là một sprite Single pivot Center; sau khi cắt 2 sprite, đặt hai con Top/Bottom đúng
        // chỗ cũ: tâm rect so với tâm texture, chia PPU. Renderer của Middle tắt đi.
        static bool EnsureMiddleSplit(Transform middle, out Transform top, out Transform bottom)
        {
            top = middle.Find("Top");
            bottom = middle.Find("Bottom");
            if (top != null && bottom != null) return true;

            var sprites = AssetDatabase.LoadAllAssetsAtPath(MiddleTexture).OfType<Sprite>()
                                       .OrderByDescending(x => x.rect.y).ToArray();
            if (sprites.Length != 2)
            {
                Debug.LogWarning($"[LockOpen] {MiddleTexture} cần cắt đúng 2 sprite (Sprite Editor ▸ Slice Automatic, pivot Center), đang có {sprites.Length}.");
                return false;
            }
            var src = middle.GetComponent<SpriteRenderer>();
            top = MakeBar(middle, "Top", sprites[0], src);
            bottom = MakeBar(middle, "Bottom", sprites[1], src);
            if (src != null) { Undo.RecordObject(src, "Split Middle"); src.enabled = false; }
            return true;
        }

        static Transform MakeBar(Transform middle, string name, Sprite sprite, SpriteRenderer src)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Split Middle");
            go.transform.SetParent(middle, false);
            var tex = sprite.texture;
            var c = sprite.rect.center;
            go.transform.localPosition = new Vector3((c.x - tex.width * 0.5f) / sprite.pixelsPerUnit,
                                                     (c.y - tex.height * 0.5f) / sprite.pixelsPerUnit, 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            if (src != null)
            {
                r.sortingLayerID = src.sortingLayerID;
                r.sortingOrder = src.sortingOrder;
                r.color = src.color;
                r.sharedMaterial = src.sharedMaterial;
                r.maskInteraction = src.maskInteraction;
            }
            return go.transform;
        }

        static void Write(GameObject root, List<Track> tracks)
        {
            var anim = root.GetComponent<LitMotionAnimation>();
            if (anim == null) anim = Undo.AddComponent<LitMotionAnimation>(root);
            Undo.RecordObject(anim, "Build Lock Open Animation");

            var so = new SerializedObject(anim);
            so.FindProperty("autoPlayMode").enumValueIndex = 0;    // None — BoxView.OpenGroupLock tự gọi Play
            so.FindProperty("animationMode").enumValueIndex = 0;   // Parallel
            var arr = so.FindProperty("components");
            arr.arraySize = tracks.Count;
            for (int i = 0; i < tracks.Count; i++)
            {
                LitMotionAnimationComponent c = tracks[i].kind == Kind.Position ? new TransformPositionAnimation()
                                              : tracks[i].kind == Kind.Scale ? new TransformScaleAnimation()
                                              : new SpriteGroupAlphaAnimation();
                arr.GetArrayElementAtIndex(i).managedReferenceValue = c;
            }
            so.ApplyModifiedProperties();   // phải có instance rồi mới có property con để ghi
            so.Update();
            for (int i = 0; i < tracks.Count; i++) Fill(arr.GetArrayElementAtIndex(i), tracks[i]);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(root);
        }

        static void Fill(SerializedProperty el, Track t)
        {
            el.FindPropertyRelative("displayName").stringValue = t.name;
            el.FindPropertyRelative("target").objectReferenceValue = t.target;
            var s = el.FindPropertyRelative("settings");
            if (t.kind == Kind.GroupAlpha)
            {
                el.FindPropertyRelative("relative").boolValue = false;
                s.FindPropertyRelative("startValue").floatValue = 1f;
                s.FindPropertyRelative("endValue").floatValue = 0f;
            }
            else
            {
                el.FindPropertyRelative("relative").boolValue = true;   // cộng lên vị trí/scale author lúc Play
                if (t.kind == Kind.Position) el.FindPropertyRelative("useWorldSpace").boolValue = false;
                s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                s.FindPropertyRelative("endValue").vector3Value = t.end;
            }
            s.FindPropertyRelative("duration").floatValue = t.duration;
            s.FindPropertyRelative("delay").floatValue = t.delay;
            s.FindPropertyRelative("ease").intValue = (int)Ease.CustomAnimationCurve;
            s.FindPropertyRelative("customEaseCurve").animationCurveValue = t.ease;
        }
    }
}
```

- [ ] **Step 3: Gỡ tool cũ**

```bash
git rm Assets/_Game/Board/Editor/LockShutterAnimationBuilder.cs Assets/_Game/Board/Editor/LockShutterAnimationBuilder.cs.meta
```

- [ ] **Step 4: Kiểm compile**

Run: `./compilecheck.sh`
Expected: 3 dòng `OK`. Unity: menu *Tools ▸ WordStack* chỉ còn *Build Lock Open Animation*.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Board/Editor/LockOpenAnimationBuilder.cs Assets/_Game/Board/Editor/LockOpenAnimationBuilder.cs.meta \
  Assets/_Game/Art/Blocker/Blocker_Box/Blocker_Box_Middle.png.meta
git commit -m "feat(blocker): tool builds the full Figma lock-open timeline on Lock Root

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 4: `BoxView.OpenGroupLock` chỉ chạy timeline Figma

Figma giờ tự kết thúc bằng mờ + co cả khối, nên bước "lồng trượt lên + mờ" cũ (lấy từ video tham khảo) bỏ đi. Bước Key Tile mờ trước giữ nguyên (user yêu cầu).

**Files:**
- Modify: `Assets/_Game/Board/Views/BoxView.cs`

**Interfaces:**
- Consumes: `LitMotionAnimation` trên `groupLockRoot` (Task 3).
- Produces: `public IEnumerator OpenGroupLock()` — chữ ký không đổi, `BoardController.ClearIntoLock` gọi như cũ.

- [ ] **Step 1: Bỏ field lồng trượt và scale icon**

Xoá 4 dòng trong header *"Mở group lock sau khi nhóm bay vào icon"*:

```csharp
        [Tooltip("Lồng trượt lên bấy nhiêu world unit trong lúc mờ dần")]
        [SerializeField] float groupOpenLift = 0.25f;
        [SerializeField] float groupOpenDur = 0.35f;
        [SerializeField] Ease groupOpenEase = Ease.OutCubic;
```

Xoá dòng khai báo `Vector3 groupArtScale = Vector3.one;` và dòng trong `Awake`:

```csharp
            if (groupArt != null) groupArtScale = groupArt.transform.localScale;
```

Đổi tên field `shutter` → `openAnim`: dòng khai báo thành

```csharp
        LitMotionAnimation openAnim;   // timeline mở trên groupLockRoot (Tools ▸ WordStack ▸ Build Lock Open Animation), null = không có
```

và dòng trong `Awake` thành

```csharp
            if (groupLockRoot != null) openAnim = groupLockRoot.GetComponent<LitMotionAnimation>();
```

- [ ] **Step 2: Thay toàn bộ comment + thân `OpenGroupLock`**

```csharp
        // Mở group lock: Key Tile mờ dần → LitMotionAnimation trên Lock Root chạy trọn timeline Figma
        // (cửa chớp gập, thanh Middle ép dẹt, Upper rơi, cả khối mờ + co 0.9) → tắt root, trả alpha/scale
        // về author để lần bind sau còn dùng. Đặt shownRoot = null ngay: SetOpen ở cuối Settle sẽ snap,
        // không Unlock lần hai. Coroutine (bên gọi yield return) vì LitMotionAnimation không cho biết
        // trước thời lượng — chờ IsPlaying.
        public IEnumerator OpenGroupLock()
        {
            var root = groupLockRoot;
            lockKnown = true; shownRoot = null;
            if (root == null || !root.activeSelf) yield break;

            FinishOpen();
            int token = ++openToken;
            var tr = root.transform;
            var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
            var a0 = new float[srs.Length];
            for (int i = 0; i < srs.Length; i++) a0[i] = srs[i].color.a;
            openRestore = () =>
            {
                if (openAnim != null) openAnim.Stop();   // OnStop từng component trả vị trí/scale/alpha lúc Play
                root.SetActive(false);
                tr.localScale = groupScale;
                for (int i = 0; i < srs.Length; i++) { var c = srs[i].color; c.a = a0[i]; srs[i].color = c; }
            };
            tr.DOKill(true);

            // Key Tile mờ hẳn trước, rồi timeline Figma mới chạy.
            var tile = groupTile != null ? groupTile : (groupArt != null ? groupArt.transform.parent : null);
            if (tile == tr && groupArt != null) tile = groupArt.transform;   // icon gắn thẳng lên root: chỉ mờ icon
            if (tile != null && groupTileFadeDur > 0f)
            {
                var fade = DOTween.Sequence().SetLink(root);
                foreach (var sr in tile.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var r = sr;
                    fade.Join(DOTween.ToAlpha(() => r.color, c => r.color = c, 0f, groupTileFadeDur).SetEase(Ease.OutQuad));
                }
                openTween = fade;
                yield return fade.WaitForCompletion();
                if (token != openToken) yield break;
            }

            if (openAnim != null)
            {
                openAnim.Stop();
                openAnim.Play();
                while (openAnim.IsPlaying)
                {
                    yield return null;
                    if (token != openToken) yield break;
                }
            }
            FinishOpen();
        }
```

`FinishOpen`, `ShowRoots` giữ nguyên.

- [ ] **Step 3: Kiểm compile**

Run: `./compilecheck.sh`
Expected: 3 dòng `OK`.
Run: `grep -n "groupOpenLift\|groupOpenDur\|groupOpenEase\|groupArtScale\|shutter" Assets/_Game/Board/Views/BoxView.cs`
Expected: không in dòng nào.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Board/Views/BoxView.cs
git commit -m "refactor(blocker): group lock open = Key Tile fade + Figma timeline only

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 5: Dựng lên `Box.prefab` và nghiệm thu bằng mắt

**Files:**
- Modify (trong Unity): `Assets/Prefabs/Box.prefab`

- [ ] **Step 1: Chạy tool**

Unity: mở `Assets/Prefabs/Box.prefab` ▸ chọn `Lock Root` ▸ *Tools ▸ WordStack ▸ Build Lock Open Animation* ▸ Save.
Expected Console: `[LockOpen] 15 component trên Lock Root. Nhớ Save prefab.` Hierarchy: `Middle` có hai con `Top`, `Bottom` trùng khít hình cũ, renderer của `Middle` tắt.

- [ ] **Step 2: Kiểm số trong prefab**

Run: `grep -c "displayName:" Assets/Prefabs/Box.prefab`
Expected: `15`.
Run: `grep -n "autoPlayMode\|animationMode" Assets/Prefabs/Box.prefab`
Expected: `autoPlayMode: 0` và `animationMode: 0`.

- [ ] **Step 3: Preview trong Edit mode, so với Figma**

Inspector `LitMotionAnimation` ▸ nút Play của package. Đối chiếu (bật Figma node `373:1245` ở chế độ timeline cạnh bên):
1. 0 → 0.458 s: hai tấm cửa gập — `Lit` về mép trái, `Lit (1)` về mép phải; lớp nan gập cùng lúc.
2. 0.459 → 0.489 s: `Middle/Top` và `Middle/Bottom` dẹt về 0 theo chiều dọc, lệch xuống một chút.
3. 0.46 → 0.49 s: `Upper` rơi xuống 0.1 unit.
4. 0.49 → 0.522 s: cả khối mờ về 0 và co còn 0.9.
Bấm Stop: mọi thứ về đúng vị trí/scale/alpha author.

- [ ] **Step 4: Nghiệm thu trong màn chơi**

Play một level có hộp khoá nhóm, gom sạch nhóm yêu cầu. Expected: 4 thẻ bay vào icon → Key Tile mờ → timeline ở Step 3 → hộp mở, thẻ bên trong nhấc được. Undo/Load màn sau đó hiện lại hộp khoá đầy đủ (không kẹt alpha 0 hay scale 0).

- [ ] **Step 5: Commit**

```bash
git add Assets/Prefabs/Box.prefab
git commit -m "feat(blocker): Box.prefab Lock Root plays the Figma lock-open timeline

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

`Box.prefab` đang chứa cả thay đổi art user làm trước đó (Lit/UpperLit/Upper/Middle mới) — commit này gom luôn, đó là chủ đích.
