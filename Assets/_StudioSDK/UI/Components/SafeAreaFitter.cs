using UnityEngine;

namespace LogosSDK.UI.Components
{
    /// <summary>
    /// Kéo RectTransform khớp Screen.safeArea — né tai thỏ, lỗ camera, thanh home indicator ở cả bốn cạnh.
    /// Gắn lên một panel mà CHA phủ kín màn hình (anchor tính theo tỉ lệ màn hình). Nền (BG) đặt NGOÀI
    /// panel này để vẫn tràn toàn màn; chỉ nút/HUD cần bấm mới nằm trong.
    /// Tự tính lại khi safe area đổi: xoay 180°, chia đôi màn hình, Device Simulator.
    /// Chỉ chạy lúc Play — không ghi đè anchor của prefab trong Edit mode.
    /// Không gắn lên root của Screen/Popup: transition của ScreenBase tween chính RectTransform đó.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreen;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            // Rẻ: chỉ so hai struct mỗi frame, tính lại khi thật sự đổi.
            if (Screen.safeArea != _appliedSafeArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
                Apply();
        }

        private void Apply()
        {
            _appliedSafeArea = Screen.safeArea;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (!ComputeAnchors(_appliedSafeArea, Screen.width, Screen.height, out var min, out var max)) return;

            var rt = (RectTransform)transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Đổi safe area (pixel) thành anchor [0,1] theo kích thước màn hình.
        /// Trả false (anchor full 0..1) khi dữ liệu không hợp lệ — vd frame đầu một số máy Android báo 0.
        /// </summary>
        public static bool ComputeAnchors(Rect safeArea, int screenWidth, int screenHeight, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || safeArea.width <= 0f || safeArea.height <= 0f)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return false;
            }

            anchorMin = new Vector2(Mathf.Clamp01(safeArea.xMin / screenWidth), Mathf.Clamp01(safeArea.yMin / screenHeight));
            anchorMax = new Vector2(Mathf.Clamp01(safeArea.xMax / screenWidth), Mathf.Clamp01(safeArea.yMax / screenHeight));
            return true;
        }
    }
}
