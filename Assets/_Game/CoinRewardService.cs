using System;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using WordStack.Contracts;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta
{
    /// <summary>
    /// Bê nguyên hình dạng <c>CoinRewardService</c> của aquapark: đăng ký nghe bus
    /// ngay trong constructor, cộng coin khi thắng, gỡ đăng ký khi Dispose.
    ///
    /// Hệ quả của việc đăng ký trong constructor: service này chỉ hoạt động khi có
    /// ai đó RESOLVE nó. Nó bind Lazy (không được Eager — Eager chạy trước lúc
    /// GameSaveInstaller đăng ký domain, CurrencyService sẽ đọc phải mặc định),
    /// nên <see cref="MetaSession"/> phải [Inject] nó để ép dựng. Aquapark cũng
    /// đúng cơ chế này, chỗ ép dựng của họ nằm trong AppFlowContext.
    /// </summary>
    public sealed class CoinRewardService : ICoinRewardService, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<CoinRewardService>();

        private readonly ICurrencyService _currency;
        private readonly int _coinsPerWin;

        public int LastAwardedAmount { get; private set; }

        public CoinRewardService(ICurrencyService currency, CoinRewardSettings settings)
        {
            _currency = currency;
            _coinsPerWin = settings != null ? settings.CoinsPerWin : 0;
            Bus.Global.On<LevelResultEvent>(OnLevelResult);
        }

        public void Dispose()
        {
            Bus.Global.Off<LevelResultEvent>(OnLevelResult);
        }

        public void ResetLastAwarded()
        {
            LastAwardedAmount = 0;
        }

        private void OnLevelResult(LevelResultEvent evt)
        {
            if (!evt.IsWin || _coinsPerWin <= 0)
            {
                LastAwardedAmount = 0;
                return;
            }

            _currency.Add(_coinsPerWin);
            LastAwardedAmount = _coinsPerWin;
            _logger.Info($"[CoinRewardService] thắng màn {evt.LevelIndex} → +{_coinsPerWin} coin");
        }
    }

    /// <summary>Số coin mỗi lần thắng. Installer dựng từ giá trị trong Inspector.</summary>
    public sealed class CoinRewardSettings
    {
        public int CoinsPerWin { get; }

        public CoinRewardSettings(int coinsPerWin)
        {
            CoinsPerWin = coinsPerWin < 0 ? 0 : coinsPerWin;
        }
    }
}
