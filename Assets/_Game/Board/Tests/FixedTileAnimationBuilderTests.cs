// Tool tháo đinh — chạy trên bản mở tạm của Tile.prefab thật, KHÔNG lưu.
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
    public class FixedTileAnimationBuilderTests
    {
        [Test]
        public void Build_SpinsAndLiftsFourNailsThenFadesAndWires()
        {
            var tile = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Tile.prefab");
            try
            {
                var tv = tile.GetComponent<TileView>();
                FixedTileAnimationBuilder.Build(tv);

                var so = new SerializedObject(tv);
                var root = (GameObject)so.FindProperty("fixedRoot").objectReferenceValue;
                var anim = (LitMotionAnimation)so.FindProperty("fixedBreakAnim").objectReferenceValue;
                Assert.AreEqual("Fixed Tile", root.name);
                Assert.AreSame(root, anim.gameObject, "anim nằm trên chính Fixed Tile");
                Assert.AreEqual(9, anim.Components.Count, "4 × (vặn + nhấc) + 1 mờ");
                Assert.AreEqual(4, anim.Components.OfType<TransformRotationAnimation>().Count());
                Assert.AreEqual(4, anim.Components.OfType<TransformPositionAnimation>().Count());
                Assert.IsTrue(anim.Components[8] is SpriteGroupAlphaAnimation, "mờ cả cụm ở cuối");
                Assert.IsFalse(root.activeSelf, "prefab giữ Fixed Tile tắt — chỉ bật khi thẻ bị đóng đinh");
            }
            finally { PrefabUtility.UnloadPrefabContents(tile); }
        }
    }
}
