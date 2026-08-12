using LogosSDK.UI.Animation;
using Reflex.Core;
using UnityEngine;

namespace LogosSDK.UI.Installers
{
    public sealed class UIAnimationInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private UIAnimationSettingsSO _animationSettings;

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            if (_animationSettings == null) return;

            containerBuilder.RegisterValue(_animationSettings);
            containerBuilder.RegisterType(typeof(UIAnimationService), new[] { typeof(IUIAnimationService) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Lazy);
        }
    }
}
