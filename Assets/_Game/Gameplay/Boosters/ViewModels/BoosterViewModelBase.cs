using System;
using BoosterModule;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using R3;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>
    /// Cầu nối BoosterModule → R3. Module dùng event C# thuần (<c>Action&lt;int&gt;</c>),
    /// còn các *BoosterButtonView chép từ aquapark lại đọc ReadOnlyReactiveProperty.
    /// Lớp này gói lại một lần cho cả 4 booster.
    ///
    /// Thay cho I*BoosterService của aquapark — inventory và persistence do
    /// BoosterManager lo, không cần service riêng cho từng loại.
    /// </summary>
    public abstract class BoosterViewModelBase : IDisposable
    {
        protected static readonly ILogger Logger = LogManager.GetLogger<BoosterViewModelBase>();

        private readonly BoosterSlotViewModel _slot;
        private readonly ReactiveProperty<int> _count = new(0);

        protected BoosterViewModelBase(BoosterId id)
        {
            BoosterId = id;
            _slot = new BoosterSlotViewModel(id);
            _slot.OnCountChanged += OnCountChanged;
            _count.Value = _slot.Count;
        }

        public BoosterId BoosterId { get; }

        public ReadOnlyReactiveProperty<int> Count => _count;

        public virtual void Dispose()
        {
            _slot.OnCountChanged -= OnCountChanged;
            _slot.Dispose();
            _count.Dispose();
        }

        /// <summary>Trừ một lượt qua BoosterManager. Nó sẽ bắn BoosterActivatedEvent.</summary>
        protected void RequestUse() => _slot.RequestUse();

        protected bool HasStock => _count.Value > 0;

        private void OnCountChanged(int value) => _count.Value = value;
    }
}
