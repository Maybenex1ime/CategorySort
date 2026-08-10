using System;
using System.Collections.Generic;
using System.Linq;
using LogosSDK.Core.Logging;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.Save
{
    public class SaveManager : ISaveManager
    {
        private static readonly ILogger Logger = LogManager.GetLogger<SaveManager>();

        private class DomainEntry
        {
            public IStorageProvider Provider;
            public string Key;
            public object CachedData;
            public bool IsDirty;
            public List<object> Migrators = new();
        }

        private readonly Dictionary<Type, DomainEntry> _domains = new();

        public void Register<T>(IStorageProvider provider, string key) where T : class, new()
        {
            var type = typeof(T);
            if (_domains.ContainsKey(type))
            {
                Logger.Warn($"Domain {type.Name} is already registered.");
                return;
            }

            var entry = new DomainEntry
            {
                Provider = provider,
                Key = key,
                IsDirty = false
            };

            var data = provider.Load<T>(key, new T());

            // Get SchemaVersion dynamically and run migrations if necessary.
            // Note: In a production scenario, you would structure Migrators more robustly.
            
            entry.CachedData = data;
            _domains[type] = entry;

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug($"Registered Save Domain: {type.Name} with key '{key}'");
            }
        }

        public T Load<T>() where T : class, new()
        {
            var type = typeof(T);
            if (_domains.TryGetValue(type, out var entry))
            {
                return (T)entry.CachedData;
            }

            Logger.Error($"Attempted to load unregistered domain: {type.Name}");
            return new T();
        }

        public void Save<T>(T data) where T : class, new()
        {
            var type = typeof(T);
            if (_domains.TryGetValue(type, out var entry))
            {
                entry.CachedData = data;
                entry.IsDirty = true;
            }
            else
            {
                Logger.Error($"Attempted to save unregistered domain: {type.Name}");
            }
        }

        public void SaveImmediate<T>(T data) where T : class, new()
        {
            var type = typeof(T);
            if (!_domains.TryGetValue(type, out var entry))
            {
                Logger.Error($"Attempted to SaveImmediate unregistered domain: {type.Name}");
                return;
            }

            entry.CachedData = data;
            try
            {
                entry.Provider.Save(entry.Key, data);
                entry.IsDirty = false;

                if (Logger.IsDebugEnabled)
                    Logger.Debug($"SaveImmediate domain: {type.Name}");
            }
            catch (Exception ex)
            {
                entry.IsDirty = true;
                Logger.Error($"Failed to SaveImmediate domain {type.Name}: {ex.Message}");
            }
        }

        public void SaveAll()
        {
            foreach (var kvp in _domains)
            {
                var entry = kvp.Value;
                if (entry.IsDirty)
                {
                    try
                    {
                        // Reflection needed because original generic type is lost in list
                        entry.Provider.GetType()
                            .GetMethod("Save")
                            ?.MakeGenericMethod(kvp.Key)
                            .Invoke(entry.Provider, new object[] { entry.Key, entry.CachedData });

                        entry.IsDirty = false;
                        
                        if (Logger.IsDebugEnabled)
                            Logger.Debug($"Saved dirty domain: {kvp.Key.Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to save domain {kvp.Key.Name}: {ex.Message}");
                    }
                }
            }
        }

        public void DeleteDomain<T>() where T : class, new()
        {
            var type = typeof(T);
            if (_domains.TryGetValue(type, out var entry))
            {
                entry.Provider.Delete(entry.Key);
                entry.CachedData = new T();
                entry.IsDirty = true;
                Logger.Debug($"Deleted domain data: {type.Name}");
            }
        }

        public bool HasDomain<T>() where T : class, new()
        {
            return _domains.ContainsKey(typeof(T));
        }
    }
}
