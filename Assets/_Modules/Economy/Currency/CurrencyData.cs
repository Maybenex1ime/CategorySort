using System;

namespace LogosMeta.Economy
{
    [Serializable]
    public class CurrencyData
    {
        // 0 = uninitialized (fresh install). CurrencyService bumps to 1 after applying initial state.
        public int SchemaVersion = 0;
        public int Coins = 0;
    }
}
