using System;

namespace LogosGame.Save.Data
{
    [Serializable]
    public class SettingsData
    {
        public int SchemaVersion = 1;
        public float SoundVolume = 1.0f;
        public float MusicVolume = 1.0f;
        public bool VibrationEnabled = true;
        public string LanguageCode = "en";
    }
}
