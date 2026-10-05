using System;
using System.Collections.Generic;
using LogosGame.Features.Gameplay.Content.Audio;
using LogosGame.Save.Data;
using LogosSDK.Audio;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using ILogger = LogosSDK.Core.Logging.ILogger;
using Object = UnityEngine.Object;

namespace LogosGame.Features.Gameplay.Services.Impl
{
    /// <summary>
    /// Real audio service. Loads clips from <see cref="AudioCatalog"/> via Addressables on first play,
    /// caches the handle, and routes SFX through a small AudioSource pool.
    /// </summary>
    public class AudioService : IAudioService, IDisposable
    {
        private const int SfxSourceCount = 8;
        private static readonly ILogger Logger = LogManager.GetLogger<AudioService>();

        private readonly AudioCatalog _catalog;
        private readonly ISaveManager _saveManager;
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _handles =
            new Dictionary<string, AsyncOperationHandle<AudioClip>>(StringComparer.Ordinal);
        private readonly Dictionary<string, AssetReferenceT<AudioClip>> _sfxRefs =
            new Dictionary<string, AssetReferenceT<AudioClip>>(StringComparer.Ordinal);
        private readonly Dictionary<string, AssetReferenceT<AudioClip>> _musicRefs =
            new Dictionary<string, AssetReferenceT<AudioClip>>(StringComparer.Ordinal);

        private GameObject _root;
        private AudioSource[] _sfxSources;
        private AudioSource _musicSource;
        private bool[] _sfxBusy;
        private ulong[] _sfxPlayOrder;   // handle của lượt phát đang giữ source; 0 = chưa phát
        private string[] _sfxClipIds;
        private ulong _nextHandle = 1;
        private float _sfxVolume = 1f;
        private float _musicVolume = 1f;
        private string _currentMusicId;
        private bool _soundEnabled = true;
        private bool _musicEnabled = true;
        private bool _disposed;

        public AudioService(AudioCatalog catalog, ISaveManager saveManager)
        {
            _catalog = catalog;
            _saveManager = saveManager;
            IndexCatalog();
            LoadSettings();
        }

        public bool IsSoundEnabled => _soundEnabled;
        public bool IsMusicEnabled => _musicEnabled;
        public bool IsMuted => !_soundEnabled && !_musicEnabled;

        public void SetMuted(bool muted)
        {
            SetSoundEnabled(!muted);
            SetMusicEnabled(!muted);
        }

        public void SetSoundEnabled(bool enabled)
        {
            if (_soundEnabled == enabled) return;
            _soundEnabled = enabled;
            if (!enabled) StopAllSFX();
            PersistSettings();
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (_musicEnabled == enabled) return;
            _musicEnabled = enabled;
            if (!enabled)
            {
                if (_musicSource != null) _musicSource.Stop();
            }
            else if (!string.IsNullOrEmpty(_currentMusicId))
            {
                StartMusic(_currentMusicId);
            }
            PersistSettings();
        }

        public ulong PlaySFX(string clipId, bool loop = false)
        {
            if (_disposed || !_soundEnabled || string.IsNullOrEmpty(clipId)) return 0;
            EnsureRoot();
            var clip = LoadClip(clipId, _sfxRefs);
            if (clip == null) return 0;

            for (int i = 0; i < SfxSourceCount; i++) _sfxBusy[i] = _sfxSources[i].isPlaying;
            int idx = SfxVoicePicker.Pick(_sfxBusy, _sfxPlayOrder);

            var src = _sfxSources[idx];
            src.Stop();
            src.clip = clip;
            src.loop = loop;
            src.volume = _sfxVolume;
            src.Play();

            ulong handle = _nextHandle++;
            _sfxPlayOrder[idx] = handle;
            _sfxClipIds[idx] = clipId;
            return handle;
        }

        public bool StopSFX(ulong handle)
        {
            if (handle == 0 || _sfxSources == null || _root == null) return false;
            for (int i = 0; i < SfxSourceCount; i++)
            {
                if (_sfxPlayOrder[i] != handle || !_sfxSources[i].isPlaying) continue;
                _sfxSources[i].Stop();
                return true;
            }
            return false;
        }

        public void StopSFX(string clipId)
        {
            if (string.IsNullOrEmpty(clipId) || _sfxSources == null || _root == null) return;
            for (int i = 0; i < SfxSourceCount; i++)
            {
                if (string.Equals(_sfxClipIds[i], clipId, StringComparison.Ordinal)) _sfxSources[i].Stop();
            }
        }

        public void StopAllSFX()
        {
            if (_sfxSources == null || _root == null) return;
            for (int i = 0; i < SfxSourceCount; i++) _sfxSources[i].Stop();
        }

        public void PlayMusic(string clipId)
        {
            if (_disposed || string.IsNullOrEmpty(clipId)) return;
            bool sameAsCurrent = string.Equals(_currentMusicId, clipId, StringComparison.Ordinal);
            // Nhớ bài được yêu cầu để bật Music lại thì phát tiếp, kể cả khi đang tắt.
            _currentMusicId = clipId;
            if (!_musicEnabled) return;
            if (sameAsCurrent && _musicSource != null && _musicSource.isPlaying) return;
            StartMusic(clipId);
        }

        private void StartMusic(string clipId)
        {
            if (_disposed) return;
            EnsureRoot();
            var clip = LoadClip(clipId, _musicRefs);
            if (clip == null) return;
            _musicSource.clip = clip;
            _musicSource.volume = _musicVolume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _currentMusicId = null;
            if (_musicSource == null) return;
            _musicSource.Stop();
        }

        private void LoadSettings()
        {
            if (_saveManager == null) return;
            try
            {
                var settings = _saveManager.Load<SettingsData>();
                if (settings == null) return;
                // Save cũ (mute chung) ghi cả hai = 0 → đọc ra tắt cả hai, tương thích ngược.
                _soundEnabled = settings.SoundVolume > 0f;
                _musicEnabled = settings.MusicVolume > 0f;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to load audio settings: " + ex.Message);
            }
        }

        private void PersistSettings()
        {
            if (_saveManager == null) return;
            try
            {
                var settings = _saveManager.Load<SettingsData>() ?? new SettingsData();
                settings.SoundVolume = _soundEnabled ? 1f : 0f;
                settings.MusicVolume = _musicEnabled ? 1f : 0f;
                _saveManager.Save(settings);
                // Save() only marks dirty; force a flush so toggles survive app quit.
                _saveManager.SaveAll();
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to persist audio settings: " + ex.Message);
            }
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            if (_sfxSources == null) return;
            for (int i = 0; i < _sfxSources.Length; i++) _sfxSources[i].volume = _sfxVolume;
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (_musicSource != null) _musicSource.volume = _musicVolume;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_musicSource != null) _musicSource.Stop();
            foreach (var kvp in _handles)
            {
                if (kvp.Value.IsValid()) Addressables.Release(kvp.Value);
            }
            _handles.Clear();
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
            _sfxSources = null;
            _musicSource = null;
        }

        private void IndexCatalog()
        {
            if (_catalog == null)
            {
                Logger.Warn("AudioCatalog is null — audio will be silent.");
                return;
            }
            var sfx = _catalog.SfxEntries;
            for (int i = 0; i < sfx.Count; i++)
            {
                var e = sfx[i];
                if (e != null && !string.IsNullOrEmpty(e.ClipId)) _sfxRefs[e.ClipId] = e.ClipRef;
            }
            var music = _catalog.MusicEntries;
            for (int i = 0; i < music.Count; i++)
            {
                var e = music[i];
                if (e != null && !string.IsNullOrEmpty(e.ClipId)) _musicRefs[e.ClipId] = e.ClipRef;
            }
        }

        private void EnsureRoot()
        {
            if (_root != null) return;
            _root = new GameObject("AudioRoot");
            Object.DontDestroyOnLoad(_root);
            _sfxSources = new AudioSource[SfxSourceCount];
            _sfxBusy = new bool[SfxSourceCount];
            _sfxPlayOrder = new ulong[SfxSourceCount];
            _sfxClipIds = new string[SfxSourceCount];
            for (int i = 0; i < SfxSourceCount; i++)
            {
                var s = _root.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = false;
                s.volume = _sfxVolume;
                _sfxSources[i] = s;
            }
            _musicSource = _root.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume;
        }

        private AudioClip LoadClip(string clipId, Dictionary<string, AssetReferenceT<AudioClip>> table)
        {
            if (_handles.TryGetValue(clipId, out var existing))
            {
                return existing.IsValid() ? existing.Result : null;
            }
            if (!table.TryGetValue(clipId, out var aref) || aref == null || !aref.RuntimeKeyIsValid())
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Audio clip '" + clipId + "' not in catalog.");
                return null;
            }
            var handle = Addressables.LoadAssetAsync<AudioClip>(aref.RuntimeKey);
            var clip = handle.WaitForCompletion();
            if (clip == null)
            {
                Logger.Warn("Failed to load audio clip '" + clipId + "'.");
                Addressables.Release(handle);
                return null;
            }
            _handles[clipId] = handle;
            return clip;
        }
    }
}
