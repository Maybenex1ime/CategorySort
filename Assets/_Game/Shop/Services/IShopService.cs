using System.Collections.Generic;
using LogosMeta.Economy;
using UnityEngine;

namespace LogosGame.Features.Shop
{
    public enum ShopPurchaseCode
    {
        Success = 0,
        UnknownProduct = 1,
        StoreDeclined = 2,
        StoreUnavailable = 3,
    }

    public readonly struct ShopPurchaseResult
    {
        public ShopPurchaseCode Code { get; }
        public string ProductId { get; }
        public int CoinsGranted { get; }

        public ShopPurchaseResult(ShopPurchaseCode code, string productId, int coinsGranted)
        {
            Code = code;
            ProductId = productId;
            CoinsGranted = coinsGranted;
        }

        public bool IsSuccess => Code == ShopPurchaseCode.Success;
    }

    /// <summary>
    /// Hai trục tiền tách bạch: gói coin trả TIỀN THẬT (qua IIAPService), item trả
    /// COIN (uỷ quyền thẳng cho IPurchaseService sẵn có). Đừng gộp — Price của
    /// TransactionDefinition là int coin, không diễn tả được "1.99 $".
    /// </summary>
    public interface IShopService
    {
        IReadOnlyList<CoinBundleDefinition> CoinBundles { get; }
        IReadOnlyList<TransactionDefinition> ItemOffers { get; }

        /// Khởi tạo store với mọi gói coin trong catalog. Gọi một lần lúc boot (không đợi mở
        /// Shop): giao dịch đã trả tiền mà chưa trao chỉ được store gửi lại sau bước này.
        Awaitable<bool> InitializeStore();

        /// Giá đã bản địa hoá từ store; chưa có thì PriceLabelFallback của catalog; id lạ → null.
        string GetPriceLabel(string productId);

        Awaitable<ShopPurchaseResult> PurchaseCoinBundle(string productId);

        /// Khôi phục giao dịch non-consumable. iOS bắt buộc có nút này; Android tự khôi phục lúc khởi tạo.
        Awaitable RestorePurchases();
        PurchaseResult PurchaseItem(string transactionId);
    }
}
