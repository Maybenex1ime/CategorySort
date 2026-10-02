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
