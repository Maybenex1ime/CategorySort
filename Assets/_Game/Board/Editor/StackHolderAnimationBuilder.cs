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
using UnityEngine.Rendering;
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

        // Cả cụm TileHolders là một SortingGroup: trên nền/khay/bóng của hộp (5–7), dưới thẻ thật (10+).
        public const int HolderGroupOrder = 8;
        // Thứ tự trong nhóm (chỉ so giữa các Peek): Peek1 nền 4 / mini 8; Peek k ≥ 2 nền −(2k − 3) / mini −(2k − 4).
        public static int BgOrder(int k) { return k == 1 ? 4 : -(2 * k - 3); }
        public static int MiniOrder(int k) { return k == 1 ? 8 : -(2 * k - 4); }

        [MenuItem("Tools/WordStack/Build Stack Holder Animations")]
        static void Menu()
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && (stage.assetPath == StackPath || stage.assetPath == TilePath))
            { Debug.LogError("[StackHolder] Đóng Prefab Mode của " + stage.assetPath + " trước khi chạy tool."); return; }
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

            var group = FindDeep(sv.transform, "Peek1").parent;
            var sg = group.GetComponent<SortingGroup>();
            if (sg == null) sg = group.gameObject.AddComponent<SortingGroup>();
            sg.sortingOrder = HolderGroupOrder;
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
