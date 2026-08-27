using BoosterModule;
using LogosGame.Features.Currency.Transactions;
using LogosMeta.Economy;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Currency.Services.Impl
{
    /// <summary>
    /// Dịch item id đã mua thành phần thưởng thật — nơi DUY NHẤT biết id nghĩa là gì,
    /// module Economy không biết booster/heart. KHÁC AQUAPARK: bắn thẳng
    /// BoosterAddedEvent của BoosterModule (BoosterManager nghe sẵn) thay vì
    /// BoosterGrantEvent trung gian — WordStack không có tầng bridge đó.
    /// </summary>
    public sealed class TransactionItemDispatcher : ITransactionItemDispatcher
    {
        private static readonly ILogger _logger = LogManager.GetLogger<TransactionItemDispatcher>();

        private readonly IHeartService _hearts;

        public TransactionItemDispatcher(IHeartService hearts)
        {
            _hearts = hearts;
        }

        public void Grant(string itemId, int amount)
        {
            if (amount <= 0) return;

            switch (itemId)
            {
                case ItemIds.BoosterMagnet:
                    Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Magnet, amount));
                    break;
                case ItemIds.BoosterShuffle:
                    Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Shuffle, amount));
                    break;
                case ItemIds.BoosterUndo:
                    Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Undo, amount));
                    break;
                case ItemIds.Heart:
                    if (_hearts != null) _hearts.Add(amount);
                    break;
                default:
                    _logger.Warn($"[TransactionItemDispatcher] Unhandled item id '{itemId}'.");
                    break;
            }
        }
    }
}
