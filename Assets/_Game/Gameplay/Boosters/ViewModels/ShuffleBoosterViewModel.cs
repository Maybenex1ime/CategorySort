using BoosterModule;
using R3;
using WordStack.Contracts;

namespace LogosGame.Features.Gameplay.Boosters.ViewModels
{
    /// <summary>
    /// Booster Shuffle — dùng ngay, không cần chọn mục tiêu (bàn tự tìm cách xếp).
    ///
    /// KHÔNG kế thừa InstantBoosterViewModelBase: lớp đó cố ý chưa trừ lượt vì hiệu ứng
    /// của Hand/Hammer/AddQueue/AddBelt chưa nối vào bàn. Shuffle có luật thật nên nó
    /// trừ lượt bằng RequestUse(), và đó là mắt xích khởi động cả chuỗi:
    ///
    ///   RequestUse() → BoosterManager trừ 1 + bắn BoosterActivatedEvent
    ///     → MetaSession bắc cầu → LevelCommands.RequestShuffle()
    ///     → BoardController.ApplyShuffle() + Settle()
    /// </summary>
    public sealed class ShuffleBoosterViewModel : BoosterViewModelBase
    {
        private readonly ReactiveProperty<bool> _isUsable;

        public ShuffleBoosterViewModel() : base(BoosterId.Shuffle)
        {
            _isUsable = new ReactiveProperty<bool>(LevelSignals.ShuffleAvailable);
            LevelSignals.ShuffleAvailabilityChanged += OnAvailabilityChanged;
        }

        /// <summary>
        /// Lớp trên còn ô trống không. Hết ô trống thì không dựng nổi Nhóm mồi, mà lượt
        /// này người chơi mua bằng coin — để bấm hụt rồi mất lượt là mất tiền thật.
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsUsable => _isUsable;

        public void OnButtonClicked()
        {
            if (!HasStock) return;        // hết lượt → View lo mở luồng mua
            if (!_isUsable.Value) return; // bàn không xáo được → không được trừ lượt

            RequestUse();
        }

        public override void Dispose()
        {
            LevelSignals.ShuffleAvailabilityChanged -= OnAvailabilityChanged;
            _isUsable.Dispose();
            base.Dispose();
        }

        private void OnAvailabilityChanged(bool available) => _isUsable.Value = available;
    }
}
