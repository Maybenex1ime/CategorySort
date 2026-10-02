using System;
using R3;

namespace LogosMeta.Economy
{
    public interface IHeartService
    {
        ReadOnlyReactiveProperty<int>      Current        { get; }
        ReadOnlyReactiveProperty<bool>     IsFull         { get; }
        ReadOnlyReactiveProperty<TimeSpan> TimeUntilNext  { get; }

        // Tim vô hạn theo thời gian: trong hạn thì ConsumeOne không trừ tim. Thời gian còn
        // lại làm tròn xuống giây (UI hiện mm:ss); Zero khi không vô hạn.
        ReadOnlyReactiveProperty<bool>     IsUnlimited       { get; }
        ReadOnlyReactiveProperty<TimeSpan> UnlimitedTimeLeft { get; }

        /// Cộng thêm thời gian vô hạn — đang còn hạn thì nối tiếp, hết hạn thì tính từ bây giờ.
        /// Ghi đĩa ngay (thường đến từ giao dịch tiền thật). duration <= 0 thì bỏ qua.
        void AddUnlimited(TimeSpan duration);

        void ConsumeOne();
        void Add(int amount);
        // Cheat/debug: set exact heart count and restart the regen timer.
        void SetHearts(int hearts);
    }
}
