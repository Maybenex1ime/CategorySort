using System;
using LogosGame.Features.Cheat.Services;
using LogosGame.Features.Cheat.Services.Impl;
using LogosGame.Features.Gameplay.Content;
using LogosMeta.CheatPanel;
using Reflex.Core;
using UnityEngine;

namespace LogosGame.Features.Cheat
{
    /// <summary>
    /// Registers <see cref="CheatService"/> at ProjectScope so the cheat HUD can
    /// reach it from every scene. Goes on the ProjectScope prefab alongside the
    /// other root installers.
    /// </summary>
    public sealed class CheatInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private CheatSettings _cheatSettings;

        public void InstallBindings(ContainerBuilder builder)
        {
            if (_cheatSettings != null)
                builder.RegisterValue(_cheatSettings, new[] { typeof(CheatSettings), typeof(ICheatPanelConfig) });

            // Must be Lazy: CheatService depends on IHeartService + ICurrencyService.
            // Eager resolution would force those to construct during container build,
            // before GameSaveInstaller.OnContainerBuilt registers HeartData/CurrencyData
            // domains — causing "unregistered domain" errors at startup.
            builder.RegisterType(typeof(CheatService),
                new[] { typeof(ICheatService), typeof(IEconomyCheatActions), typeof(ICheatNotificationSource), typeof(IDisposable) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
