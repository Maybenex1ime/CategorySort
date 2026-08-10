using System;
using LogosMeta.Economy;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta
{
    public sealed class HeartInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField, Min(1)] private int _maxHearts = 5;
        [SerializeField, Min(1)] private int _regenMinutes = 30;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(
                new HeartSettings(_maxHearts, TimeSpan.FromMinutes(_regenMinutes)),
                new[] { typeof(HeartSettings) });

            builder.RegisterType(typeof(SystemTimeProvider),
                new[] { typeof(ITimeProvider) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterType(typeof(HeartService),
                new[] { typeof(IHeartService), typeof(IDisposable) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
