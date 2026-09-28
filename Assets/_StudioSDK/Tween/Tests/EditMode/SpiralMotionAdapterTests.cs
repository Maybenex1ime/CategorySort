using LitMotion;
using NUnit.Framework;
using UnityEngine;

namespace LogosSDK.Tween.Tests.EditMode
{
    [Category("UnitTest")]
    public class SpiralMotionAdapterTests
    {
        // Số liệu thật của TimelineSuccess bên Mukbang (Tray_0/Slot): xoáy 1.5 vòng vào giữa bàn.
        static readonly Vector3 Start = new Vector3(-3f, 0f, 0f);
        static readonly Vector3 End = new Vector3(0f, -2.75f, 0f);

        static Vector3 Eval(Vector3 start, Vector3 end, float progress, SpiralOption.OrbitPlane plane = SpiralOption.OrbitPlane.XY)
        {
            var adapter = default(SpiralMotionAdapter);
            var options = new SpiralOption(1.5f, plane);
            return adapter.Evaluate(ref start, ref end, ref options, new MotionEvaluationContext { Progress = progress });
        }

        [Test]
        public void AtProgressZero_ReturnsStartValue()
        {
            Assert.That(Vector3.Distance(Eval(Start, End, 0f), Start), Is.LessThan(1e-4f));
        }

        [Test]
        public void AtProgressOne_ReturnsEndValue()
        {
            Assert.That(Vector3.Distance(Eval(Start, End, 1f), End), Is.LessThan(1e-4f));
        }

        [Test]
        public void AtHalfway_RadiusIsHalfTheInitialRadius()
        {
            float initial = Vector3.Distance(Start, End);
            Assert.That(Vector3.Distance(Eval(Start, End, 0.5f), End), Is.EqualTo(initial * 0.5f).Within(1e-4f));
        }

        [Test]
        public void StartEqualsEnd_ReturnsEndValue()
        {
            Assert.That(Eval(End, End, 0.3f), Is.EqualTo(End));
        }

        [Test]
        public void XZPlane_KeepsEndY()
        {
            var start = new Vector3(2f, 5f, 0f);
            var end = new Vector3(0f, 1f, 0f);
            Assert.That(Eval(start, end, 0.4f, SpiralOption.OrbitPlane.XZ).y, Is.EqualTo(1f).Within(1e-5f));
        }
    }
}
