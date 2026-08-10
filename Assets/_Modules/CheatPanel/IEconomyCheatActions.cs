namespace LogosMeta.CheatPanel
{
    // Cheat actions the module's coin/heart sections invoke. Implemented by the
    // game's cheat service (which owns clamping, toasts and the real services).
    public interface IEconomyCheatActions
    {
        void SetCoins(int amount);
        void AddCoinIncrement();

        void SetHearts(int hearts);
        void RefillHeartsToMax();
    }
}
