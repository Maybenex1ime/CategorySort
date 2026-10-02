namespace LogosGame.Features.Shop
{
    /// <summary>
    /// Mã sản phẩm IAP — PHẢI khớp product id tạo trên Google Play Console /
    /// App Store Connect VÀ entry trong SO_ShopCatalog.asset.
    ///
    /// Id đã publish thì KHÔNG sửa được: đổi ở đây là đơn của user cũ không khớp
    /// nữa. Chốt kỹ trước lần build lên store đầu tiên.
    /// </summary>
    public static class ShopProductIds
    {
        public const string Coins1000 = "coins_1000";
        public const string Coins5000 = "coins_5000";
        public const string Coins10000 = "coins_10000";
        public const string Coins25000 = "coins_25000";
        public const string Coins50000 = "coins_50000";
        public const string Coins100000 = "coins_100000";

        // Gói combo (coin + item) — đã có trong SO_ShopCatalog.asset.
        public const string SpecialOffer = "special_offer";
        public const string Beginner = "beginner";
        public const string Medium = "medium";
        public const string High = "high";

        // Mua một lần (NonConsumable) — KHÔNG nằm trong CoinBundles, sống ở ShopCatalog.RemoveAds.
        public const string RemoveAds = "remove_ads";
    }
}
