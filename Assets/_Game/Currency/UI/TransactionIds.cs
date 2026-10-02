namespace LogosGame.Features.Currency.UI
{
    /// <summary>
    /// Mã giao dịch mua bằng coin trong game — PHẢI khớp entry trong SO_TransactionCatalog.asset
    /// (Assets/_Game/Content/), id sai hiện UnknownTransaction lúc runtime.
    /// </summary>
    public static class TransactionIds
    {
        private const string BoosterShuffle = "t_booster_shuffle";
        private const string BoosterMagnet = "t_booster_magnet";
        private const string BoosterUndo = "t_booster_undo";
        private const string Heart = "t_heart";

        /// Mã giao dịch mua tài nguyên này bằng coin; null khi không bán (Coin).
        public static string For(ResourceType type) => type switch
        {
            ResourceType.Heart => Heart,
            ResourceType.BoosterShuffle => BoosterShuffle,
            ResourceType.BoosterMagnet => BoosterMagnet,
            ResourceType.BoosterUndo => BoosterUndo,
            _ => null,
        };
    }
}
