using LogosSDK.Core.Logging;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosMeta.Economy
{
    public sealed class PurchaseService : IPurchaseService
    {
        private static readonly ILogger _logger = LogManager.GetLogger<PurchaseService>();

        private readonly ICurrencyService _currency;
        private readonly ITransactionCatalog _catalog;
        private readonly ITransactionItemDispatcher _dispatcher;

        public PurchaseService(ICurrencyService currency, ITransactionCatalog catalog, ITransactionItemDispatcher dispatcher)
        {
            _currency = currency;
            _catalog = catalog;
            _dispatcher = dispatcher;
        }

        public bool TryGetTransaction(string transactionId, out TransactionDefinition entry)
        {
            entry = default;
            if (_catalog == null) return false;
            return _catalog.TryGet(transactionId, out entry);
        }

        public PurchaseResult TryPurchase(string transactionId)
        {
            if (!TryGetTransaction(transactionId, out TransactionDefinition entry))
            {
                _logger.Warn($"[PurchaseService] Unknown transaction '{transactionId}'.");
                return new PurchaseResult(PurchaseResultCode.UnknownTransaction, transactionId, 0);
            }

            if (!_currency.HasEnough(entry.Price))
            {
                return new PurchaseResult(PurchaseResultCode.NotEnoughCurrency, transactionId, 0);
            }

            if (entry.Items == null || entry.Items.Length == 0)
            {
                _logger.Warn($"[PurchaseService] Transaction '{transactionId}' has no items configured.");
                return new PurchaseResult(PurchaseResultCode.InvalidItem, transactionId, 0);
            }

            if (!_currency.TrySpend(entry.Price))
            {
                return new PurchaseResult(PurchaseResultCode.NotEnoughCurrency, transactionId, 0);
            }

            for (int i = 0; i < entry.Items.Length; i++)
            {
                TransactionItem item = entry.Items[i];
                if (item.Amount <= 0) continue;
                _dispatcher.Grant(item.ItemId, item.Amount);
            }

            _logger.Info($"[PurchaseService] Purchased '{transactionId}' for {entry.Price} coin.");
            return new PurchaseResult(PurchaseResultCode.Success, transactionId, entry.Price);
        }
    }
}
