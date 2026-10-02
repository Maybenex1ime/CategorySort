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
