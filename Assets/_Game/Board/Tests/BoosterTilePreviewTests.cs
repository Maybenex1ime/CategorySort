// Chuỗi xem trước hiệu ứng thẻ booster (BoosterTilePreview) — giá trị đầu / cuối từng nhịp phải khớp
// đúng thông số trong SO_BoosterAnim.
using NUnit.Framework;
using UnityEngine;
using WordStack.Board.Editor;

namespace WordStack.Board.Tests
{
    public class BoosterTilePreviewTests
    {
        static BoosterAnimSettings NewSettings()
        {
            var a = ScriptableObject.CreateInstance<BoosterAnimSettings>();
            a.magnetPopDur = 0.1f; a.magnetPopScale = 1.2f;
            a.magnetFlyDur = 0.4f; a.magnetGatherScale = 1.6f;
            a.magnetHold = 0.1f; a.magnetBurstDur = 0.2f;
            a.magnetSpin = 90f;
            return a;
        }

        static float ScaleAt(BoosterTilePreview.Step[] steps, float t)
        {
            BoosterTilePreview.Sample(steps, t, out float scale, out _);
            return scale;
        }

        [Test]
        public void MagnetTile_PopsGrowsHoldsThenBurstsToZero()
        {
            var a = NewSettings();
            var steps = BoosterTilePreview.MagnetTile(a);

            Assert.AreEqual(0.8f, BoosterTilePreview.TotalDuration(steps), 1e-4f);
            Assert.AreEqual(1f, ScaleAt(steps, 0f), 1e-4f, "bắt đầu ở cỡ gốc");
            Assert.AreEqual(1.2f, ScaleAt(steps, 0.1f), 1e-4f, "hết nhịp phồng = magnetPopScale");
            Assert.AreEqual(1.6f, ScaleAt(steps, 0.55f), 1e-4f, "đang đứng ở điểm hội tụ = magnetGatherScale");

            Assert.IsFalse(BoosterTilePreview.Sample(steps, 1f, out float end, out float spin), "qua hết chuỗi → false");
            Assert.AreEqual(0f, end, 1e-4f, "nổ về 0");
            Assert.AreEqual(90f, spin, 1e-4f, "xoay tới magnetSpin và giữ nguyên");
            Object.DestroyImmediate(a);
        }

        [Test]
        public void MagnetTile_SpinOffWhenTiny()
        {
            var a = NewSettings();
            a.magnetSpin = 0.005f;
            BoosterTilePreview.Sample(BoosterTilePreview.MagnetTile(a), 0.3f, out _, out float spin);
            Assert.AreEqual(0f, spin, 1e-6f, "|magnetSpin| ≤ 0.01 là tắt, như BoardController");
            Object.DestroyImmediate(a);
        }

        [Test]
        public void ParentUndoReveal_EndAtNormalSize()
        {
            var a = NewSettings();
            foreach (var steps in new[]
                     { BoosterTilePreview.ParentTile(a), BoosterTilePreview.UndoTile(a), BoosterTilePreview.RevealTile(a) })
            {
                Assert.IsFalse(BoosterTilePreview.Sample(steps, 10f, out float scale, out _));
                Assert.AreEqual(1f, scale, 1e-4f, "kết thúc về cỡ gốc");
            }
            Assert.AreEqual(0f, ScaleAt(BoosterTilePreview.ParentTile(a), 0f), 1e-4f, "thẻ cha nở từ 0");
            Assert.AreEqual(0f, ScaleAt(BoosterTilePreview.RevealTile(a), 0f), 1e-4f, "thẻ hiện ra từ 0");
            Object.DestroyImmediate(a);
        }
    }
}
