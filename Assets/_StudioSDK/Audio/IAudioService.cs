namespace LogosSDK.Audio
{
    public interface IAudioService
    {
        bool IsMuted { get; }
        void SetMuted(bool muted);
        void PlaySFX(string clipId);
        void PlayMusic(string clipId);
        void StopMusic();
        void SetSFXVolume(float volume);
        void SetMusicVolume(float volume);
    }
}
