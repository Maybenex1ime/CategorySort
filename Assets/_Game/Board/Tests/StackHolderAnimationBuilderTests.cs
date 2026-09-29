// Tool dựng animation Stack Tile Holder — chạy trên bản mở tạm của prefab thật, KHÔNG lưu.
using System.Linq;
using FigmaMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordStack.Board.Editor;

namespace WordStack.Board.Tests
{
    public class StackHolderAnimationBuilderTests
    {
        [Test]
        public void Orders_FollowSpecTable()
        {
            int[] bg = { 4, -1, -3, -5, -7 }, mini = { 8, 0, -2, -4, -6 };
            for (int k = 1; k <= 5; k++)
            {
                Assert.AreEqual(bg[k - 1], StackHolderAnimationBuilder.BgOrder(k), "nền Peek" + k);
                Assert.AreEqual(mini[k - 1], StackHolderAnimationBuilder.MiniOrder(k), "mini Peek" + k);
            }
        }

        [Test]
        public void BuildStack_WiresFiveHoldersWithFillAndLift()
        {
            var box = AssetDatabase.LoadAssetAtPath<BoxView>("Assets/Prefabs/Box.prefab");
            var tile = AssetDatabase.LoadAssetAtPath<TileView>("Assets/Prefabs/Tile.prefab");
            var stack = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Stack.prefab");
            try
            {
                var sv = stack.GetComponent<StackView>();
                StackHolderAnimationBuilder.BuildStack(sv, box, tile);

                var holders = new SerializedObject(sv).FindProperty("holders");
                Assert.AreEqual(StackView.HolderCount, holders.arraySize);
                for (int k = 0; k < holders.arraySize; k++)
                {
                    var h = holders.GetArrayElementAtIndex(k);
                    var fill = (LitMotionAnimation)h.FindPropertyRelative("fill").objectReferenceValue;
                    var lift = (LitMotionAnimation)h.FindPropertyRelative("lift").objectReferenceValue;
                    Assert.AreEqual(8, fill.Components.Count, "Peek" + (k + 1) + ": 4 × (bay + scale)");
                    Assert.AreEqual(4, fill.Components.OfType<FlyToTargetAnimation>().Count());
                    Assert.AreEqual(2, lift.Components.Count, "nhấc + mờ");
                    var minis = h.FindPropertyRelative("minis");
                    for (int i = 0; i < 4; i++)
                    {
                        var m = (GameObject)minis.GetArrayElementAtIndex(i).objectReferenceValue;
                        Assert.AreEqual("Mini " + i, m.name);
                        Assert.AreEqual(StackHolderAnimationBuilder.MiniOrder(k + 1), m.GetComponent<SpriteRenderer>().sortingOrder);
                    }
                    var root = (GameObject)h.FindPropertyRelative("root").objectReferenceValue;
                    Assert.AreEqual(StackHolderAnimationBuilder.BgOrder(k + 1), root.transform.Find("Bg").GetComponent<SpriteRenderer>().sortingOrder);
                }
                var sg = sv.transform.Find("TileHolders").GetComponent<UnityEngine.Rendering.SortingGroup>();
                Assert.AreEqual(StackHolderAnimationBuilder.HolderGroupOrder, sg.sortingOrder, "cụm holder trên hộp, dưới thẻ");
                Assert.IsNull(sv.BoxAnchor.GetComponentInChildren<BoxView>(true), "hộp đo tạm đã xoá");
            }
            finally { PrefabUtility.UnloadPrefabContents(stack); }
        }

        [Test]
        public void BuildTileReveal_FadesArtAndPunches()
        {
            var tile = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Tile.prefab");
            try
            {
                var tv = tile.GetComponent<TileView>();
                StackHolderAnimationBuilder.BuildTileReveal(tv);
                var reveal = (LitMotionAnimation)new SerializedObject(tv).FindProperty("revealAnim").objectReferenceValue;
                Assert.IsNotNull(reveal);
                Assert.AreEqual(2, reveal.Components.Count);
                Assert.IsTrue(reveal.Components[0] is SpriteGroupAlphaAnimation);
                Assert.IsTrue(reveal.Components[1] is TransformScalePunchAnimation);
            }
            finally { PrefabUtility.UnloadPrefabContents(tile); }
        }
    }
}
