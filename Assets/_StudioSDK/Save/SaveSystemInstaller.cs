using LogosSDK.Save.Providers;
using Reflex.Core;
using UnityEngine;

namespace LogosSDK.Save
{
    public class SaveSystemInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterType(typeof(SafeFileWriter), new[] { typeof(SafeFileWriter) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Eager);
            
            // Register providers as their concrete types so GameSaveInstaller can inject them
            builder.RegisterType(typeof(JsonFileStorage), new[] { typeof(JsonFileStorage) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Eager);
            builder.RegisterType(typeof(EncryptedFileStorage), new[] { typeof(EncryptedFileStorage) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Eager);
            builder.RegisterType(typeof(PlayerPrefsStorage), new[] { typeof(PlayerPrefsStorage) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Eager);
            
            // SaveManager acts as the primary orchestrator
            builder.RegisterType(typeof(SaveManager), new[] { typeof(ISaveManager), typeof(SaveManager) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Eager);
        }
    }
}
