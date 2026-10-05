namespace LogosSDK.Audio
{
    public interface IAudioService
    {
        // Sound (SFX) và Music bật/tắt riêng. IsMuted = cả hai tắt; SetMuted bật/tắt cả hai.
        bool IsSoundEnabled { get; }
        bool IsMusicEnabled { get; }
        void SetSoundEnabled(bool enabled);
        void SetMusicEnabled(bool enabled);
        bool IsMuted { get; }
        void SetMuted(bool muted);

        // Trả handle > 0 để StopSFX; 0 = không phát được. Gọi cũ PlaySFX("id") vẫn compile.
        ulong PlaySFX(string clipId, bool loop = false);
        bool StopSFX(ulong handle);
        void StopSFX(string clipId);
        void StopAllSFX();

        void PlayMusic(string clipId);
        void StopMusic();
        void SetSFXVolume(float volume);
        void SetMusicVolume(float volume);
    }
}
