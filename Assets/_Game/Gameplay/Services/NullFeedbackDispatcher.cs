using LogosSDK.Core.Logging;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Gameplay.Services
{
    /// <summary>
    /// Placeholder cho tầng phản hồi (âm thanh + rung). Giữ đúng điểm gọi như
    /// aquapark để khi WordStack có audio thì chỉ thay implement, không phải đi
    /// sửa lại các View.
    ///
    /// TODO: nối vào IAudioService + IHapticService khi port tầng audio.
    /// </summary>
    public sealed class NullFeedbackDispatcher : IFeedbackDispatcher
    {
        private static readonly ILogger _logger = LogManager.GetLogger<NullFeedbackDispatcher>();

        public void Initialize() { }

        public void Dispose() { }

        public void PlayUiButtonClick()
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("[Feedback] UI button click — chưa có audio.");
        }
    }
}
