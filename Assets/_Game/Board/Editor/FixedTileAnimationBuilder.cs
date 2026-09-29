// Dựng animation tháo đinh của thẻ đóng đinh (spec docs/superpowers/specs/2026-09-29-fixed-tile-design.md Mục 6):
//   4 đinh (con "Blocker - Fixed Tile…" của Fixed Tile) cùng lúc quay 1 vòng ngược chiều kim đồng hồ + nhấc lên,
//   xong cả cụm Fixed Tile (4 đinh + tấm back) mờ về 0. Nối vào TileView.fixedRoot / fixedBreakAnim.
// Tools ▸ WordStack ▸ Build Fixed Tile Break Animation. Sửa thẳng asset Tile.prefab (LoadPrefabContents →
// SaveAsPrefabAsset): ĐÓNG Prefab Mode trước. Chạy lại = dựng lại từ đầu, ghi đè số đã chỉnh trong Inspector.
// Hoàn tác bằng git.
using System.Collections.Generic;
using FigmaMotion;
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using UnityEditor;
using UnityEngine;
using static WordStack.Board.Editor.AnimationBuildKit;

namespace WordStack.Board.Editor
{
    public static class FixedTileAnimationBuilder
    {
        const string TilePath = "Assets/Prefabs/Tile.prefab";
        const string UndoName = "Build Fixed Tile Break Animation";
        const string NailPrefix = "Blocker - Fixed Tile";

        const float SpinDur = 0.6f, Lift = 0.3f, FadeDur = 0.2f;

        [MenuItem("Tools/WordStack/Build Fixed Tile Break Animation")]
        static void Menu()
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == TilePath)
            { Debug.LogError("[FixedTile] Đóng Prefab Mode của " + TilePath + " trước khi chạy tool."); return; }

            var tile = PrefabUtility.LoadPrefabContents(TilePath);
            try
            {
                Build(tile.GetComponent<TileView>());
                PrefabUtility.SaveAsPrefabAsset(tile, TilePath);
                Debug.Log("[FixedTile] " + TilePath + ": đã dựng tháo đinh.");
            }
            finally { PrefabUtility.UnloadPrefabContents(tile); }
        }

        /// <summary>Dựng fixedBreakAnim trên Fixed Tile của thẻ đang mở (LoadPrefabContents hoặc Prefab Mode).</summary>
        public static void Build(TileView tv)
        {
            var root = FindDeep(tv.transform, "Fixed Tile");
            if (root == null) throw new System.InvalidOperationException("Không thấy con \"Fixed Tile\" trong Tile.prefab.");
            var nails = new List<Transform>();
            foreach (Transform c in root) if (c.name.StartsWith(NailPrefix)) nails.Add(c);
            if (nails.Count != 4)
                throw new System.InvalidOperationException("Fixed Tile phải có đúng 4 đinh \"" + NailPrefix + "…\", đang có " + nails.Count + ".");

            var anim = root.GetComponent<LitMotionAnimation>();
            if (anim == null) anim = root.gameObject.AddComponent<LitMotionAnimation>();

            var parts = new List<(LitMotionAnimationComponent, System.Action<SerializedProperty>)>();
            foreach (var n in nails)
            {
                var nail = n;
                parts.Add((new TransformRotationAnimation(), c =>
                {
                    Common(c, nail.name + " · vặn", nail, true);
                    var s = c.FindPropertyRelative("settings");
                    s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                    s.FindPropertyRelative("endValue").vector3Value = new Vector3(0f, 0f, 360f);   // z+ = ngược chiều kim đồng hồ
                    Timing(s, SpinDur, 0f, Ease.InOutSine);
                }));
                parts.Add((new TransformPositionAnimation(), c =>
                {
                    Common(c, nail.name + " · nhấc", nail, true);
                    var s = c.FindPropertyRelative("settings");
                    s.FindPropertyRelative("startValue").vector3Value = Vector3.zero;
                    s.FindPropertyRelative("endValue").vector3Value = new Vector3(0f, Lift, 0f);
                    Timing(s, SpinDur, 0f, Ease.OutCubic);
                }));
            }
            parts.Add((new SpriteGroupAlphaAnimation(), c =>
            {
                Common(c, "Fixed Tile · mờ (đinh + back)", root, false);
                var s = c.FindPropertyRelative("settings");
                s.FindPropertyRelative("startValue").floatValue = 1f;
                s.FindPropertyRelative("endValue").floatValue = 0f;
                Timing(s, FadeDur, SpinDur, Ease.OutQuad);
            }));
            Write(anim, UndoName, parts.ToArray());

            var so = new SerializedObject(tv);
            so.FindProperty("fixedRoot").objectReferenceValue = root.gameObject;
            so.FindProperty("fixedBreakAnim").objectReferenceValue = anim;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
