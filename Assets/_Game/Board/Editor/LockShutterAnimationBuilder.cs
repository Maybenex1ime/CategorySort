// Dựng LitMotionAnimation "cửa chớp gập" trên Lock Root từ keyframe Figma
// (Logos-Project › Group 547, node 373:1245). Mở Box.prefab, chọn Lock Root, rồi
// Tools ▸ WordStack ▸ Build Lock Shutter Animation. Sau đó chỉnh + preview thẳng trong Inspector
// của LitMotionAnimation (nút Play/Stop của package chạy được trong Edit mode).
//
// Tool làm ba việc, đều Undo được:
//   1. Gỡ script mất (LockShutterMotion cũ đã xoá).
//   2. Tách lớp nan (con tên chứa "Upper") ra ngang hàng lớp nền, giữ vị trí world — Figma để hai
//      lớp là anh em; là con thì scale của nền nhân lên nan, sai animation.
//   3. Ghi đè LitMotionAnimation: mỗi tấm 4 component (nền X, nền scaleX, nan X, nan scaleX), chạy
//      song song, Relative (cộng lên vị trí/scale author), ease = custom curve chuẩn hoá 0..1.
// Chạy lại là dựng lại từ số Figma — chỉnh tay trong Inspector trước đó sẽ mất.
//
// Số Figma (timeline 2 s lặp, mọi chuyển động trong 0.5 s đầu; ở đây chạy một lần):
//   nền  X: 0 → −30 px @0.25 s → −37 px @0.5 s, hai đoạn ease-out
//   nền  scaleX: giữ 1 tới 0.063 s, ease-out về 0 @0.5 s
//   nan  X: 8 mẫu bake, nội suy thẳng, −39 px @0.458 s
//   nan  scaleX: giữ 1 tới 0.063 s, ease-out về 0 @0.458 s
// Tấm nằm bên phải tâm Lock Root được lật dấu X → gập về mép phải (hai cánh mở ra hai bên).
using System.Collections.Generic;
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using UnityEditor;
using UnityEngine;

namespace WordStack.Board.Editor
{
    static class LockShutterAnimationBuilder
    {
        // 1 px Figma = 0.005 unit trong Lock Root: art xuất 2x (Blocker_Box_LitBack 142 px = 71 px Figma), PPU 400.
        const float PxToUnit = 0.005f;
        const float HoldEnd = 0.063f;             // 3.15 % của 2 s
        const float BackEnd = 0.5f, UpperEnd = 0.458f;
        const float BackPx = -37f, UpperPx = -39f;

        [MenuItem("Tools/WordStack/Build Lock Shutter Animation")]
        static void Build()
        {
            var root = Selection.activeGameObject;
            if (root == null) { Debug.LogWarning("[LockShutter] Chọn Lock Root (trong Box.prefab) trước."); return; }

            Undo.SetCurrentGroupName("Build Lock Shutter Animation");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RegisterCompleteObjectUndo(root, "Remove missing scripts");
            int missing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

            var panels = new List<(Transform back, Transform upper, float dir)>();
            foreach (Transform c in root.transform)
            {
                Transform up = null;
                foreach (Transform g in c) if (g.name.Contains("Upper")) { up = g; break; }
                if (up != null) panels.Add((c, up, c.localPosition.x > 0f ? -1f : 1f));
            }
            if (panels.Count == 0) { Debug.LogWarning("[LockShutter] Không thấy tấm nào: cần con có con tên chứa \"Upper\".", root); return; }

            foreach (var p in panels)
                if (p.upper.parent == p.back)
                    Undo.SetTransformParent(p.upper, root.transform, "Flatten shutter slats");   // giữ vị trí world

            var anim = root.GetComponent<LitMotionAnimation>();
            if (anim == null) anim = Undo.AddComponent<LitMotionAnimation>(root);
            Undo.RecordObject(anim, "Build Lock Shutter Animation");

            var so = new SerializedObject(anim);
            so.FindProperty("autoPlayMode").enumValueIndex = 0;    // None — BoxView.OpenGroupLock tự gọi Play
            so.FindProperty("animationMode").enumValueIndex = 0;   // Parallel
            var arr = so.FindProperty("components");
            arr.arraySize = panels.Count * 4;

            // Lượt 1: gán instance. Lượt 2 (sau Apply/Update) mới có property con để ghi.
            for (int i = 0; i < panels.Count; i++)
            {
                arr.GetArrayElementAtIndex(i * 4 + 0).managedReferenceValue = new TransformPositionAnimation();
                arr.GetArrayElementAtIndex(i * 4 + 1).managedReferenceValue = new TransformScaleAnimation();
                arr.GetArrayElementAtIndex(i * 4 + 2).managedReferenceValue = new TransformPositionAnimation();
                arr.GetArrayElementAtIndex(i * 4 + 3).managedReferenceValue = new TransformScaleAnimation();
            }
            so.ApplyModifiedProperties();
            so.Update();

            for (int i = 0; i < panels.Count; i++)
            {
                var p = panels[i];
                string n = p.back.name;
                Fill(arr.GetArrayElementAtIndex(i * 4 + 0), n + " · nền X", p.back, true,
                     new Vector3(p.dir * BackPx * PxToUnit, 0f, 0f), 0f, BackEnd,
                     EaseOut((0f, 0f), (0.5f, 30f / 37f), (1f, 1f)));
                Fill(arr.GetArrayElementAtIndex(i * 4 + 1), n + " · nền scaleX", p.back, false,
                     new Vector3(-p.back.localScale.x, 0f, 0f), HoldEnd, BackEnd - HoldEnd,
                     EaseOut((0f, 0f), (1f, 1f)));
                Fill(arr.GetArrayElementAtIndex(i * 4 + 2), n + " · nan X", p.upper, true,
                     new Vector3(p.dir * UpperPx * PxToUnit, 0f, 0f), 0f, UpperEnd,
                     UpperXCurve());
                Fill(arr.GetArrayElementAtIndex(i * 4 + 3), n + " · nan scaleX", p.upper, false,
                     new Vector3(-p.upper.localScale.x, 0f, 0f), HoldEnd, UpperEnd - HoldEnd,
                     EaseOut((0f, 0f), (1f, 1f)));
            }
            so.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(root);

            Debug.Log($"[LockShutter] Dựng {panels.Count} tấm × 4 component trên {root.name}" +
                      (missing > 0 ? $", gỡ {missing} script mất" : "") + ". Nhớ Save prefab.", anim);
        }

        static void Fill(SerializedProperty el, string name, Transform target, bool isPosition,
                         Vector3 end, float delay, float duration, AnimationCurve curve)
        {
            el.FindPropertyRelative("displayName").stringValue = name;
            el.FindPropertyRelative("target").objectReferenceValue = target;
            el.FindPropertyRelative("relative").boolValue = true;   // cộng lên giá trị author lúc Play
            if (isPosition) el.FindPropertyRelative("useWorldSpace").boolValue = false;
            var s = el.FindPropertyRelative("settings");
            s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
            s.FindPropertyRelative("endValue").vector3Value = end;
            s.FindPropertyRelative("duration").floatValue = duration;
            s.FindPropertyRelative("delay").floatValue = delay;
            s.FindPropertyRelative("ease").intValue = (int)Ease.CustomAnimationCurve;
            s.FindPropertyRelative("customEaseCurve").animationCurveValue = curve;
        }

        // ---- Curve ease chuẩn hoá (thời gian 0..1, tiến độ 0..1) ----

        // CSS ease-out = cubic-bezier(0, 0, 0.58, 1). Key weighted biến mỗi đoạn thành đúng cubic
        // bezier đó: handle ra của key đầu dài ~0 (P1 ≡ P0), handle vào của key sau nằm ngang, dài
        // 0.42 đoạn (P2 = 58 % thời gian, đã tới giá trị đích).
        static AnimationCurve EaseOut(params (float t, float v)[] k)
        {
            var keys = new Keyframe[k.Length];
            for (int i = 0; i < k.Length; i++)
                keys[i] = new Keyframe(k[i].t, k[i].v, 0f, 0f, 0.42f, 0.0001f) { weightedMode = WeightedMode.Both };
            return new AnimationCurve(keys);
        }

        // Lớp nan: 8 mẫu Figma (giây, px) chuẩn hoá theo 0.458 s và −39 px, nội suy thẳng.
        static AnimationCurve UpperXCurve()
        {
            var raw = new[] { (0f, 0f), (0.1f, 18.268f), (0.2f, 30.007f), (0.245f, 31.976f),
                              (0.25f, 32.275f), (0.3f, 34.725f), (0.4f, 38.237f), (0.458f, 39f) };
            var k = new (float t, float v)[raw.Length];
            for (int i = 0; i < raw.Length; i++) k[i] = (raw[i].Item1 / UpperEnd, raw[i].Item2 / 39f);
            var keys = new Keyframe[k.Length];
            for (int i = 0; i < k.Length; i++)
            {
                float inT = i > 0 ? Slope(k[i - 1], k[i]) : Slope(k[i], k[i + 1]);
                float outT = i < k.Length - 1 ? Slope(k[i], k[i + 1]) : inT;
                keys[i] = new Keyframe(k[i].t, k[i].v, inT, outT);
            }
            return new AnimationCurve(keys);
        }

        static float Slope((float t, float v) a, (float t, float v) b) { return (b.v - a.v) / (b.t - a.t); }
    }
}
