using LogosGame.Features.Gameplay.Content.Audio;
using LogosGame.Features.Gameplay.Services.Impl;
using LogosSDK.Audio;
using LogosSDK.Services;
using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

namespace LogosGame.Audio
{
    /// <summary>
    /// Project-scope installer for IAudioService and IHapticService.
    /// MUST live on the ProjectScope prefab (Assets/Prefabs/ProjectScope.prefab),
    /// not on a per-scene installer — Reflex scene scopes are siblings, so a
    /// service registered in one scene is not visible from another.
    /// </summary>
    public sealed class AudioServicesInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private AudioCatalog _audioCatalog;

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterValue(_audioCatalog, new[] { typeof(AudioCatalog) });

            containerBuilder.RegisterType(typeof(AudioService),
                new[] { typeof(IAudioService) },
                Lifetime.Singleton, Resolution.Lazy);

            containerBuilder.RegisterType(typeof(HapticService),
                new[] { typeof(IHapticService) },
                Lifetime.Singleton, Resolution.Lazy);
        }
    }
}
