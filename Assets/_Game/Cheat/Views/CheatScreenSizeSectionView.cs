using System.Collections.Generic;
using LogosGame.Features.Gameplay.Content;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.Cheat.Views
{
    /// <summary>
    /// SCREEN section: một nút mỗi size Android trong <see cref="CheatSettings.ScreenPresets"/>.
    /// Bấm là đổi cửa sổ build Windows sang ĐÚNG TỈ LỆ của máy đó để soi layout UI.
    /// Size thật lớn hơn màn PC (1080×2400 cao hơn mọi màn 1080p) nên cửa sổ được thu nhỏ
    /// giữ nguyên tỉ lệ cho vừa màn — Canvas Scaler chỉ quan tâm tỉ lệ, không quan tâm pixel.
    ///
    /// Chỉ có tác dụng trên BUILD: trong Editor Screen.SetResolution không làm gì — dùng
    /// dropdown size của Game view. Không giả lập tai thỏ / Screen.safeArea.
    /// </summary>
    public sealed class CheatScreenSizeSectionView : MonoBehaviour
    {
        [Inject] private readonly CheatSettings _cheatSettings;

        [Tooltip("Nút mẫu (để inactive). Mỗi preset nhân bản một nút, chữ ghi vào TMP_Text con đầu tiên.")]
        [SerializeField] private Button _buttonTemplate;
        [SerializeField] private Transform _buttonContainer;
        [Tooltip("Trả cửa sổ về size lúc mở game. Để trống nếu không cần.")]
        [SerializeField] private Button _resetButton;
        [Tooltip("Hiện size đang áp. Để trống nếu không cần.")]
        [SerializeField] private TMP_Text _currentLabel;
        [SerializeField] private GameObject _sectionRoot;

        // Phần màn PC cửa sổ được chiếm tối đa — chừa thanh tiêu đề + taskbar.
        private const float ScreenFill = 0.9f;

        private readonly List<Button> _buttons = new List<Button>();
        private int _startWidth, _startHeight;

        private void Start()
        {
            _startWidth = Screen.width;
            _startHeight = Screen.height;

            var presets = _cheatSettings != null ? _cheatSettings.ScreenPresets : null;
            if (presets == null || presets.Count == 0)
            {
                if (_sectionRoot != null) _sectionRoot.SetActive(false);
                else gameObject.SetActive(false);
                return;
            }

            if (_buttonTemplate != null && _buttonContainer != null)
            {
                _buttonTemplate.gameObject.SetActive(false);
                foreach (CheatSettings.ScreenPreset preset in presets)
                {
                    Button b = Instantiate(_buttonTemplate, _buttonContainer);
                    b.gameObject.SetActive(true);
                    b.name = "Screen " + preset.Width + "x" + preset.Height;
                    var text = b.GetComponentInChildren<TMP_Text>(true);
                    if (text != null) text.text = preset.Label + "\n" + preset.Width + "x" + preset.Height;
                    CheatSettings.ScreenPreset captured = preset;
                    b.onClick.AddListener(() => Apply(captured.Label, captured.Width, captured.Height));
                    _buttons.Add(b);
                }
            }

            if (_resetButton != null) _resetButton.onClick.AddListener(OnResetClicked);
            ShowLabel("Start", _startWidth, _startHeight, _startWidth, _startHeight);
        }

        private void OnDestroy()
        {
            foreach (Button b in _buttons) if (b != null) b.onClick.RemoveAllListeners();
            if (_resetButton != null) _resetButton.onClick.RemoveListener(OnResetClicked);
        }

        private void OnResetClicked() => Apply("Start", _startWidth, _startHeight);

        private void Apply(string label, int width, int height)
        {
            Resolution display = Screen.currentResolution;
            Vector2Int fit = FitInside(width, height,
                Mathf.RoundToInt(display.width * ScreenFill), Mathf.RoundToInt(display.height * ScreenFill));

#if UNITY_EDITOR
            Debug.LogWarning("[Cheat Screen] Editor không đổi được cửa sổ — chọn size " + width + "x" + height +
                             " ở dropdown Game view. Cheat này chỉ có tác dụng trên build.");
#else
            Screen.SetResolution(fit.x, fit.y, FullScreenMode.Windowed);
#endif
            ShowLabel(label, width, height, fit.x, fit.y);
        }

        private void ShowLabel(string label, int width, int height, int windowW, int windowH)
        {
            if (_currentLabel == null) return;
            string ratio = (height * 9f / Mathf.Max(width, 1)).ToString("0.#") + ":9";
            _currentLabel.text = label + " " + width + "x" + height + " (" + ratio + ")" +
                                 (windowW != width || windowH != height ? " -> window " + windowW + "x" + windowH : "");
        }

        /// <summary>Thu nhỏ (không phóng to) width×height giữ tỉ lệ cho vừa maxW×maxH.</summary>
        internal static Vector2Int FitInside(int width, int height, int maxW, int maxH)
        {
            float scale = Mathf.Min(1f, (float)maxW / Mathf.Max(width, 1), (float)maxH / Mathf.Max(height, 1));
            return new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(width * scale)),
                                  Mathf.Max(1, Mathf.RoundToInt(height * scale)));
        }
    }
}
