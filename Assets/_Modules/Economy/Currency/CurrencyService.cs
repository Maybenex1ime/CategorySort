using System;
using System.Collections.Generic;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using R3;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosMeta.Economy
{
    public sealed class CurrencyService : ICurrencyService, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<CurrencyService>();

        // Đủ rộng để phủ mọi lần store gửi lại (chỉ xảy ra với vài giao dịch gần nhất).
        private const int MaxGrantIds = 200;

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

        public bool AddOnce(int amount, string grantId)
        {
            if (amount <= 0 || string.IsNullOrEmpty(grantId)) return false;
            if (HasGrant(grantId))
            {
                _logger.Info($"[CurrencyService] AddOnce('{grantId}') đã cộng trước đó — bỏ qua.");
                return false;
            }

            if (_data.GrantIds == null) _data.GrantIds = new List<string>();

            _data.Coins += amount;
            _data.GrantIds.Add(grantId);
            if (_data.GrantIds.Count > MaxGrantIds)
                _data.GrantIds.RemoveRange(0, _data.GrantIds.Count - MaxGrantIds);

            // Ghi NGAY, không chờ lần ghi trễ: bên gọi chỉ xác nhận với store sau khi hàm này trả về.
            _save.SaveImmediate(_data);
            _coins.Value = _data.Coins;
            _logger.Info($"[CurrencyService] +{amount} coin (một lần, '{grantId}') → {_data.Coins}");
            return true;
        }

        public bool HasGrant(string grantId) =>
            !string.IsNullOrEmpty(grantId) && _data.GrantIds != null && _data.GrantIds.Contains(grantId);

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
