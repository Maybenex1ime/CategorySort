using LogosSDK.UI.Components;
using NUnit.Framework;
using UnityEngine;

namespace LogosSDK.UI.Tests.EditMode
{
    [Category("UnitTest")]
    public class SafeAreaFitterTests
    {
        const float Eps = 1e-5f;

        static void AssertAnchors(Rect safeArea, int w, int h, Vector2 expectedMin, Vector2 expectedMax)
        {
            bool ok = SafeAreaFitter.ComputeAnchors(safeArea, w, h, out var min, out var max);
            Assert.IsTrue(ok);
            Assert.That(min.x, Is.EqualTo(expectedMin.x).Within(Eps), "anchorMin.x");
            Assert.That(min.y, Is.EqualTo(expectedMin.y).Within(Eps), "anchorMin.y");
            Assert.That(max.x, Is.EqualTo(expectedMax.x).Within(Eps), "anchorMax.x");
            Assert.That(max.y, Is.EqualTo(expectedMax.y).Within(Eps), "anchorMax.y");
        }

        [Test]
        public void FullScreenSafeArea_StretchesZeroToOne()
        {
            AssertAnchors(new Rect(0, 0, 1080, 2340), 1080, 2340, Vector2.zero, Vector2.one);
        }

        [Test]
        public void TopNotch_LowersOnlyTopEdge()
        {
            AssertAnchors(new Rect(0, 0, 1080, 2250), 1080, 2340, Vector2.zero, new Vector2(1f, 2250f / 2340f));
        }

        [Test]
        public void NotchAndHomeIndicator_InsetsTopAndBottom()
        {
            // iPhone dọc: tai thỏ trên 132px, home indicator dưới 102px.
            AssertAnchors(new Rect(0, 102, 1170, 2532 - 102 - 132), 1170, 2532,
                          new Vector2(0f, 102f / 2532f), new Vector2(1f, (2532f - 132f) / 2532f));
        }

        [Test]
        public void SideInsets_InsetsLeftAndRight()
        {
            AssertAnchors(new Rect(132, 0, 2400 - 264, 1080), 2400, 1080,
                          new Vector2(132f / 2400f, 0f), new Vector2((2400f - 132f) / 2400f, 1f));
        }

        [Test]
        public void InvalidInput_ReturnsFalseAndFullStretch()
        {
            bool ok = SafeAreaFitter.ComputeAnchors(new Rect(0, 0, 0, 0), 1080, 2340, out var min, out var max);
            Assert.IsFalse(ok);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);

            Assert.IsFalse(SafeAreaFitter.ComputeAnchors(new Rect(0, 0, 1080, 2340), 0, 2340, out _, out _));
        }
    }
}
