namespace LogosGame.Features.Gameplay.Services
{
    /// <summary>
    /// Subscribes to gameplay SO Event Channels and orchestrates visual + audio feedback.
    /// </summary>
    public interface IFeedbackDispatcher
    {
        void Initialize();
        void Dispose();

        /// <summary>Plays the UI button click SFX. Call from any View on user button press.</summary>
        void PlayUiButtonClick();
    }
}
