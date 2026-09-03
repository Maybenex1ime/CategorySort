using BoosterModule;
using R3;
using WordStack.Contracts;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>
    /// Booster Undo — dùng ngay, không cần chọn mục tiêu (bàn tự biết nước nào phải lùi).
    ///
    /// Trừ lượt bằng RequestUse(), và đó là mắt xích khởi động cả chuỗi:
    ///   RequestUse() → BoosterManager trừ 1 + bắn BoosterActivatedEvent
    ///     → MetaSession bắc cầu → LevelCommands.RequestUndo()
    ///     → BoardController.ApplyUndo() + Settle()
    /// </summary>
    public sealed class UndoBoosterViewModel : BoosterViewModelBase
    {
        private readonly ReactiveProperty<bool> _isUsable;

        public UndoBoosterViewModel() : base(BoosterId.Undo)
        {
            _isUsable = new ReactiveProperty<bool>(LevelSignals.UndoAvailable);
            LevelSignals.UndoAvailabilityChanged += OnAvailabilityChanged;
        }

        /// <summary>
        /// Có nước đi nào để lùi không. Tắt lúc vào màn (chưa đi nước nào), sau khi undo,
        /// và sau khi dùng Magnet/Shuffle — mà lượt này người chơi mua bằng coin, để bấm
        /// hụt rồi mất lượt là mất tiền thật.
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsUsable => _isUsable;

        public void OnButtonClicked()
        {
            if (!HasStock) return;        // hết lượt → View lo mở luồng mua
            if (!_isUsable.Value) return; // không có gì để lùi → không được trừ lượt

            RequestUse();
        }

        public override void Dispose()
        {
            LevelSignals.UndoAvailabilityChanged -= OnAvailabilityChanged;
            _isUsable.Dispose();
            base.Dispose();
        }

        private void OnAvailabilityChanged(bool available) => _isUsable.Value = available;
    }
}
