// Curve dựng từ keyframe Figma phải trùng số Figma, không xấp xỉ.
// Số chuẩn của cubic-bezier(0, 0, 0.58, 1) giải số x(u) = t (plan 2026-09-27, Global Constraints).
using FigmaMotion;
using NUnit.Framework;

namespace WordStack.Board.Tests
{
    public class FigmaEaseTests
    {
        [TestCase(0.25f, 0.3781f)]
        [TestCase(0.5f, 0.6846f)]
        [TestCase(0.75f, 0.9065f)]
        public void CssEaseOut01_MatchesCubicBezier(float t, float expected)
        {
            Assert.AreEqual(expected, FigmaEase.CssEaseOut01().Evaluate(t), 0.005f);
        }

        [Test]
        public void EaseOut_MultiSegment_HitsEveryKeyAndEasesEachSegment()
        {
            var c = FigmaEase.EaseOut((0f, 0f), (0.5f, 0.8f), (1f, 1f));
            Assert.AreEqual(0.8f, c.Evaluate(0.5f), 1e-4f);
            Assert.AreEqual(1f, c.Evaluate(1f), 1e-4f);
            Assert.AreEqual(0.8f * 0.6846f, c.Evaluate(0.25f), 0.005f);   // giữa đoạn 1
        }

        [Test]
        public void Linear_InterpolatesStraightBetweenSamples()
        {
            var c = FigmaEase.Linear((0f, 0f), (0.2f, 0.5f), (1f, 1f));
            Assert.AreEqual(0.25f, c.Evaluate(0.1f), 1e-4f);
            Assert.AreEqual(0.75f, c.Evaluate(0.6f), 1e-4f);
        }

        // Số chuẩn giải số x(u) = t của từng cubic-bezier CSS (không phải xấp xỉ).
        [TestCase("ease-in", 0.25f, 0.0935f)]
        [TestCase("ease-in", 0.5f, 0.3154f)]
        [TestCase("ease-in", 0.75f, 0.6219f)]
        [TestCase("ease-in-out", 0.25f, 0.1292f)]
        [TestCase("Ease In Out", 0.5f, 0.5f)]
        [TestCase("ease_in_out", 0.75f, 0.8708f)]
        [TestCase("cubic-bezier(0, 0, 0.58, 1)", 0.25f, 0.3781f)]
        [TestCase("cubic-bezier(0.34, 1.56, 0.64, 1)", 0.5f, 1.0874f)]   // overshoot: y vượt 1
        public void Curve_MatchesCssCubicBezier(string ease, float t, float expected)
        {
            Assert.IsTrue(FigmaEase.TryParse(ease, out var b), ease);
            var c = FigmaEase.Curve(new[] { (0f, 0f), (1f, 1f) }, new[] { b });
            Assert.AreEqual(expected, c.Evaluate(t), 0.005f);
        }

        [Test]
        public void Curve_EachSegmentUsesItsOwnEaseInsideItsOwnBox()
        {
            Assert.IsTrue(FigmaEase.TryParse("ease-in-out", out var inOut));
            var c = FigmaEase.Curve(new[] { (0f, 0f), (0.2f, 1f), (0.6f, 3f) }, new[] { FigmaEase.LinearBezier, inOut });
            Assert.AreEqual(0.5f, c.Evaluate(0.1f), 1e-4f);          // đoạn 1 thẳng
            Assert.AreEqual(2f, c.Evaluate(0.4f), 0.005f);           // giữa đoạn 2: ease-in-out đối xứng
            Assert.AreEqual(1f + 2f * 0.1292f, c.Evaluate(0.3f), 0.005f);
        }

        [TestCase("")]
        [TestCase("bounce")]
        [TestCase("cubic-bezier(0, 0, 1)")]
        [TestCase("cubic-bezier(1.2, 0, 0.5, 1)")]   // x ngoài 0..1 — CSS không cho
        [TestCase("cubic-bezier(a, 0, 0.5, 1)")]
        public void TryParse_RejectsUnknown(string s)
        {
            Assert.IsFalse(FigmaEase.TryParse(s, out _));
        }
    }
}
