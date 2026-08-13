using LogosSDK.Core.AppFlow;
using LogosSDK.Save;
using LogosSDK.UI.Core;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta.AppFlow.Installers
{
    /// <summary>
    /// Gắn lên SceneScope, CẠNH UIInstaller — không đặt lên ProjectScope.prefab
    /// vì manager cần UIManager, mà UIManager là object trong scene.
    /// </summary>
    public sealed class AppFlowInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField, Min(0f)] private float _minLoadingSeconds = 2f;

        public void InstallBindings(ContainerBuilder builder)
        {
            // Lazy: UIManager do UIInstaller đăng ký, thứ tự component trên
            // SceneScope không đảm bảo. AppBootstrap resolve ở Start nên đủ trễ.
            builder.RegisterFactory<IAppFlowManager>(
                c => new WordStackAppFlowManager(
                    c.Resolve<UIManager>(),
                    _minLoadingSeconds,
                    c.Resolve<ISaveManager>(),
                    c.Resolve<ICoinRewardService>()),
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
