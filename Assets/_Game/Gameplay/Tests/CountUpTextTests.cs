// CountUpText: số coin đếm dần thay vì nhảy (report UI animation #2).
using LitMotion;
using LogosGame.Features.UI.Common;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    public class CountUpTextTests
    {
        GameObject _go;
        TextMeshProUGUI _text;
        ManualMotionDispatcher _clock;
        CountUpText _count;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("count");
            _text = _go.AddComponent<TextMeshProUGUI>();
            _clock = new ManualMotionDispatcher();
            _count = new CountUpText(_text, 1f, _clock.Scheduler);
        }

        [TearDown]
        public void TearDown()
        {
            _count.Dispose();
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void FirstValueShowsImmediately()
        {
            _count.Set(120);
            Assert.AreEqual("120", _text.text);
        }

        [Test]
        public void LaterValueCountsUp()
        {
            _count.Set(100);
            _count.Set(200);
            _clock.Update(0.5);
            Assert.That(_count.Shown, Is.GreaterThan(100).And.LessThan(200), "giữa chừng phải là số trung gian");
            _clock.Update(0.6);
            Assert.AreEqual("200", _text.text);
        }

        [Test]
        public void NewTargetMidwayContinuesFromShownNumber()
        {
            _count.Set(0);
            _count.Set(100);
            _clock.Update(0.5);
            int mid = _count.Shown;
            _count.Set(50);
            Assert.AreEqual(mid, _count.Shown, "đổi đích không được nhảy số");
            _clock.Update(1.1);
            Assert.AreEqual("50", _text.text);
        }

        [Test]
        public void SetImmediateCancelsCounting()
        {
            _count.Set(0);
            _count.Set(100);
            _count.SetImmediate(7);
            _clock.Update(2);
            Assert.AreEqual("7", _text.text);
        }
    }
}
