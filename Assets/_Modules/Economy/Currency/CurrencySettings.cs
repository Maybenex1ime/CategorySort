namespace LogosMeta.Economy
{
    // Game-side installers construct this from their own config source
    // (SO, remote config...) and RegisterValue it — the module stays
    // ignorant of where the numbers come from.
    public sealed class CurrencySettings
    {
        public int InitialCoins { get; }

        public CurrencySettings(int initialCoins)
        {
            InitialCoins = initialCoins < 0 ? 0 : initialCoins;
        }
    }
}
