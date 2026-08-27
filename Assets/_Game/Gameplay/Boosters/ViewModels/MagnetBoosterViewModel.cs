using BoosterModule;
using R3;
using WordStack.Contracts;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>
    /// Booster Nam châm — dùng ngay, không cần chọn mục tiêu (bàn tự tìm nhóm đáng hút).
    ///
    /// Trừ lượt bằng RequestUse(), và đó là mắt xích khởi động cả chuỗi:
    ///   RequestUse() → BoosterManager trừ 1 + bắn BoosterActivatedEvent
    ///     → MetaSession bắc cầu → LevelCommands.RequestMagnet()
    ///     → BoardController.ApplyMagnet() + Settle()
    /// </summary>
    public sealed class MagnetBoosterViewModel : BoosterViewModelBase
    {
        private readonly ReactiveProperty<bool> _isUsable;

        public MagnetBoosterViewModel() : base(BoosterId.Magnet)
        {
            _isUsable = new ReactiveProperty<bool>(LevelSignals.MagnetAvailable);
            LevelSignals.MagnetAvailabilityChanged += OnAvailabilityChanged;
        }

        /// <summary>
        /// Bàn có nhóm nào hút được không. View bind cờ này để xám nút: nam châm CÓ LÚC
        /// bất lực (chỉ còn nhóm cha đang chờ nhóm con collapse thì không nhóm nào đủ 4
        /// thẻ trên bàn), mà lượt này người chơi mua bằng coin — để bấm hụt rồi mất lượt
        /// là mất tiền thật.
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsUsable => _isUsable;

        public void OnButtonClicked()
        {
            if (!HasStock) return;        // hết lượt → View lo mở luồng mua
            if (!_isUsable.Value) return; // bàn không có mục tiêu → không được trừ lượt

            RequestUse();
        }

        public override void Dispose()
        {
            LevelSignals.MagnetAvailabilityChanged -= OnAvailabilityChanged;
            _isUsable.Dispose();
            base.Dispose();
        }

        private void OnAvailabilityChanged(bool available) => _isUsable.Value = available;
    }
}
