using Reflex.Core;
using Reflex.Enums;
using LogosSDK.UI.Core;
using UnityEngine;
using UnityEngine.Serialization;
using Resolution = Reflex.Enums.Resolution;

namespace LogosSDK.UI.Installers
{
    public sealed class UIInstaller : MonoBehaviour, IInstaller
    {
        [FormerlySerializedAs("uiManager")]
        [SerializeField] private UIManager _uiManager;

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            if (_uiManager != null)
            {
                containerBuilder.RegisterValue(_uiManager);
                containerBuilder.RegisterType(typeof(NavigationService), new[] { typeof(INavigationService) }, Lifetime.Singleton, Resolution.Lazy);
            }
        }
    }
}
