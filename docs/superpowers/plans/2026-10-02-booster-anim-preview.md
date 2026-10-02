# Booster Anim Preview Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xem trước hiệu ứng booster trong Editor: nền booster và nút booster HUD dùng component `LitMotionAnimation`; hiệu ứng thẻ xem trước bằng nút trong Inspector của `SO_BoosterAnim`.

**Architecture:** Nền + nút: component `LitMotionAnimation` do menu Editor dựng (mẫu `FixedTileAnimationBuilder`), code chỉ `Stop()`/`Play()`. Thẻ: hàm thuần `BoosterTilePreview` tính scale/góc theo thời gian bằng `EaseUtility.Evaluate`, Custom Editor gọi mỗi `EditorApplication.update` (scheduler mặc định của LitMotion không chạy ngoài Play mode).

**Tech Stack:** Unity 6000.3.8f1, C#, LitMotion 2.0.2 + LitMotion.Animation 2.0.2, NUnit EditMode.

**Spec:** `docs/superpowers/specs/2026-10-02-booster-anim-preview-design.md`. Thực hiện trực tiếp trong phiên (không subagent) — batch nhỏ, user đã duyệt L1.

## Global Constraints

- `LitMotionAnimation.Stop()` trả giá trị về lúc trước `Play()`; `Play()` khi đang chạy là chạy tiếp → luôn `Stop()` rồi `Play()`.
- Không fade-in Stop khi xong (alpha về 0); trước fade-out mới Stop fade-in rồi đặt alpha = 1 cùng frame.
- Nền: CanvasGroup alpha 0→1 / 1→0, 0.15 s, OutQuad. Nút HUD: Scale (Punch) relative, end (0.2,0.2,0.2), 0.2 s, Frequency 10, DampingRatio 1.9.
- Không sửa `.prefab` / `.unity` trên đĩa — menu Editor ghi, user chạy trong Unity.
- `WordStack.Meta.Editor` (thế giới netstandard) không tham chiếu `WordStack.Board` → không dùng `AnimationBuildKit`.
- Commit kết thúc bằng `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; `git add` đường dẫn cụ thể.

**Lệnh kiểm:** `./compilecheck.sh` rồi `bash .git/sdd/testcheck.sh`. Test EditMode chỉ chạy được trong Unity (Task 4).

---

### Task 1: Preview hiệu ứng thẻ trong Inspector `SO_BoosterAnim`

**Files:**
- Create: `Assets/_Game/Board/Editor/BoosterTilePreview.cs`
- Create: `Assets/_Game/Board/Editor/BoosterAnimSettingsEditor.cs`
- Test: `Assets/_Game/Board/Tests/BoosterTilePreviewTests.cs`

**Interfaces:**
- Produces: `BoosterTilePreview.MagnetTile/ParentTile/UndoTile/RevealTile(BoosterAnimSettings) → Step[]`, `Sample(Step[], float t, out float scale, out float spin) → bool`, `TotalDuration(Step[])`.

- [ ] **Step 1: Viết test**

```csharp
// Chuỗi xem trước hiệu ứng thẻ booster (BoosterTilePreview) — giá trị đầu / cuối từng nhịp phải khớp
// đúng thông số trong SO_BoosterAnim.
using NUnit.Framework;
using UnityEngine;
using WordStack.Board.Editor;

namespace WordStack.Board.Tests
{
    public class BoosterTilePreviewTests
    {
        static BoosterAnimSettings NewSettings()
        {
            var a = ScriptableObject.CreateInstance<BoosterAnimSettings>();
            a.magnetPopDur = 0.1f; a.magnetPopScale = 1.2f;
            a.magnetFlyDur = 0.4f; a.magnetGatherScale = 1.6f;
            a.magnetHold = 0.1f; a.magnetBurstDur = 0.2f;
            a.magnetSpin = 90f;
            return a;
        }

        static float ScaleAt(BoosterTilePreview.Step[] steps, float t)
        {
            BoosterTilePreview.Sample(steps, t, out float scale, out _);
            return scale;
        }

        [Test]
        public void MagnetTile_PopsGrowsHoldsThenBurstsToZero()
        {
            var a = NewSettings();
            var steps = BoosterTilePreview.MagnetTile(a);

            Assert.AreEqual(0.8f, BoosterTilePreview.TotalDuration(steps), 1e-4f);
            Assert.AreEqual(1f, ScaleAt(steps, 0f), 1e-4f, "bắt đầu ở cỡ gốc");
            Assert.AreEqual(1.2f, ScaleAt(steps, 0.1f), 1e-4f, "hết nhịp phồng = magnetPopScale");
            Assert.AreEqual(1.6f, ScaleAt(steps, 0.55f), 1e-4f, "đang đứng ở điểm hội tụ = magnetGatherScale");

            Assert.IsFalse(BoosterTilePreview.Sample(steps, 1f, out float end, out float spin), "qua hết chuỗi → false");
            Assert.AreEqual(0f, end, 1e-4f, "nổ về 0");
            Assert.AreEqual(90f, spin, 1e-4f, "xoay tới magnetSpin và giữ nguyên");
            Object.DestroyImmediate(a);
        }

        [Test]
        public void MagnetTile_SpinOffWhenTiny()
        {
            var a = NewSettings();
            a.magnetSpin = 0.005f;
            BoosterTilePreview.Sample(BoosterTilePreview.MagnetTile(a), 0.3f, out _, out float spin);
            Assert.AreEqual(0f, spin, 1e-6f, "|magnetSpin| ≤ 0.01 là tắt, như BoardController");
            Object.DestroyImmediate(a);
        }

        [Test]
        public void ParentUndoReveal_EndAtNormalSize()
        {
            var a = NewSettings();
            foreach (var steps in new[]
                     { BoosterTilePreview.ParentTile(a), BoosterTilePreview.UndoTile(a), BoosterTilePreview.RevealTile(a) })
            {
                Assert.IsFalse(BoosterTilePreview.Sample(steps, 10f, out float scale, out _));
                Assert.AreEqual(1f, scale, 1e-4f, "kết thúc về cỡ gốc");
            }
            Assert.AreEqual(0f, ScaleAt(BoosterTilePreview.ParentTile(a), 0f), 1e-4f, "thẻ cha nở từ 0");
            Assert.AreEqual(0f, ScaleAt(BoosterTilePreview.RevealTile(a), 0f), 1e-4f, "thẻ hiện ra từ 0");
            Object.DestroyImmediate(a);
        }
    }
}
```

- [ ] **Step 2: Compile — phải đỏ** (`BoosterTilePreview` chưa có). Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `game.dll OK`, `editor.dll OK`, `meta.dll OK`, `board-tests.dll OK`, `meta-tests.dll OK`.

- [ ] **Step 3: Hàm thuần**

```csharp
// Chuỗi scale/góc của các hiệu ứng thẻ booster để xem trước trong Inspector của SO_BoosterAnim
// (spec docs/superpowers/specs/2026-10-02-booster-anim-preview-design.md Mục 5). Không tạo motion —
// scheduler mặc định của LitMotion không chạy ngoài Play mode — chỉ tính giá trị theo thời gian.
//
// Ease cố định (OutQuad, OutBack) lặp lại từ BoardController.MagnetAnimation / AppendParentFlight /
// UndoAnimation: sửa bên đó thì sửa cả ở đây.
using LitMotion;
using UnityEngine;

namespace WordStack.Board.Editor
{
    public static class BoosterTilePreview
    {
        /// <summary>Một nhịp: scale (bội của cỡ gốc) và góc z (độ, cộng vào góc gốc) đi từ From tới To.</summary>
        public struct Step
        {
            public float Duration;
            public float ScaleFrom, ScaleTo;
            public Ease ScaleEase;
            public float SpinFrom, SpinTo;
            public Ease SpinEase;
        }

        public static Step[] MagnetTile(BoosterAnimSettings a)
        {
            float spin = Mathf.Abs(a.magnetSpin) > 0.01f ? a.magnetSpin : 0f;   // như BoardController: |spin| ≤ 0.01 = tắt
            return new[]
            {
                Scale(a.magnetPopDur, 1f, a.magnetPopScale, Ease.OutQuad),
                new Step
                {
                    Duration = a.magnetFlyDur,
                    ScaleFrom = a.magnetPopScale, ScaleTo = a.magnetGatherScale, ScaleEase = Ease.OutQuad,
                    SpinFrom = 0f, SpinTo = spin, SpinEase = a.magnetFlyEase,
                },
                Hold(a.magnetHold, a.magnetGatherScale, spin),
                new Step
                {
                    Duration = a.magnetBurstDur,
                    ScaleFrom = a.magnetGatherScale, ScaleTo = 0f, ScaleEase = a.magnetBurstEase,
                    SpinFrom = spin, SpinTo = spin,
                },
            };
        }

        public static Step[] ParentTile(BoosterAnimSettings a)
        {
            return new[]
            {
                Scale(a.magnetParentBloomDur, 0f, a.magnetGatherScale, Ease.OutBack),
                Hold(a.magnetParentHold, a.magnetGatherScale, 0f),
                Scale(a.magnetParentFlyDur, a.magnetGatherScale, 1f, Ease.OutQuad),
            };
        }

        public static Step[] UndoTile(BoosterAnimSettings a)
        {
            return new[]
            {
                Scale(a.undoPopDur, 1f, a.undoPopScale, Ease.OutQuad),
                Scale(a.undoFlyDur, a.undoPopScale, 1f, Ease.OutQuad),
            };
        }

        public static Step[] RevealTile(BoosterAnimSettings a)
        {
            return new[] { Scale(a.magnetRevealDur, 0f, 1f, Ease.OutBack) };
        }

        public static float TotalDuration(Step[] steps)
        {
            float t = 0f;
            foreach (var s in steps) t += Mathf.Max(0f, s.Duration);
            return t;
        }

        /// <summary>
        /// Giá trị tại thời điểm t (giây từ lúc bắt đầu). Trả false khi đã qua hết chuỗi — lúc đó scale/spin là
        /// giá trị cuối của nhịp cuối.
        /// </summary>
        public static bool Sample(Step[] steps, float t, out float scale, out float spin)
        {
            scale = 1f; spin = 0f;
            if (steps == null || steps.Length == 0) return false;
            float start = 0f;
            foreach (var s in steps)
            {
                float d = Mathf.Max(0f, s.Duration);
                if (t < start + d)
                {
                    float k = d <= 0f ? 1f : Mathf.Clamp01((t - start) / d);
                    scale = Mathf.LerpUnclamped(s.ScaleFrom, s.ScaleTo, EaseUtility.Evaluate(k, s.ScaleEase));
                    spin = Mathf.LerpUnclamped(s.SpinFrom, s.SpinTo, EaseUtility.Evaluate(k, s.SpinEase));
                    return true;
                }
                start += d;
            }
            var last = steps[steps.Length - 1];
            scale = last.ScaleTo; spin = last.SpinTo;
            return false;
        }

        static Step Scale(float dur, float from, float to, Ease ease)
        {
            return new Step { Duration = dur, ScaleFrom = from, ScaleTo = to, ScaleEase = ease };
        }

        static Step Hold(float dur, float scale, float spin)
        {
            return new Step { Duration = dur, ScaleFrom = scale, ScaleTo = scale, SpinFrom = spin, SpinTo = spin };
        }
    }
}
```

- [ ] **Step 4: Custom Editor**

```csharp
// Inspector của SO_BoosterAnim: mọi field như cũ + khối Preview diễn thử hiệu ứng thẻ booster lên đối tượng
// đang chọn ngay trong Edit mode (spec docs/superpowers/specs/2026-10-02-booster-anim-preview-design.md Mục 5).
// Chỉ đổi scale/góc tạm thời: xong hoặc Stop là trả về như cũ, không ghi gì vào scene/prefab.
using UnityEditor;
using UnityEngine;

namespace WordStack.Board.Editor
{
    [CustomEditor(typeof(BoosterAnimSettings))]
    public class BoosterAnimSettingsEditor : UnityEditor.Editor
    {
        Transform previewTarget;
        Vector3 baseScale, baseEuler;
        BoosterTilePreview.Step[] steps;
        double startTime;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview hiệu ứng thẻ", EditorStyles.boldLabel);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Preview chỉ chạy ở Edit mode.", MessageType.Info);
                return;
            }
            var sel = Selection.activeTransform;
            if (sel == null && previewTarget == null)
            {
                EditorGUILayout.HelpBox("Chọn một thẻ trong scene (hoặc mở Tile.prefab) rồi bấm nút để xem. " +
                                        "Cỡ hiện tại của thẻ được coi là 1.", MessageType.Info);
                return;
            }

            var a = (BoosterAnimSettings)target;
            using (new EditorGUI.DisabledScope(previewTarget != null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Thẻ nam châm")) Begin(sel, BoosterTilePreview.MagnetTile(a));
                    if (GUILayout.Button("Thẻ cha")) Begin(sel, BoosterTilePreview.ParentTile(a));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Thẻ undo")) Begin(sel, BoosterTilePreview.UndoTile(a));
                    if (GUILayout.Button("Thẻ hiện ra")) Begin(sel, BoosterTilePreview.RevealTile(a));
                }
            }
            using (new EditorGUI.DisabledScope(previewTarget == null))
                if (GUILayout.Button("Stop")) End();
        }

        void OnDisable() { End(); }

        void Begin(Transform t, BoosterTilePreview.Step[] s)
        {
            if (t == null) return;
            End();
            previewTarget = t;
            baseScale = t.localScale;
            baseEuler = t.localEulerAngles;
            steps = s;
            startTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        void Tick()
        {
            if (previewTarget == null) { End(); return; }
            bool running = BoosterTilePreview.Sample(steps, (float)(EditorApplication.timeSinceStartup - startTime),
                                                     out float scale, out float spin);
            previewTarget.localScale = baseScale * scale;
            previewTarget.localEulerAngles = baseEuler + new Vector3(0f, 0f, spin);
            SceneView.RepaintAll();
            if (!running) End();
        }

        // Trả scale/góc gốc. Gọi lại nhiều lần vô hại.
        void End()
        {
            EditorApplication.update -= Tick;
            if (previewTarget != null)
            {
                previewTarget.localScale = baseScale;
                previewTarget.localEulerAngles = baseEuler;
                SceneView.RepaintAll();
            }
            previewTarget = null;
            steps = null;
            Repaint();
        }
    }
}
```

- [ ] **Step 5: Compile — xanh.** Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `game.dll OK`, `editor.dll OK`, `meta.dll OK`, `board-tests.dll OK`, `meta-tests.dll OK`.

- [ ] **Step 6: Commit** — `git add` 3 file trên, message `Booster anim: preview tile effects in the SO_BoosterAnim inspector`.

---

### Task 2: Nền booster mờ vào/ra bằng component

**Files:**
- Modify: `Assets/_Game/Board/Views/BoardController.cs` (field cạnh `boosterBackdrop`, `Backdrop()`)
- Modify: `Assets/_Game/Board/Views/BoosterAnimSettings.cs` (bỏ `backdropFadeIn/Out`)
- Create: `Assets/_Game/Board/Editor/BoosterBackdropAnimationBuilder.cs`
- Test: `Assets/_Game/Board/Tests/BoosterBackdropAnimationBuilderTests.cs`

**Interfaces:**
- Produces: `BoardController.backdropFadeIn`, `backdropFadeOut` (`LitMotionAnimation`, serialized); `BoosterBackdropAnimationBuilder.Build(BoardController)`, `FadeDur = 0.15f`.

- [ ] **Step 1: Viết test**

```csharp
// Tool mờ tấm nền booster — chạy trên object tạm, không đụng scene thật.
using LitMotion.Animation;
using LitMotion.Animation.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordStack.Board.Editor;

namespace WordStack.Board.Tests
{
    public class BoosterBackdropAnimationBuilderTests
    {
        GameObject board, backdrop;

        [TearDown]
        public void TearDown()
        {
            if (board != null) Object.DestroyImmediate(board);
            if (backdrop != null) Object.DestroyImmediate(backdrop);
        }

        static float Value(LitMotionAnimation anim, string field)
        {
            var c = new SerializedObject(anim).FindProperty("components").GetArrayElementAtIndex(0);
            return c.FindPropertyRelative("settings").FindPropertyRelative(field).floatValue;
        }

        [Test]
        public void Build_AddsFadeInAndOutOnBackdropAndWiresFields()
        {
            board = new GameObject("board");
            var bc = board.AddComponent<BoardController>();
            backdrop = new GameObject("backdrop");
            var so = new SerializedObject(bc);
            so.FindProperty("boosterBackdrop").objectReferenceValue = backdrop;
            so.ApplyModifiedPropertiesWithoutUndo();

            BoosterBackdropAnimationBuilder.Build(bc);
            BoosterBackdropAnimationBuilder.Build(bc);   // chạy lại không đẻ thêm component

            Assert.IsNotNull(backdrop.GetComponent<CanvasGroup>(), "thiếu CanvasGroup thì tool thêm");
            var anims = backdrop.GetComponents<LitMotionAnimation>();
            Assert.AreEqual(2, anims.Length);

            so.Update();
            var fadeIn = (LitMotionAnimation)so.FindProperty("backdropFadeIn").objectReferenceValue;
            var fadeOut = (LitMotionAnimation)so.FindProperty("backdropFadeOut").objectReferenceValue;
            Assert.AreSame(anims[0], fadeIn);
            Assert.AreSame(anims[1], fadeOut);
            foreach (var a in anims)
            {
                Assert.AreEqual(1, a.Components.Count);
                Assert.IsTrue(a.Components[0] is CanvasGroupAlphaAnimation);
                Assert.AreEqual(BoosterBackdropAnimationBuilder.FadeDur, Value(a, "duration"), 1e-6f);
            }
            Assert.AreEqual(0f, Value(fadeIn, "startValue"));
            Assert.AreEqual(1f, Value(fadeIn, "endValue"));
            Assert.AreEqual(1f, Value(fadeOut, "startValue"));
            Assert.AreEqual(0f, Value(fadeOut, "endValue"));
        }
    }
}
```

- [ ] **Step 2: Compile — phải đỏ.** Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `game.dll OK`, `editor.dll OK`, `meta.dll OK`, `board-tests.dll OK`, `meta-tests.dll OK`.

- [ ] **Step 3: BoardController + BoosterAnimSettings**

```diff
diff --git a/Assets/_Game/Board/Views/BoardController.cs b/Assets/_Game/Board/Views/BoardController.cs
index 8f58298..37dbedd 100644
--- a/Assets/_Game/Board/Views/BoardController.cs
+++ b/Assets/_Game/Board/Views/BoardController.cs
@@ -20,6 +20,7 @@ using UnityEngine;
 using UnityEngine.InputSystem;
 using LitMotion;
 using LitMotion.Adapters;
+using LitMotion.Animation;
 using LitMotion.Extensions;
 using WordStack.Contracts;
 
@@ -57,11 +58,15 @@ namespace WordStack.Board
         // mặc định khai trong class, bàn không sập.
         [SerializeField] BoosterAnimSettings animSettings;
         // Tấm nền xám bật suốt lúc booster diễn: chặn click uGUI và làm nền cho thẻ bay.
-        // User tự dựng Panel; code chỉ bật/tắt (có CanvasGroup thì mờ dần theo SO). Muốn thẻ
+        // User tự dựng Panel; code chỉ bật/tắt (mờ vào/ra bằng hai LitMotionAnimation bên dưới). Muốn thẻ
         // bay NỔI TRÊN tấm nền thì Panel phải nằm dưới sorting 90 của thẻ bay — tức Canvas
         // riêng Screen Space-Camera (order 20..89) hoặc SpriteRenderer world-space; Canvas
         // Screen Space-Overlay luôn vẽ đè lên mọi sprite.
         [SerializeField] GameObject boosterBackdrop;
+        // CanvasGroup alpha 0→1 / 1→0 trên boosterBackdrop — dựng bằng Tools ▸ WordStack ▸ Build Booster
+        // Backdrop Animation, chỉnh và xem trước ngay trên component. Để trống thì nền bật/tắt khan.
+        [SerializeField] LitMotionAnimation backdropFadeIn;
+        [SerializeField] LitMotionAnimation backdropFadeOut;
 
         // DEBUG: tự gắn bộ blocker mẫu (y như phím B) lên MỌI màn ngay lúc nạp. Level JSON
         // chưa author blocker nên đây là cách duy nhất thấy chúng trong luồng chơi thật.
@@ -726,26 +731,43 @@ namespace WordStack.Board
             return d;
         }
 
-        // Bật/tắt tấm nền booster. Có CanvasGroup → mờ dần theo backdropFadeIn/Out; không có →
-        // SetActive khan. Chưa gán → không làm gì (bàn vẫn chạy). Tắt xong mới Rebuild để nền
-        // không che cascade sau đó.
+        // Bật/tắt tấm nền booster bằng hai LitMotionAnimation (spec 2026-10-02-booster-anim-preview Mục 3).
+        // Thiếu component → SetActive khan. Chưa gán nền → không làm gì (bàn vẫn chạy). Tắt xong mới
+        // Rebuild để nền không che cascade sau đó.
+        //
+        // LitMotionAnimation.Stop() trả giá trị về lúc trước Play: KHÔNG Stop fade-in khi xong (alpha sẽ
+        // về 0); trước fade-out mới Stop nó và đặt lại alpha = 1 ngay cùng frame.
         IEnumerator Backdrop(bool on)
         {
             if (boosterBackdrop == null) yield break;
-            var a = A;
             var cg = boosterBackdrop.GetComponent<CanvasGroup>();
-            float dur = on ? a.backdropFadeIn : a.backdropFadeOut;
-            if (cg == null || dur <= 0f)
+            var anim = on ? backdropFadeIn : backdropFadeOut;
+            if (cg == null || anim == null)
             {
-                boosterBackdrop.SetActive(on);
+                if (on) boosterBackdrop.SetActive(true);
                 if (cg != null) cg.alpha = on ? 1f : 0f;
+                if (!on) boosterBackdrop.SetActive(false);
                 yield break;
             }
-            if (on) { cg.alpha = 0f; boosterBackdrop.SetActive(true); }
-            yield return LMotion.Create(cg.alpha, on ? 1f : 0f, dur)
-                                .WithEase(Ease.OutQuad).WithCancelOnError()
-                                .BindToAlpha(cg).AddTo(boosterBackdrop).ToYieldInstruction();
-            if (!on) boosterBackdrop.SetActive(false);
+
+            if (on)
+            {
+                cg.alpha = 0f;
+                boosterBackdrop.SetActive(true);
+            }
+            else
+            {
+                if (backdropFadeIn != null) backdropFadeIn.Stop();
+                cg.alpha = 1f;
+            }
+            anim.Stop();
+            anim.Play();
+            while (anim != null && anim.IsPlaying) yield return null;
+            if (!on)
+            {
+                anim.Stop();
+                boosterBackdrop.SetActive(false);
+            }
         }
 
         // Chốt chung cho mọi booster. Log từng lý do từ chối — không có nó thì bấm xong
diff --git a/Assets/_Game/Board/Views/BoosterAnimSettings.cs b/Assets/_Game/Board/Views/BoosterAnimSettings.cs
index d5cc08f..bf31e6c 100644
--- a/Assets/_Game/Board/Views/BoosterAnimSettings.cs
+++ b/Assets/_Game/Board/Views/BoosterAnimSettings.cs
@@ -9,11 +9,6 @@ namespace WordStack.Board
     [CreateAssetMenu(menuName = "WordStack/Booster Anim Settings", fileName = "SO_BoosterAnim")]
     public class BoosterAnimSettings : ScriptableObject
     {
-        [Header("Tấm nền xám suốt lúc booster diễn (BoardController.boosterBackdrop)")]
-        [Tooltip("Mờ dần vào (giây) — chỉ khi Panel có CanvasGroup; 0 = bật khan")]
-        public float backdropFadeIn = 0.15f;
-        public float backdropFadeOut = 0.15f;
-
         [Header("Nam châm — 4 thẻ bay về một điểm rồi nổ")]
         [Tooltip("Điểm hội tụ theo toạ độ viewport của camera: (0.5, 0.5) = giữa màn hình")]
         public Vector2 magnetGatherViewport = new Vector2(0.5f, 0.5f);
```

- [ ] **Step 4: Menu dựng**

```csharp
// Dựng hai animation mờ vào / mờ ra của tấm nền booster (spec docs/superpowers/specs/2026-10-02-booster-anim-preview-design.md
// Mục 3): hai LitMotionAnimation trên BoardController.boosterBackdrop, mỗi cái một CanvasGroup/Alpha, nối vào
// backdropFadeIn / backdropFadeOut. Tools ▸ WordStack ▸ Build Booster Backdrop Animation — chạy trên scene đang mở,
// xong thì LƯU SCENE. Chạy lại = dựng lại, ghi đè số đã chỉnh trong Inspector.
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static WordStack.Board.Editor.AnimationBuildKit;

namespace WordStack.Board.Editor
{
    public static class BoosterBackdropAnimationBuilder
    {
        const string UndoName = "Build Booster Backdrop Animation";
        public const float FadeDur = 0.15f;

        [MenuItem("Tools/WordStack/Build Booster Backdrop Animation")]
        static void Menu()
        {
            var bc = Object.FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
            if (bc == null) { Debug.LogError("[Backdrop] Scene đang mở không có BoardController."); return; }
            Build(bc);
            EditorSceneManager.MarkSceneDirty(bc.gameObject.scene);
            Debug.Log("[Backdrop] Đã dựng mờ vào/ra cho tấm nền booster — nhớ lưu scene.");
        }

        /// <summary>Dựng hai fade lên boosterBackdrop của bc và nối field. Thiếu nền thì ném lỗi.</summary>
        public static void Build(BoardController bc)
        {
            var so = new SerializedObject(bc);
            var backdrop = (GameObject)so.FindProperty("boosterBackdrop").objectReferenceValue;
            if (backdrop == null)
                throw new System.InvalidOperationException("BoardController chưa gán boosterBackdrop.");

            var cg = backdrop.GetComponent<CanvasGroup>();
            if (cg == null) cg = Undo.AddComponent<CanvasGroup>(backdrop);

            var anims = backdrop.GetComponents<LitMotionAnimation>();
            var fadeIn = anims.Length > 0 ? anims[0] : Undo.AddComponent<LitMotionAnimation>(backdrop);
            var fadeOut = anims.Length > 1 ? anims[1] : Undo.AddComponent<LitMotionAnimation>(backdrop);
            Fill(fadeIn, cg, "Nền · mờ vào", 0f, 1f);
            Fill(fadeOut, cg, "Nền · mờ ra", 1f, 0f);

            so.FindProperty("backdropFadeIn").objectReferenceValue = fadeIn;
            so.FindProperty("backdropFadeOut").objectReferenceValue = fadeOut;
            so.ApplyModifiedProperties();
        }

        static void Fill(LitMotionAnimation anim, CanvasGroup cg, string name, float from, float to)
        {
            Write(anim, UndoName, (new CanvasGroupAlphaAnimation(), c =>
            {
                Common(c, name, cg, false);
                var s = c.FindPropertyRelative("settings");
                s.FindPropertyRelative("startValue").floatValue = from;
                s.FindPropertyRelative("endValue").floatValue = to;
                Timing(s, FadeDur, 0f, Ease.OutQuad);
            }));
        }
    }
}
```

- [ ] **Step 5: Compile — xanh.** Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `game.dll OK`, `editor.dll OK`, `meta.dll OK`, `board-tests.dll OK`, `meta-tests.dll OK`.

- [ ] **Step 6: Commit** — 4 file trên, message `Booster anim: backdrop fades are LitMotionAnimation components`.

---

### Task 3: Nút booster HUD nảy khi bấm

**Files:**
- Modify: `Assets/_Game/Gameplay/Boosters/Views/BoosterButtonView.cs`
- Modify: `Assets/_Game/WordStack.Meta.asmdef`, `Assets/_Game/Editor/WordStack.Meta.Editor.asmdef`, `Assets/_Game/Gameplay/Tests/WordStack.Meta.Tests.asmdef`
- Create: `Assets/_Game/Editor/BoosterButtonPunchBuilder.cs`
- Test: `Assets/_Game/Gameplay/Tests/BoosterButtonPunchBuilderTests.cs`

**Interfaces:**
- Produces: `BoosterButtonView._clickPunch` (`LitMotionAnimation`); `BoosterButtonPunchBuilder.Build(GameObject root) → int`, hằng `PrefabPath`, `Strength`, `Duration`, `Frequency`, `DampingRatio`.

- [ ] **Step 1: Viết test**

```csharp
// Tool nảy nút booster HUD — chạy trên bản mở tạm của GamePlayUIRoot .prefab thật, KHÔNG lưu.
using LitMotion.Animation;
using LitMotion.Animation.Components;
using LogosGame.Features.Gameplay.Boosters.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordStack.Meta.Editor;

namespace WordStack.Meta.Tests
{
    public class BoosterButtonPunchBuilderTests
    {
        [Test]
        public void Build_AddsScalePunchOnEveryBoosterButtonAndWires()
        {
            var root = PrefabUtility.LoadPrefabContents(BoosterButtonPunchBuilder.PrefabPath);
            try
            {
                var views = root.GetComponentsInChildren<BoosterButtonView>(true);
                Assert.AreEqual(3, views.Length, "Magnet / Shuffle / Undo");
                Assert.AreEqual(3, BoosterButtonPunchBuilder.Build(root));
                BoosterButtonPunchBuilder.Build(root);   // chạy lại không đẻ thêm component

                foreach (var view in views)
                {
                    var so = new SerializedObject(view);
                    var button = (Component)so.FindProperty("_button").objectReferenceValue;
                    var anim = (LitMotionAnimation)so.FindProperty("_clickPunch").objectReferenceValue;
                    Assert.IsNotNull(anim, view.name + ": _clickPunch chưa nối");
                    Assert.AreSame(button.gameObject, anim.gameObject, "anim nằm trên chính nút");
                    Assert.AreEqual(1, button.GetComponents<LitMotionAnimation>().Length);
                    Assert.AreEqual(1, anim.Components.Count);
                    Assert.IsTrue(anim.Components[0] is TransformScalePunchAnimation);

                    var c = new SerializedObject(anim).FindProperty("components").GetArrayElementAtIndex(0);
                    Assert.AreSame(button.transform, c.FindPropertyRelative("target").objectReferenceValue);
                    Assert.IsTrue(c.FindPropertyRelative("relative").boolValue);
                    var s = c.FindPropertyRelative("settings");
                    Assert.AreEqual(Vector3.one * BoosterButtonPunchBuilder.Strength, s.FindPropertyRelative("endValue").vector3Value);
                    Assert.AreEqual(BoosterButtonPunchBuilder.Duration, s.FindPropertyRelative("duration").floatValue, 1e-6f);
                    var o = s.FindPropertyRelative("options");
                    Assert.AreEqual(BoosterButtonPunchBuilder.Frequency, o.FindPropertyRelative("Frequency").intValue);
                    Assert.AreEqual(BoosterButtonPunchBuilder.DampingRatio, o.FindPropertyRelative("DampingRatio").floatValue, 1e-6f);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
```

- [ ] **Step 2: Compile — phải đỏ.** Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `game.dll OK`, `editor.dll OK`, `meta.dll OK`, `board-tests.dll OK`, `meta-tests.dll OK`.

- [ ] **Step 3: View + asmdef**

```diff
diff --git a/Assets/_Game/Editor/WordStack.Meta.Editor.asmdef b/Assets/_Game/Editor/WordStack.Meta.Editor.asmdef
index 6884c74..379d030 100644
--- a/Assets/_Game/Editor/WordStack.Meta.Editor.asmdef
+++ b/Assets/_Game/Editor/WordStack.Meta.Editor.asmdef
@@ -13,7 +13,9 @@
         "Unity.Addressables.Editor",
         "Unity.Addressables",
         "Unity.TextMeshPro",
-        "LogosSDK.UI"
+        "LogosSDK.UI",
+        "LitMotion",
+        "LitMotion.Animation"
     ],
     "includePlatforms": [
         "Editor"
diff --git a/Assets/_Game/Gameplay/Boosters/Views/BoosterButtonView.cs b/Assets/_Game/Gameplay/Boosters/Views/BoosterButtonView.cs
index 37971b4..36bedc7 100644
--- a/Assets/_Game/Gameplay/Boosters/Views/BoosterButtonView.cs
+++ b/Assets/_Game/Gameplay/Boosters/Views/BoosterButtonView.cs
@@ -5,6 +5,7 @@ using LogosGame.Features.Gameplay.Boosters.ViewModels;
 using LogosGame.Features.Gameplay.Content;
 using LogosGame.Features.Gameplay.Flow;
 using LogosMeta.Economy;
+using LitMotion.Animation;
 using LogosSDK.Core.Events;
 using R3;
 using Reflex.Attributes;
@@ -36,6 +37,9 @@ namespace LogosGame.Features.Gameplay.Boosters.Views
         private enum State { Locked, HasStock, Buyable, WatchAd }
 
         [SerializeField] private Button _button;
+        [Tooltip("Nảy khi bấm — Scale (Punch) trên chính nút. Dựng bằng Tools ▸ WordStack ▸ Build Booster Button " +
+                 "Punch; để trống thì không nảy.")]
+        [SerializeField] private LitMotionAnimation _clickPunch;
         [Tooltip("Để trống thì tự lấy/thêm trên chính GameObject này.")]
         [SerializeField] private CanvasGroup _canvasGroup;
         [Tooltip("CanvasGroup của riêng phần nút (nền + icon + số lượt), KHÔNG chứa nhãn giá / icon ad. " +
@@ -151,6 +155,13 @@ namespace LogosGame.Features.Gameplay.Boosters.Views
         {
             if (ViewModel == null) return;
 
+            // Stop trả scale về cỡ thường rồi mới nảy lại — Play khi đang chạy chỉ là chạy tiếp.
+            if (_clickPunch != null)
+            {
+                _clickPunch.Stop();
+                _clickPunch.Play();
+            }
+
             switch (_state)
             {
                 case State.HasStock:
diff --git a/Assets/_Game/Gameplay/Tests/WordStack.Meta.Tests.asmdef b/Assets/_Game/Gameplay/Tests/WordStack.Meta.Tests.asmdef
index cc9783f..35744ae 100644
--- a/Assets/_Game/Gameplay/Tests/WordStack.Meta.Tests.asmdef
+++ b/Assets/_Game/Gameplay/Tests/WordStack.Meta.Tests.asmdef
@@ -10,7 +10,10 @@
         "Core.EventBus",
         "LogosSDK.Save",
         "UnityEngine.TestRunner",
-        "UnityEditor.TestRunner"
+        "UnityEditor.TestRunner",
+        "WordStack.Meta.Editor",
+        "LitMotion",
+        "LitMotion.Animation"
     ],
     "includePlatforms": [
         "Editor"
diff --git a/Assets/_Game/WordStack.Meta.asmdef b/Assets/_Game/WordStack.Meta.asmdef
index 14e731c..be89ac2 100644
--- a/Assets/_Game/WordStack.Meta.asmdef
+++ b/Assets/_Game/WordStack.Meta.asmdef
@@ -23,7 +23,8 @@
         "Unity.Services.Core",
         "LitMotion",
         "LitMotion.Extensions",
-        "LogosSDK.Tween"
+        "LogosSDK.Tween",
+        "LitMotion.Animation"
     ],
     "includePlatforms": [],
     "excludePlatforms": [],
```

- [ ] **Step 4: Menu dựng**

```csharp
// Dựng hiệu ứng nảy khi bấm cho nút booster HUD (spec docs/superpowers/specs/2026-10-02-booster-anim-preview-design.md
// Mục 4): mỗi BoosterButtonView trong GamePlayUIRoot .prefab có một LitMotionAnimation Transform/Scale (Punch) trên
// GameObject của _button, nối vào _clickPunch. Tools ▸ WordStack ▸ Build Booster Button Punch — sửa thẳng asset
// (LoadPrefabContents → SaveAsPrefabAsset): ĐÓNG Prefab Mode của prefab đó trước. Chạy lại = dựng lại từ đầu.
//
// Cách ghi component giống AnimationBuildKit (WordStack.Board.Editor) nhưng không dùng được nó: assembly này nằm ở
// thế giới compile netstandard của meta, không tham chiếu WordStack.Board.
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using LogosGame.Features.Gameplay.Boosters.Views;
using UnityEditor;
using UnityEngine;

namespace WordStack.Meta.Editor
{
    public static class BoosterButtonPunchBuilder
    {
        public const string PrefabPath = "Assets/_Shared/Prefab/GamePlayUIRoot .prefab";
        const string UndoName = "Build Booster Button Punch";

        // Cùng số với nút demo BoosterModule/BoosterSlotView: phồng 0.2 trong 0.2 s, rung 10, tắt dần 1.9.
        public const float Strength = 0.2f, Duration = 0.2f, DampingRatio = 1.9f;
        public const int Frequency = 10;

        [MenuItem("Tools/WordStack/Build Booster Button Punch")]
        static void Menu()
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == PrefabPath)
            { Debug.LogError("[BoosterPunch] Đóng Prefab Mode của " + PrefabPath + " trước khi chạy tool."); return; }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                int n = Build(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BoosterPunch] " + PrefabPath + ": đã dựng nảy cho " + n + " nút booster.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>Dựng punch cho mọi BoosterButtonView dưới root. Trả số nút đã dựng.</summary>
        public static int Build(GameObject root)
        {
            int n = 0;
            foreach (var view in root.GetComponentsInChildren<BoosterButtonView>(true))
            {
                var so = new SerializedObject(view);
                var button = so.FindProperty("_button").objectReferenceValue as Component;
                if (button == null)
                {
                    Debug.LogWarning("[BoosterPunch] " + view.name + " chưa gán _button — bỏ qua.");
                    continue;
                }

                var anim = button.GetComponent<LitMotionAnimation>();
                if (anim == null) anim = Undo.AddComponent<LitMotionAnimation>(button.gameObject);
                Write(anim, button.transform);

                so.FindProperty("_clickPunch").objectReferenceValue = anim;
                so.ApplyModifiedPropertiesWithoutUndo();
                n++;
            }
            return n;
        }

        // Ghi đè toàn bộ component: Parallel, không tự chạy (code gọi Play).
        static void Write(LitMotionAnimation anim, Transform target)
        {
            Undo.RecordObject(anim, UndoName);
            var so = new SerializedObject(anim);
            // version = 1 + playOnAwake = false: né migration trong OnAfterDeserialize ghi đè autoPlayMode.
            so.FindProperty("version").intValue = 1;
            so.FindProperty("playOnAwake").boolValue = false;
            so.FindProperty("autoPlayMode").enumValueIndex = 0;    // None
            so.FindProperty("animationMode").enumValueIndex = 0;   // Parallel
            var arr = so.FindProperty("components");
            arr.arraySize = 1;
            arr.GetArrayElementAtIndex(0).managedReferenceValue = new TransformScalePunchAnimation();
            so.ApplyModifiedProperties();   // phải có instance rồi mới có property con để ghi
            so.Update();

            var c = arr.GetArrayElementAtIndex(0);
            c.FindPropertyRelative("displayName").stringValue = "Nút · nảy khi bấm";
            c.FindPropertyRelative("target").objectReferenceValue = target;
            c.FindPropertyRelative("relative").boolValue = true;   // nảy quanh cỡ hiện tại của nút
            var s = c.FindPropertyRelative("settings");
            s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
            s.FindPropertyRelative("endValue").vector3Value = Vector3.one * Strength;
            s.FindPropertyRelative("duration").floatValue = Duration;
            s.FindPropertyRelative("delay").floatValue = 0f;
            s.FindPropertyRelative("ease").intValue = (int)Ease.Linear;
            s.FindPropertyRelative("loops").intValue = 1;
            var o = s.FindPropertyRelative("options");
            o.FindPropertyRelative("Frequency").intValue = Frequency;
            o.FindPropertyRelative("DampingRatio").floatValue = DampingRatio;
            so.ApplyModifiedProperties();
        }
    }
}
```

- [ ] **Step 5: Compile — xanh.** Run: `./compilecheck.sh && bash .git/sdd/testcheck.sh`
Expected: `game.dll OK`, `editor.dll OK`, `meta.dll OK`, `board-tests.dll OK`, `meta-tests.dll OK`.

- [ ] **Step 6: Commit** — 6 file trên, message `Booster anim: HUD booster buttons punch on click`.

---

### Task 4: Trong Unity (user làm)

- [ ] **Step 1:** Mở Unity cho nó import, commit các file `.meta` mới (6 script).
- [ ] **Step 2:** `Tools ▸ WordStack ▸ Build Booster Button Punch` (đóng Prefab Mode của `GamePlayUIRoot .prefab` trước).
- [ ] **Step 3:** Mở `Main.unity`, `Tools ▸ WordStack ▸ Build Booster Backdrop Animation`, lưu scene.
- [ ] **Step 4:** Test Runner → EditMode: `BoosterTilePreviewTests`, `BoosterBackdropAnimationBuilderTests`, `BoosterButtonPunchBuilderTests` xanh.
- [ ] **Step 5:** Chọn `SO_BoosterAnim`, chọn một thẻ (mở `Tile.prefab`), bấm 4 nút Preview; bấm Play trên component nền / nút.
- [ ] **Step 6:** Play mode: dùng booster — nền mờ vào/ra; bấm nút booster HUD — nút nảy.
