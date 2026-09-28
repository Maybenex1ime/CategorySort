// Opacity cả group = alpha author của TỪNG renderer × một hệ số chung (không ghi đè về cùng một alpha).
using FigmaMotion;
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Board.Tests
{
    public class SpriteGroupAlphaAnimationTests
    {
        [Test]
        public void ApplyAlpha_ScalesEachRendererFromItsOwnAuthorAlpha()
        {
            var a = new GameObject("a").AddComponent<SpriteRenderer>();
            var b = new GameObject("b").AddComponent<SpriteRenderer>();
            try
            {
                var rs = new[] { a, b };
                var baseAlpha = new[] { 1f, 0.5f };

                SpriteGroupAlphaAnimation.ApplyAlpha(rs, baseAlpha, 0.5f);
                Assert.AreEqual(0.5f, a.color.a, 1e-5f);
                Assert.AreEqual(0.25f, b.color.a, 1e-5f);

                SpriteGroupAlphaAnimation.ApplyAlpha(rs, baseAlpha, 1f);   // về lại author
                Assert.AreEqual(1f, a.color.a, 1e-5f);
                Assert.AreEqual(0.5f, b.color.a, 1e-5f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a.gameObject);
                UnityEngine.Object.DestroyImmediate(b.gameObject);
            }
        }
    }
}
