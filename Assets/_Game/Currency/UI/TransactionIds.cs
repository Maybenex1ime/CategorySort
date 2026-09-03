using BoosterModule;

namespace LogosGame.Features.Currency.UI
{
    /// <summary>
    /// Mã giao dịch — PHẢI khớp entry trong SO_TransactionCatalog.asset
    /// (Assets/_Game/Content/), id sai hiện UnknownTransaction lúc runtime.
    /// </summary>
    public static class TransactionIds
    {
        public const string BoosterShuffle = "t_booster_shuffle";
        public const string BoosterMagnet = "t_booster_magnet";
        public const string BoosterUndo = "t_booster_undo";
        public const string Heart = "t_heart";

        public static string ForBooster(BoosterId id) => id switch
        {
            BoosterId.Shuffle => BoosterShuffle,
            BoosterId.Magnet => BoosterMagnet,
            BoosterId.Undo => BoosterUndo,
            _ => null
        };

        public static bool TryGetBoosterId(string transactionId, out BoosterId boosterId)
        {
            switch (transactionId)
            {
                case BoosterShuffle: boosterId = BoosterId.Shuffle; return true;
                case BoosterMagnet:  boosterId = BoosterId.Magnet;  return true;
                case BoosterUndo:    boosterId = BoosterId.Undo;    return true;
                default:             boosterId = BoosterId.None;    return false;
            }
        }
    }
}
