using LogosGame.Features.Gameplay.Content.Audio;
using LogosGame.Features.Gameplay.Services.Impl;
using LogosGame.Save.Data;
using LogosSDK.Save;
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    /// <summary>
    /// Sound và Music là hai công tắc riêng (port SettingData.SoundEnable/MusicEnable của Mukbang),
    /// lưu vào SettingsData cũ: bật ⇔ volume > 0. Không test phát âm: chỉ đụng phần setting,
    /// service không tạo AudioSource nào.
    /// </summary>
    public sealed class AudioServiceSettingsTests
    {
        private AudioCatalog _catalog;

        [SetUp]
        public void SetUp() => _catalog = ScriptableObject.CreateInstance<AudioCatalog>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_catalog);

        [Test]
        public void Load_TatSoundBatMusic_DocDungTungKenh()
        {
            var audio = new AudioService(_catalog, new FakeSave(new SettingsData { SoundVolume = 0f, MusicVolume = 1f }));

            Assert.That(audio.IsSoundEnabled, Is.False);
            Assert.That(audio.IsMusicEnabled, Is.True);
            Assert.That(audio.IsMuted, Is.False);
        }

        [Test]
        public void Load_SaveCuMuteCaHai_IsMuted()
        {
            var audio = new AudioService(_catalog, new FakeSave(new SettingsData { SoundVolume = 0f, MusicVolume = 0f }));

            Assert.That(audio.IsMuted, Is.True);
        }

        [Test]
        public void SetSoundEnabled_ChiGhiKenhSound()
        {
            var save = new FakeSave(new SettingsData());
            var audio = new AudioService(_catalog, save);

            audio.SetSoundEnabled(false);

            Assert.That(save.Data.SoundVolume, Is.EqualTo(0f));
            Assert.That(save.Data.MusicVolume, Is.EqualTo(1f));
            Assert.That(save.SaveCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetMusicEnabled_GiaTriKhongDoi_KhongGhiDia()
        {
            var save = new FakeSave(new SettingsData());
            var audio = new AudioService(_catalog, save);

            audio.SetMusicEnabled(true);

            Assert.That(save.SaveCalls, Is.EqualTo(0));
        }

        [Test]
        public void SetMuted_TatCaHaiKenh()
        {
            var save = new FakeSave(new SettingsData());
            var audio = new AudioService(_catalog, save);

            audio.SetMuted(true);

            Assert.That(audio.IsSoundEnabled, Is.False);
            Assert.That(audio.IsMusicEnabled, Is.False);
            Assert.That(save.Data.SoundVolume, Is.EqualTo(0f));
            Assert.That(save.Data.MusicVolume, Is.EqualTo(0f));
        }

        [Test]
        public void PlaySFX_SoundTat_TraVe0()
        {
            var audio = new AudioService(_catalog, new FakeSave(new SettingsData { SoundVolume = 0f }));

            Assert.That(audio.PlaySFX("ui_button_click"), Is.EqualTo(0UL));
            Assert.That(GameObject.Find("AudioRoot"), Is.Null);
        }

        [Test]
        public void PlayMusic_MusicTat_KhongTaoAudioRoot()
        {
            var audio = new AudioService(_catalog, new FakeSave(new SettingsData { MusicVolume = 0f }));

            audio.PlayMusic("bgm_main");

            Assert.That(GameObject.Find("AudioRoot"), Is.Null);
        }

        // Load trả đúng instance Data nên AudioService sửa thẳng vào nó.
        private sealed class FakeSave : ISaveManager
        {
            public readonly SettingsData Data;
            public int SaveCalls;

            public FakeSave(SettingsData data) => Data = data;

            public void Register<T>(IStorageProvider provider, string key) where T : class, new() { }
            public T Load<T>() where T : class, new() => Data as T;
            public void Save<T>(T data) where T : class, new() => SaveCalls++;
            public void SaveImmediate<T>(T data) where T : class, new() => SaveCalls++;
            public void SaveAll() { }
            public void DeleteDomain<T>() where T : class, new() { }
            public bool HasDomain<T>() where T : class, new() => true;
        }
    }
}
