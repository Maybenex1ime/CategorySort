using System;
using LogosGame.Features.Currency.Services.Impl;
using LogosGame.Features.Currency.Transactions;
using LogosMeta.Economy;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta
{
    public sealed class CurrencyInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField, Min(0)] private int _initialCoins;
        [SerializeField, Min(0)] private int _coinsPerWin = 50;
        [SerializeField] private TransactionCatalog _catalog;

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

            // Hệ mua chỉ sống khi catalog được gán (menu WordStack ▸ Setup ▸ Wire
            // meta components). Thiếu asset → IPurchaseService vắng mặt và
            // BoosterPurchaseFlow tự rơi về stub log, không sập.
            if (_catalog != null)
            {
                builder.RegisterValue(_catalog, new[] { typeof(ITransactionCatalog) });

                builder.RegisterType(typeof(TransactionItemDispatcher),
                    new[] { typeof(ITransactionItemDispatcher) },
                    Reflex.Enums.Lifetime.Singleton,
                    Reflex.Enums.Resolution.Lazy);

                builder.RegisterType(typeof(PurchaseService),
                    new[] { typeof(IPurchaseService) },
                    Reflex.Enums.Lifetime.Singleton,
                    Reflex.Enums.Resolution.Lazy);
            }
        }
    }
}
