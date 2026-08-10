namespace LogosMeta.Economy
{
    public enum PurchaseResultCode
    {
        Success = 0,
        UnknownTransaction = 1,
        NotEnoughCurrency = 2,
        InvalidItem = 3,
    }

    public readonly struct PurchaseResult
    {
        public PurchaseResultCode Code { get; }
        public string TransactionId { get; }
        public int PriceCharged { get; }

        public PurchaseResult(PurchaseResultCode code, string transactionId, int priceCharged)
        {
            Code = code;
            TransactionId = transactionId;
            PriceCharged = priceCharged;
        }

        public bool IsSuccess => Code == PurchaseResultCode.Success;
    }

    public interface IPurchaseService
    {
        bool TryGetTransaction(string transactionId, out TransactionDefinition entry);
        PurchaseResult TryPurchase(string transactionId);
    }
}
