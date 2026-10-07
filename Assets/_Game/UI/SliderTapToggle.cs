using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LogosGame.Features.UI
{
    /// Công tắc on/off dựng bằng Slider 0/1: bấm vào BẤT KỲ đâu trên công tắc là đảo trạng thái.
    /// Slider gốc nhảy về giá trị gần chỗ bấm nhất, nên bấm vào nửa đang bật thì không đổi gì
    /// (feedback #1). Tắt phần bắt input của Slider (enabled = false: value vẫn đổi, onValueChanged
    /// vẫn bắn, không bị tint mờ như interactable = false) và tự nhận click thay.
    /// Gắn cùng GameObject với Slider.
    [RequireComponent(typeof(Slider))]
    public sealed class SliderTapToggle : MonoBehaviour, IPointerClickHandler
    {
        private Slider _slider;
        private Slider Slider => _slider != null ? _slider : (_slider = GetComponent<Slider>());

        private void Awake() => Slider.enabled = false;   // Slider chỉ còn vẽ + giữ value

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var s = Slider;
            s.value = s.value > (s.minValue + s.maxValue) / 2f ? s.minValue : s.maxValue;
        }
    }
}
