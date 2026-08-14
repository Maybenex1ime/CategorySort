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
        private int _sfxRoundRobin;
        private float _sfxVolume = 1f;
        private float _musicVolume = 1f;
        private string _currentMusicId;
        private bool _isMuted;
        private bool _disposed;

        public AudioService(AudioCatalog catalog, ISaveManager saveManager)
        {
            _catalog = catalog;
            _saveManager = saveManager;
            IndexCatalog();
            LoadMutedFromSettings();
        }

        public bool IsMuted => _isMuted;

        public void SetMuted(bool muted)
        {
            if (_isMuted == muted) return;
            _isMuted = muted;

            if (_isMuted)
            {
                if (_musicSource != null) _musicSource.Stop();
            }
            else if (!string.IsNullOrEmpty(_currentMusicId))
            {
                EnsureRoot();
                var clip = LoadClip(_currentMusicId, _musicRefs);
                if (clip != null)
                {
                    _musicSource.clip = clip;
                    _musicSource.volume = _musicVolume;
                    _musicSource.Play();
                }
            }

            PersistMuted();
        }

        public void PlaySFX(string clipId)
        {
            if (_disposed || _isMuted || string.IsNullOrEmpty(clipId)) return;
            EnsureRoot();
            var clip = LoadClip(clipId, _sfxRefs);
            if (clip == null) return;
            var src = _sfxSources[_sfxRoundRobin];
            _sfxRoundRobin = (_sfxRoundRobin + 1) % SfxSourceCount;
            src.PlayOneShot(clip, _sfxVolume);
        }

        public void PlayMusic(string clipId)
        {
            if (_disposed || string.IsNullOrEmpty(clipId)) return;
            bool sameAsCurrent = string.Equals(_currentMusicId, clipId, StringComparison.Ordinal);
            // Remember the request so we can resume on unmute, even if currently muted.
            _currentMusicId = clipId;
            if (_isMuted) return;
            if (sameAsCurrent && _musicSource != null && _musicSource.isPlaying) return;
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

        private void LoadMutedFromSettings()
        {
            if (_saveManager == null) return;
            try
            {
                var settings = _saveManager.Load<SettingsData>();
                if (settings == null) return;
                // Treat both volumes <= 0 as "muted" for backward compatibility.
                _isMuted = settings.SoundVolume <= 0f && settings.MusicVolume <= 0f;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to load audio mute setting: " + ex.Message);
            }
        }

        private void PersistMuted()
        {
            if (_saveManager == null) return;
            try
            {
                var settings = _saveManager.Load<SettingsData>() ?? new SettingsData();
                settings.SoundVolume = _isMuted ? 0f : 1f;
                settings.MusicVolume = _isMuted ? 0f : 1f;
                _saveManager.Save(settings);
                // Save() only marks dirty; force a flush so toggles survive app quit.
                _saveManager.SaveAll();
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to persist audio mute setting: " + ex.Message);
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
            for (int i = 0; i < SfxSourceCount; i++)
            {
                var s = _root.AddComponent<AudioSource>();
                s.playOnAwake = false;
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
