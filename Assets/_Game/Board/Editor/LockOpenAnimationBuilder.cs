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
