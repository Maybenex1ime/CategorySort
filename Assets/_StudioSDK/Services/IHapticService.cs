namespace LogosSDK.Services
{
    public interface IHapticService
    {
        bool IsEnabled { get; }
        void SetEnabled(bool enabled);
        void Play(HapticLevel level);
    }
}
