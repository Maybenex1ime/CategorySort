using System;
using LogosMeta.Economy;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta
{
    public sealed class CurrencyInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField, Min(0)] private int _initialCoins;
        [SerializeField, Min(0)] private int _coinsPerWin = 50;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(new CurrencySettings(_initialCoins), new[] { typeof(CurrencySettings) });
            builder.RegisterValue(new CoinRewardSettings(_coinsPerWin), new[] { typeof(CoinRewardSettings) });

            builder.RegisterType(typeof(CurrencyService),
                new[] { typeof(ICurrencyService), typeof(IDisposable) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            // Lazy bắt buộc, xem chú thích trong CoinRewardService. MetaSession ép dựng.
            builder.RegisterType(typeof(CoinRewardService),
                new[] { typeof(ICoinRewardService), typeof(IDisposable) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
