using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>
    /// Báo khi người chơi bắt đầu giữ / thả tay trên vùng này (cần một Graphic bật
    /// Raycast Target trên cùng GameObject). Chỉ tính ngón đầu tiên.
    /// </summary>
    public sealed class PressHoldArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event Action HoldStarted;
        public event Action HoldEnded;

        private int _pointerId = int.MinValue;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            _pointerId = eventData.pointerId;
            HoldStarted?.Invoke();
        }

        // Unity gửi PointerUp về đúng object nhận PointerDown, kể cả khi thả tay ở ngoài.
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        // Popup bị tắt giữa lúc đang giữ → không bao giờ có PointerUp, tự nhả.
        private void OnDisable() => Release();

        private void Release()
        {
            if (_pointerId == int.MinValue) return;
            _pointerId = int.MinValue;
            HoldEnded?.Invoke();
        }
    }
}
