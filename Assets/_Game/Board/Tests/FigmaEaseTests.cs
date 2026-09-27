// Curve dựng từ keyframe Figma phải trùng số Figma, không xấp xỉ.
// Số chuẩn của cubic-bezier(0, 0, 0.58, 1) giải số x(u) = t (plan 2026-09-27, Global Constraints).
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
    }
}
