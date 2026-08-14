using System;
using BoosterModule;
using LogosGame.Features.Cheat.Services;
using LogosGame.Features.Gameplay.Content;
using LogosMeta.CheatPanel;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using R3;
using WordStack.Meta.AppFlow;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Cheat.Services.Impl
{
    /// <summary>
    /// Bản WordStack của CheatService — cùng contract với aquapark nhưng ba nhánh
    /// nối khác:
    ///   economy → IHeartService / ICurrencyService (như cũ)
    ///   booster → BoosterModule qua Bus (aquapark cần bridge sang scene Gameplay;
    ///             BoosterManager ở đây là service DI Eager trong AppFlowInstaller
    ///             nghe cùng bus nên khỏi bridge, AreBoostersAvailable luôn true)
    ///   level   → JumpToLevelRequestedEvent, AppFlow ghi save + vào gameplay
    /// </summary>
    public sealed class CheatService : ICheatService, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<CheatService>();

        private readonly IHeartService _hearts;
        private readonly ICurrencyService _currency;
        private readonly CheatSettings _settings;
        private readonly int _maxHearts;

        private readonly Subject<CheatNotification> _notifications = new Subject<CheatNotification>();
        private readonly ReactiveProperty<bool> _boostersAvailable = new ReactiveProperty<bool>(true);

        public Observable<CheatNotification> Notifications => _notifications;
        public ReadOnlyReactiveProperty<bool> AreBoostersAvailable => _boostersAvailable;

        public CheatService(IHeartService hearts, ICurrencyService currency,
            CheatSettings settings, HeartSettings heartSettings)
        {
            _hearts = hearts;
            _currency = currency;
            _settings = settings;
            _maxHearts = heartSettings != null ? heartSettings.MaxHearts : 5;
        }

        public void Dispose()
        {
            _notifications.Dispose();
            _boostersAvailable.Dispose();
        }

        // --- Hearts -------------------------------------------------------------

        public void SetHearts(int hearts)
        {
            if (_hearts == null) { Emit(false, "HEART FAIL: thiếu HeartService"); return; }
            if (hearts < 0) hearts = 0;
            if (hearts > _maxHearts) hearts = _maxHearts;
            _hearts.SetHearts(hearts);
            Emit(true, $"HEART: set {hearts}");
        }

        public void RefillHeartsToMax()
        {
            if (_hearts == null) { Emit(false, "HEART FAIL: thiếu HeartService"); return; }
            _hearts.SetHearts(_maxHearts);
            Emit(true, $"HEART: refill {_maxHearts}");
        }

        // --- Coins --------------------------------------------------------------

        public void SetCoins(int amount)
        {
            if (_currency == null) { Emit(false, "COIN FAIL: thiếu CurrencyService"); return; }
            if (amount < 0) amount = 0;
            _currency.SetCoins(amount);
            Emit(true, $"COIN: set {amount}");
        }

        public void AddCoinIncrement()
        {
            if (_currency == null) { Emit(false, "COIN FAIL: thiếu CurrencyService"); return; }
            int step = _settings != null ? _settings.CoinIncrement : 100;
            _currency.Add(step);
            Emit(true, $"COIN: +{step}");
        }

        // --- Boosters -----------------------------------------------------------

        public void SetBoosterCount(BoosterId boosterId, int amount)
        {
            Bus.Global.Fire(new BoosterSetEvent(boosterId, amount));
            Emit(true, $"BOOSTER: {boosterId} = {amount}");
        }

        public void AddBoosterCount(BoosterId boosterId, int delta)
        {
            if (delta <= 0) { Emit(false, $"BOOSTER FAIL: delta {delta}"); return; }
            Bus.Global.Fire(new BoosterAddedEvent(boosterId, delta));
            Emit(true, $"BOOSTER: {boosterId} +{delta}");
        }

        // --- Level --------------------------------------------------------------

        public void JumpToLevel(int oneBasedLevelNumber)
        {
            if (oneBasedLevelNumber < 1)
            {
                Emit(false, $"LEVEL FAIL: '{oneBasedLevelNumber}' không hợp lệ");
                return;
            }

            Bus.Global.Fire(new JumpToLevelRequestedEvent(oneBasedLevelNumber));
            Emit(true, $"LEVEL: nhảy tới màn {oneBasedLevelNumber}");
        }

        public void ForceWin()
        {
            Bus.Global.Fire(new ForceOutcomeRequestedEvent(true));
            Emit(true, "OUTCOME: ép THẮNG");
        }

        public void ForceLose()
        {
            Bus.Global.Fire(new ForceOutcomeRequestedEvent(false));
            Emit(true, "OUTCOME: ép THUA");
        }

        // --- Private ------------------------------------------------------------

        private void Emit(bool success, string message)
        {
            if (!success) _logger.Warn($"[Cheat] {message}");
            else if (_logger.IsDebugEnabled) _logger.Debug($"[Cheat] {message}");

            _notifications.OnNext(new CheatNotification(success, message));
        }
    }
}
