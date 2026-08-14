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

    /// <summary>
    /// Booster cần chọn mục tiêu: bấm để "lên nòng", bấm lần nữa để huỷ.
    ///
    /// CHƯA TRỪ LƯỢT khi lên nòng — bên aquapark việc trừ xảy ra lúc người chơi chạm
    /// bàn để chọn ô. WordStack chưa nối booster vào bàn nên chỉ đổi trạng thái armed;
    /// khi có luật tác động lên bàn thì gọi <see cref="BoosterViewModelBase.RequestUse"/>
    /// ở đúng thời điểm chạm bàn.
    /// </summary>
    public abstract class ArmableBoosterViewModelBase : BoosterViewModelBase
    {
        private readonly ReactiveProperty<bool> _isArmed = new(false);

        protected ArmableBoosterViewModelBase(BoosterId id) : base(id) { }

        public ReadOnlyReactiveProperty<bool> IsArmed => _isArmed;

        public void OnButtonClicked()
        {
            if (_isArmed.Value)
            {
                _isArmed.Value = false;
                return;
            }

            if (!HasStock) return;   // hết lượt → View lo mở luồng mua

            _isArmed.Value = true;
            Logger.Info($"[Booster] {BoosterId} lên nòng — chưa nối vào bàn nên chưa trừ lượt.");
        }

        public override void Dispose()
        {
            _isArmed.Dispose();
            base.Dispose();
        }
    }

    /// <summary>Booster dùng ngay, không cần chọn mục tiêu.</summary>
    public abstract class InstantBoosterViewModelBase : BoosterViewModelBase
    {
        protected InstantBoosterViewModelBase(BoosterId id) : base(id) { }

        public void OnButtonClicked() => RequestUse();
    }
}
