using System;
using System.Collections.Generic;
using BoosterModule;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using R3;

namespace LogosGame.Features.Currency.Services.Impl
{
    /// <summary>
    /// Coin/tim chuyển thẳng property reactive của ví / HeartService. Booster không có property
    /// reactive nên giữ một bản sao, lấy số ban đầu từ BoosterManager rồi cập nhật theo
    /// BoosterInventoryChangedEvent. Service nào vắng thì loại đó luôn 0 — UI không sập.
    /// </summary>
    public sealed class ResourceService : IResourceService, IDisposable
    {
        private static readonly ReadOnlyReactiveProperty<int> Zero = new ReactiveProperty<int>(0);

        private readonly ICurrencyService _currency;
        private readonly IHeartService _hearts;
        private readonly BoosterManager _boosters;
        private readonly Dictionary<ResourceType, ReactiveProperty<int>> _boosterCounts =
            new Dictionary<ResourceType, ReactiveProperty<int>>();

        public ResourceService(ICurrencyService currency, IHeartService hearts, BoosterManager boosters)
        {
            _currency = currency;
            _hearts = hearts;
            _boosters = boosters;
            Bus.Global.On<BoosterInventoryChangedEvent>(OnBoosterChanged);
        }

        public void Dispose() => Bus.Global.Off<BoosterInventoryChangedEvent>(OnBoosterChanged);

        public ReadOnlyReactiveProperty<int> Observe(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Coin: return _currency != null ? _currency.Coins : Zero;
                case ResourceType.Heart: return _hearts != null ? _hearts.Current : Zero;
            }

            return type.TryGetBoosterId(out BoosterId id) ? BoosterCount(type, id) : Zero;
        }

        private ReactiveProperty<int> BoosterCount(ResourceType type, BoosterId id)
        {
            if (!_boosterCounts.TryGetValue(type, out ReactiveProperty<int> count))
            {
                count = new ReactiveProperty<int>(_boosters != null ? _boosters.GetCount(id) : 0);
                _boosterCounts[type] = count;
            }
            return count;
        }

        private void OnBoosterChanged(BoosterInventoryChangedEvent evt)
        {
            if (ResourceTypeExtensions.TryFromBooster(evt.Id, out ResourceType type))
                BoosterCount(type, evt.Id).Value = evt.CurrentCount;
        }
    }
}
