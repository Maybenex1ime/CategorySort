using System;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using R3;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosMeta.Economy
{
    public sealed class CurrencyService : ICurrencyService, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<CurrencyService>();

        private readonly ISaveManager _save;
        private readonly ReactiveProperty<int> _coins;

        private CurrencyData _data;

        public ReadOnlyReactiveProperty<int> Coins => _coins;

        public CurrencyService(ISaveManager save, CurrencySettings settings)
        {
            _save = save;
            _data = _save.Load<CurrencyData>();

            if (_data.SchemaVersion == 0)
            {
                _data.Coins = settings != null ? settings.InitialCoins : 0;
                _data.SchemaVersion = 1;
                _save.Save(_data);
            }

            ClampCoins();
            _coins = new ReactiveProperty<int>(_data.Coins);
        }

        public bool HasEnough(int amount) => amount >= 0 && _data.Coins >= amount;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            _data.Coins += amount;
            _save.Save(_data);
            _coins.Value = _data.Coins;
            _logger.Info($"[CurrencyService] +{amount} coin → {_data.Coins}");
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0) return false;
            if (_data.Coins < amount)
            {
                _logger.Info($"[CurrencyService] TrySpend({amount}) rejected; have {_data.Coins}");
                return false;
            }
            _data.Coins -= amount;
            _save.Save(_data);
            _coins.Value = _data.Coins;
            _logger.Info($"[CurrencyService] -{amount} coin → {_data.Coins}");
            return true;
        }

        public void SetCoins(int amount)
        {
            if (amount < 0) amount = 0;
            _data.Coins = amount;
            _save.Save(_data);
            _coins.Value = _data.Coins;
            _logger.Info($"[CurrencyService] SetCoins({amount}) → {_data.Coins}");
        }

        public void Dispose()
        {
            _coins.Dispose();
        }

        private void ClampCoins()
        {
            if (_data.Coins < 0) _data.Coins = 0;
        }
    }
}
