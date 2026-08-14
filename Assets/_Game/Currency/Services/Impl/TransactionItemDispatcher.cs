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
                case ItemIds.BoosterHand:
                    Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Hand, amount));
                    break;
                case ItemIds.BoosterHammer:
                    Bus.Global.Fire(new BoosterAddedEvent(BoosterId.Hammer, amount));
                    break;
                case ItemIds.BoosterAddQueue:
                    Bus.Global.Fire(new BoosterAddedEvent(BoosterId.AddQueue, amount));
                    break;
                case ItemIds.BoosterAddBelt:
                    Bus.Global.Fire(new BoosterAddedEvent(BoosterId.AddBelt, amount));
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
