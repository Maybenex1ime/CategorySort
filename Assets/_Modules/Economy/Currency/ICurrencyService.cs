using R3;

namespace LogosMeta.Economy
{
    public interface ICurrencyService
    {
        ReadOnlyReactiveProperty<int> Coins { get; }

        bool HasEnough(int amount);
        void Add(int amount);
        bool TrySpend(int amount);
        // Cheat/debug: overwrite the coin balance (clamped to >= 0) and persist.
        void SetCoins(int amount);
    }
}
