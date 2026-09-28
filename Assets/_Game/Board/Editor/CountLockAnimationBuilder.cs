// Dựng hai LitMotionAnimation của hộp khoá theo số (blocker Box) trên Chained Root, nối vào BoxView:
//   countStepAnim   — số giảm 1: Handle quay một vòng ngược chiều kim đồng hồ + root nảy.
//   countUnlockAnim — số về 0: Handle quay một vòng, xong root trượt lên + mờ (sprite lẫn chữ).
// Mở Box.prefab, chọn object gốc (có BoxView), Tools ▸ WordStack ▸ Build Count Lock Animations, Save.
// Số mặc định lấy từ bản script cũ. Chạy MỘT lần — về sau chỉnh thẳng trong Inspector; chạy lại là
// ghi đè số đã chỉnh. Undo được.
// ponytail: tool dùng một lần, xoá được sau khi prefab đã có hai animation.
using FigmaMotion;
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using UnityEditor;
using UnityEngine;

namespace WordStack.Board.Editor
{
    static class CountLockAnimationBuilder
    {
        const float SpinDur = 0.45f;
        const float Punch = 0.12f, PunchDur = 0.25f;
        const float LiftDur = 0.35f, Lift = 0.25f;

        [MenuItem("Tools/WordStack/Build Count Lock Animations")]
        static void Build()
        {
            var go = Selection.activeGameObject;
            var box = go != null ? go.GetComponent<BoxView>() : null;
            if (box == null) { Debug.LogWarning("[CountLock] Chọn object gốc có BoxView (trong Box.prefab) trước."); return; }

            var bso = new SerializedObject(box);
            var root = bso.FindProperty("lockedRoot").objectReferenceValue as GameObject;
            var text = bso.FindProperty("lockedCountText").objectReferenceValue as Component;
            if (root == null) { Debug.LogWarning("[CountLock] BoxView chưa nối Locked Root.", box); return; }
            var handle = FindDeep(root.transform, "Handle");
            if (handle == null) { Debug.LogWarning("[CountLock] Không thấy con \"Handle\" dưới " + root.name + ".", root); return; }

            Undo.SetCurrentGroupName("Build Count Lock Animations");
            int group = Undo.GetCurrentGroup();

            var anims = root.GetComponents<LitMotionAnimation>();
            var step = anims.Length > 0 ? anims[0] : Undo.AddComponent<LitMotionAnimation>(root);
            var unlock = anims.Length > 1 ? anims[1] : Undo.AddComponent<LitMotionAnimation>(root);

            var scale = root.transform.localScale;
            Write(step,
                (new TransformRotationAnimation(), c => Spin(c, handle)),
                (new TransformScalePunchAnimation(), c =>
                {
                    Common(c, "Chained Root · nảy", root.transform, true);
                    var s = c.FindPropertyRelative("settings");
                    s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                    s.FindPropertyRelative("endValue").vector3Value = scale * Punch;
                    Timing(s, PunchDur, 0f, Ease.Linear);
                    // DOPunchScale(vibrato 8, elasticity 0.6) cũ, cùng quy đổi với BoxView bản script.
                    s.FindPropertyRelative("options").FindPropertyRelative("Frequency").intValue = 8;
                    s.FindPropertyRelative("options").FindPropertyRelative("DampingRatio").floatValue = 2.4f;
                }));

            var parts = new System.Collections.Generic.List<(LitMotionAnimationComponent, System.Action<SerializedProperty>)>
            {
                (new TransformRotationAnimation(), c => Spin(c, handle)),
                (new TransformPositionAnimation(), c =>
                {
                    Common(c, "Chained Root · trượt lên", root.transform, true);
                    var s = c.FindPropertyRelative("settings");
                    s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                    s.FindPropertyRelative("endValue").vector3Value = new Vector3(0f, Lift, 0f);
                    Timing(s, LiftDur, SpinDur, Ease.OutCubic);
                }),
                (new SpriteGroupAlphaAnimation(), c => Fade(c, "Chained Root · mờ", root.transform)),
            };
            if (text != null) parts.Add((new TMPTextColorAlphaAnimation(), c => Fade(c, "Lock Text · mờ", text)));
            Write(unlock, parts.ToArray());

            bso.Update();
            bso.FindProperty("countStepAnim").objectReferenceValue = step;
            bso.FindProperty("countUnlockAnim").objectReferenceValue = unlock;
            bso.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(group);
            Debug.Log("[CountLock] Đã dựng 2 LitMotionAnimation trên " + root.name + ". Nhớ Save prefab.", root);
        }

        static void Spin(SerializedProperty c, Transform handle)
        {
            Common(c, "Handle · quay 1 vòng", handle, true);
            var s = c.FindPropertyRelative("settings");
            s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
            s.FindPropertyRelative("endValue").vector3Value = new Vector3(0f, 0f, 360f);   // z+ = ngược chiều kim đồng hồ
            Timing(s, SpinDur, 0f, Ease.OutBack);
        }

        static void Fade(SerializedProperty c, string name, Object target)
        {
            Common(c, name, target, false);
            var s = c.FindPropertyRelative("settings");
            s.FindPropertyRelative("startValue").floatValue = 1f;
            s.FindPropertyRelative("endValue").floatValue = 0f;
            Timing(s, LiftDur, SpinDur, Ease.OutCubic);
        }

        static void Common(SerializedProperty c, string name, Object target, bool relative)
        {
            c.FindPropertyRelative("displayName").stringValue = name;
            c.FindPropertyRelative("target").objectReferenceValue = target;
            c.FindPropertyRelative("relative").boolValue = relative;
        }

        static void Timing(SerializedProperty s, float duration, float delay, Ease ease)
        {
            s.FindPropertyRelative("duration").floatValue = duration;
            s.FindPropertyRelative("delay").floatValue = delay;
            s.FindPropertyRelative("ease").intValue = (int)ease;
            s.FindPropertyRelative("loops").intValue = 1;
        }

        // Ghi đè toàn bộ component của anim: Parallel, không tự chạy (BoxView gọi Play).
        static void Write(LitMotionAnimation anim, params (LitMotionAnimationComponent comp, System.Action<SerializedProperty> fill)[] parts)
        {
            Undo.RecordObject(anim, "Build Count Lock Animations");
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

        static Transform FindDeep(Transform t, string name)
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
