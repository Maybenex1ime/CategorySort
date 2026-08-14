using LogosSDK.Audio;
using LogosSDK.Services;

namespace LogosGame.Features.Gameplay.Services
{
    /// <summary>
    /// Bản WordStack của FeedbackDispatcher. Bên aquapark nó còn nghe cả chục
    /// SO event channel gameplay (float/belt/queue) — WordStack không có các
    /// channel đó nên chỉ giữ phần phản hồi UI. Clip id "ui_button_click" giữ
    /// đúng chuỗi aquapark dùng: điền entry đó vào SO_AudioCatalog là có tiếng,
    /// không phải sửa code.
    /// </summary>
    public sealed class WordStackFeedbackDispatcher : IFeedbackDispatcher
    {
        private readonly IAudioService _audio;
        private readonly IHapticService _haptic;

        public WordStackFeedbackDispatcher(IAudioService audio, IHapticService haptic)
        {
            _audio = audio;
            _haptic = haptic;
        }

        public void Initialize() { }

        public void Dispose() { }

        public void PlayUiButtonClick()
        {
            // Catalog chưa có entry thì AudioService tự no-op (debug log) — an toàn.
            _audio?.PlaySFX("ui_button_click");

            if (_haptic != null && _haptic.IsEnabled)
                _haptic.Play(HapticLevel.Light);
        }
    }
}
