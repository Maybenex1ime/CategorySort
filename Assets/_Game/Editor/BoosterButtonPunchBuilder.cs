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
