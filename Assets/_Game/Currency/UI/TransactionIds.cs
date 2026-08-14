using BoosterModule;

namespace LogosGame.Features.Currency.UI
{
    /// <summary>
    /// STUB. Mã giao dịch cho từng booster. WordStack chưa có catalog giao dịch nên
    /// đây chỉ là quy ước đặt tên chuỗi — chưa ai tra ngược lại được.
    ///
    /// Giữ tên + namespace của aquapark để *BoosterButtonView chép sang chạy ngay.
    /// Khi có cửa hàng thì các id này phải khớp entry trong catalog.
    /// </summary>
    public static class TransactionIds
    {
        public const string Heart = "heart";

        public static string ForBooster(BoosterId boosterId) => boosterId switch
        {
            BoosterId.Hand => "booster.hand",
            BoosterId.Hammer => "booster.hammer",
            BoosterId.AddQueue => "booster.addqueue",
            BoosterId.AddBelt => "booster.addbelt",
            _ => null,
        };

        public static bool TryGetBoosterId(string transactionId, out BoosterId boosterId)
        {
            switch (transactionId)
            {
                case "booster.hand": boosterId = BoosterId.Hand; return true;
                case "booster.hammer": boosterId = BoosterId.Hammer; return true;
                case "booster.addqueue": boosterId = BoosterId.AddQueue; return true;
                case "booster.addbelt": boosterId = BoosterId.AddBelt; return true;
                default: boosterId = BoosterId.None; return false;
            }
        }
    }
}
