using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LogosSDK.Audio.Tests.EditMode
{
    [Category("UnitTest")]
    public class AudioPlayerTests
    {
        private GameObject _go;
        private AudioPlayer _player;
        private FakeAudio _audio;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AudioPlayerTest");
            _player = _go.AddComponent<AudioPlayer>();
            _audio = new FakeAudio();
            Set("_clipId", "sfx_flame");
            Set("_audio", _audio);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private void Set(string field, object value) =>
            typeof(AudioPlayer).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_player, value);

        [Test]
        public void Play_GoiPlaySFXVoiIdVaLoop()
        {
            Set("_loop", true);

            _player.Play();

            Assert.That(_audio.Played, Is.EqualTo(new List<(string, bool)> { ("sfx_flame", true) }));
        }

        [Test]
        public void Stop_DungDungHandleVuaPhat()
        {
            _player.Play();
            _player.Stop();

            Assert.That(_audio.Stopped, Is.EqualTo(new List<ulong> { 1UL }));
        }

        [Test]
        public void Loop_PlayLanHai_DungLuotTruoc_KhongChongLoop()
        {
            Set("_loop", true);

            _player.Play();
            _player.Play();

            Assert.That(_audio.Stopped, Is.EqualTo(new List<ulong> { 1UL }));
            Assert.That(_audio.Played.Count, Is.EqualTo(2));
        }

        [Test]
        public void Stop_ChuaPhat_KhongGoiService()
        {
            _player.Stop();

            Assert.That(_audio.Stopped, Is.Empty);
        }

        [Test]
        public void ChuaInject_Play_ChiCanhBao()
        {
            Set("_audio", null);
            LogAssert.Expect(LogType.Warning, "[AudioPlayer] AudioPlayerTest chưa được inject IAudioService.");

            _player.Play();
        }

        private sealed class FakeAudio : IAudioService
        {
            public readonly List<(string, bool)> Played = new List<(string, bool)>();
            public readonly List<ulong> Stopped = new List<ulong>();
            private ulong _next = 1;

            public ulong PlaySFX(string clipId, bool loop = false)
            {
                Played.Add((clipId, loop));
                return _next++;
            }

            public bool StopSFX(ulong handle)
            {
                Stopped.Add(handle);
                return true;
            }

            public bool IsSoundEnabled => true;
            public bool IsMusicEnabled => true;
            public bool IsMuted => false;
            public void SetSoundEnabled(bool enabled) { }
            public void SetMusicEnabled(bool enabled) { }
            public void SetMuted(bool muted) { }
            public void StopSFX(string clipId) { }
            public void StopAllSFX() { }
            public void PlayMusic(string clipId) { }
            public void StopMusic() { }
            public void SetSFXVolume(float volume) { }
            public void SetMusicVolume(float volume) { }
        }
    }
}
