using R3;

namespace LogosMeta.Economy
{
    public interface ICurrencyService
    {
        ReadOnlyReactiveProperty<int> Coins { get; }

        bool HasEnough(int amount);
        void Add(int amount);

        /// Cộng coin đúng MỘT lần cho mỗi grantId và ghi đĩa NGAY — dành cho đường tiền thật,
        /// nơi cùng một giao dịch có thể được gửi tới hai lần.
        /// Trả true = vừa cộng. Trả false = grantId đã được cộng trước đó, hoặc tham số
        /// không hợp lệ (amount <= 0, grantId rỗng) — không đổi gì.
        bool AddOnce(int amount, string grantId);

        /// grantId này đã từng được cộng qua AddOnce chưa.
        bool HasGrant(string grantId);
        bool TrySpend(int amount);
        // Cheat/debug: overwrite the coin balance (clamped to >= 0) and persist.
        void SetCoins(int amount);
    }
}
