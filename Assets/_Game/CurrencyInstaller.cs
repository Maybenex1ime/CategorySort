using System;
using LogosMeta.Economy;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta
{
    public sealed class CurrencyInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField, Min(0)] private int _initialCoins;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(new CurrencySettings(_initialCoins), new[] { typeof(CurrencySettings) });

            builder.RegisterType(typeof(CurrencyService),
                new[] { typeof(ICurrencyService), typeof(IDisposable) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
