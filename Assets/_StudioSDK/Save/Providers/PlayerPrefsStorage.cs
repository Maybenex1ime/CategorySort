using System;
using UnityEngine;
using Newtonsoft.Json;
using LogosSDK.Core.Logging;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.Save.Providers
{
    public class PlayerPrefsStorage : IStorageProvider
    {
        private static readonly ILogger Logger = LogManager.GetLogger<PlayerPrefsStorage>();

        public void Save<T>(string key, T data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                PlayerPrefs.SetString(key, json);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save to PlayerPrefs with key: {key}. Error: {ex.Message}");
            }
        }

        public T Load<T>(string key, T defaultValue)
        {
            if (PlayerPrefs.HasKey(key))
            {
                try
                {
                    var json = PlayerPrefs.GetString(key);
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load from PlayerPrefs with key: {key}. Error: {ex.Message}");
                }
            }
            return defaultValue;
        }

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public void Delete(string key) => PlayerPrefs.DeleteKey(key);

        public void Flush() => PlayerPrefs.Save();
    }
}
