using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using LogosSDK.Core.Logging;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.Save.Providers
{
    public class JsonFileStorage : IStorageProvider
    {
        protected static readonly ILogger Logger = LogManager.GetLogger<JsonFileStorage>();
        protected readonly SafeFileWriter _writer;
        protected readonly string _folderPath;

        public JsonFileStorage(SafeFileWriter writer)
        {
            _writer = writer;
            _folderPath = Path.Combine(Application.persistentDataPath, "SaveData");
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }
        }

        protected string GetFilePath(string key)
        {
            return Path.Combine(_folderPath, $"{key}.json");
        }

        public virtual void Save<T>(string key, T data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                _writer.Write(GetFilePath(key), json);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save {typeof(T).Name} to JsonFileStorage key {key}: {ex.Message}");
            }
        }

        public virtual T Load<T>(string key, T defaultValue)
        {
            var json = _writer.Read(GetFilePath(key));
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to deserialize {typeof(T).Name} from JsonFileStorage key {key}: {ex.Message}");
                }
            }
            return defaultValue;
        }

        public bool HasKey(string key)
        {
            var path = GetFilePath(key);
            return File.Exists(path) || File.Exists(path + ".bak");
        }

        public void Delete(string key)
        {
            _writer.Delete(GetFilePath(key));
        }

        public void Flush()
        {
            // JsonFileStorage writes immediately on Save, or SafeFileWriter does.
        }
    }
}
