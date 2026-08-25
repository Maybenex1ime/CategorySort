using BoosterModule;

namespace LogosGame.Features.Currency.UI
{
    /// <summary>
    /// Mã giao dịch — PHẢI khớp entry trong SO_TransactionCatalog.asset
    /// (Assets/_Game/Content/), id sai hiện UnknownTransaction lúc runtime.
    /// Cùng bộ id với aquapark để port qua lại không lệch.
    /// </summary>
    public static class TransactionIds
    {
        public const string BoosterHand = "t_booster_hand";
        public const string BoosterHammer = "t_booster_hammer";
        public const string BoosterAddQueue = "t_booster_addqueue";
        public const string BoosterAddBelt = "t_booster_addbelt";
        public const string BoosterMagnet = "t_booster_magnet";
        public const string Heart = "t_heart";

        public static string ForBooster(BoosterId id) => id switch
        {
            BoosterId.Hand => BoosterHand,
            BoosterId.Hammer => BoosterHammer,
            BoosterId.AddQueue => BoosterAddQueue,
            BoosterId.AddBelt => BoosterAddBelt,
            BoosterId.Magnet => BoosterMagnet,
            _ => null
        };

        public static bool TryGetBoosterId(string transactionId, out BoosterId boosterId)
        {
            switch (transactionId)
            {
                case BoosterHand:     boosterId = BoosterId.Hand;     return true;
                case BoosterHammer:   boosterId = BoosterId.Hammer;   return true;
                case BoosterAddQueue: boosterId = BoosterId.AddQueue; return true;
                case BoosterAddBelt:  boosterId = BoosterId.AddBelt;  return true;
                case BoosterMagnet:   boosterId = BoosterId.Magnet;   return true;
                default:              boosterId = BoosterId.None;     return false;
            }
        }
    }
}
