using LogosSDK.Core.Events;
using LogosSDK.Core.Services;
using LogosSDK.Core.Services.Impl;
using Reflex.Core;
using UnityEngine;

namespace LogosSDK.Core.Installers
{
    public class ProjectInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            // LogManager is static — used via LogManager.GetLogger<T>(), not injected
            containerBuilder.RegisterType(typeof(GlobalBus), new[] { typeof(IEventBus) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Eager);
            containerBuilder.RegisterType(typeof(AddressableAssetService), new[] { typeof(IAssetService) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Eager);
        }
    }
}
