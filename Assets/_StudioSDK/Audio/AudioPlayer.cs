using Reflex.Attributes;
using UnityEngine;

namespace LogosSDK.Audio
{
    // Phát một SFX dựng trong Inspector. Port AudioPlayer của Mukbang, đổi AudioClip sang clip id
    // của IAudioService. Loop: Play lại thì dừng lượt cũ (không chồng loop), object tắt thì tự dừng.
    public sealed class AudioPlayer : MonoBehaviour
    {
        public enum AutoPlay { None, OnStart, OnEnable }

        [SerializeField] private string _clipId;
        [SerializeField] private bool _loop;
        [SerializeField] private AutoPlay _autoPlay = AutoPlay.None;

        [Inject] private IAudioService _audio;
        private ulong _handle;
        private bool _playOnStart;

        private void OnEnable()
        {
            if (_autoPlay != AutoPlay.OnEnable) return;
            // Lần bật đầu tiên chạy trước khi Reflex inject (scene load / Instantiate) → dời sang Start.
            if (_audio != null) Play();
            else _playOnStart = true;
        }

        private void Start()
        {
            if (_autoPlay == AutoPlay.OnStart || _playOnStart) Play();
        }

        private void OnDisable()
        {
            if (_loop) Stop();
        }

        public void Play()
        {
            if (_audio == null)
            {
                Debug.LogWarning($"[AudioPlayer] {name} chưa được inject IAudioService.", this);
                return;
            }
            if (_loop) Stop();
            _handle = _audio.PlaySFX(_clipId, _loop);
        }

        public void Stop()
        {
            if (_audio == null || _handle == 0) return;
            _audio.StopSFX(_handle);
            _handle = 0;
        }
    }
}
