// Tool mờ tấm nền booster — chạy trên object tạm, không đụng scene thật.
using LitMotion.Animation;
using LitMotion.Animation.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordStack.Board.Editor;

namespace WordStack.Board.Tests
{
    public class BoosterBackdropAnimationBuilderTests
    {
        GameObject board, backdrop;

        [TearDown]
        public void TearDown()
        {
            if (board != null) Object.DestroyImmediate(board);
            if (backdrop != null) Object.DestroyImmediate(backdrop);
        }

        static float Value(LitMotionAnimation anim, string field)
        {
            var c = new SerializedObject(anim).FindProperty("components").GetArrayElementAtIndex(0);
            return c.FindPropertyRelative("settings").FindPropertyRelative(field).floatValue;
        }

        [Test]
        public void Build_AddsFadeInAndOutOnBackdropAndWiresFields()
        {
            board = new GameObject("board");
            var bc = board.AddComponent<BoardController>();
            backdrop = new GameObject("backdrop");
            var so = new SerializedObject(bc);
            so.FindProperty("boosterBackdrop").objectReferenceValue = backdrop;
            so.ApplyModifiedPropertiesWithoutUndo();

            BoosterBackdropAnimationBuilder.Build(bc);
            BoosterBackdropAnimationBuilder.Build(bc);   // chạy lại không đẻ thêm component

            Assert.IsNotNull(backdrop.GetComponent<CanvasGroup>(), "thiếu CanvasGroup thì tool thêm");
            var anims = backdrop.GetComponents<LitMotionAnimation>();
            Assert.AreEqual(2, anims.Length);

            so.Update();
            var fadeIn = (LitMotionAnimation)so.FindProperty("backdropFadeIn").objectReferenceValue;
            var fadeOut = (LitMotionAnimation)so.FindProperty("backdropFadeOut").objectReferenceValue;
            Assert.AreSame(anims[0], fadeIn);
            Assert.AreSame(anims[1], fadeOut);
            foreach (var a in anims)
            {
                Assert.AreEqual(1, a.Components.Count);
                Assert.IsTrue(a.Components[0] is CanvasGroupAlphaAnimation);
                Assert.AreEqual(BoosterBackdropAnimationBuilder.FadeDur, Value(a, "duration"), 1e-6f);
            }
            Assert.AreEqual(0f, Value(fadeIn, "startValue"));
            Assert.AreEqual(1f, Value(fadeIn, "endValue"));
            Assert.AreEqual(1f, Value(fadeOut, "startValue"));
            Assert.AreEqual(0f, Value(fadeOut, "endValue"));
        }
    }
}
