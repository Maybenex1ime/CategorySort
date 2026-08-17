using System;
using LogosGame.Save.Data;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using LogosSDK.Services;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Gameplay.Services.Impl
{
    public class HapticService : IHapticService
    {
        private static readonly ILogger Logger = LogManager.GetLogger<HapticService>();

        private readonly ISaveManager _saveManager;
        private bool _isEnabled = true;

        public HapticService(ISaveManager saveManager)
        {
            _saveManager = saveManager;
            LoadEnabledFromSettings();
        }

        public bool IsEnabled => _isEnabled;

        public void SetEnabled(bool enabled)
        {
            if (_isEnabled == enabled) return;
            _isEnabled = enabled;
            PersistEnabled();
        }

        public void Play(HapticLevel level)
        {
            if (!_isEnabled) return;
            // Handheld chỉ tồn tại trên build mobile — Standalone/Editor không có class này.
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private void LoadEnabledFromSettings()
        {
            if (_saveManager == null) return;
            try
            {
                var settings = _saveManager.Load<SettingsData>();
                if (settings != null) _isEnabled = settings.VibrationEnabled;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to load haptic setting: " + ex.Message);
            }
        }

        private void PersistEnabled()
        {
            if (_saveManager == null) return;
            try
            {
                var settings = _saveManager.Load<SettingsData>() ?? new SettingsData();
                settings.VibrationEnabled = _isEnabled;
                _saveManager.Save(settings);
                // Save() only marks dirty; force a flush so toggles survive app quit.
                _saveManager.SaveAll();
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to persist haptic setting: " + ex.Message);
            }
        }
    }
}
