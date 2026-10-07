// Feedback #1: bấm đâu trên công tắc Settings cũng đảo on/off.
using LogosGame.Features.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordStack.Meta.Tests
{
    public class SliderTapToggleTests
    {
        GameObject _go;
        Slider _slider;
        SliderTapToggle _toggle;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("switch", typeof(RectTransform));
            _slider = _go.AddComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.wholeNumbers = true;
            _toggle = _go.AddComponent<SliderTapToggle>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        static PointerEventData Click(PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            return new PointerEventData(null) { button = button };
        }

        [Test]
        public void EachTapFlipsAndFiresOnce()
        {
            int fired = 0;
            _slider.onValueChanged.AddListener(_ => fired++);

            _toggle.OnPointerClick(Click());
            Assert.AreEqual(1f, _slider.value);
            _toggle.OnPointerClick(Click());
            Assert.AreEqual(0f, _slider.value);
            Assert.AreEqual(2, fired, "mỗi lần bấm bắn đúng một onValueChanged");
        }

        [Test]
        public void RightClickDoesNothing()
        {
            _toggle.OnPointerClick(Click(PointerEventData.InputButton.Right));
            Assert.AreEqual(0f, _slider.value);
        }
    }
}
