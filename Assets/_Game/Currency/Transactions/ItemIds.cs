namespace LogosGame.Features.Currency.Transactions
{
    // Well-known transaction ITEM ids configured in SO_TransactionCatalog by GD.
    // Keep in sync with catalog asset entries — unknown ids surface as a warn
    // in TransactionItemDispatcher at purchase time.
    public static class ItemIds
    {
        public const string BoosterShuffle = "booster.shuffle";
        public const string BoosterMagnet = "booster.magnet";
        public const string BoosterUndo = "booster.undo";
        public const string Heart = "heart";
    }
}
