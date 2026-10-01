using System;
using LogosGame.Features.Currency;
using UnityEngine;

namespace LogosGame.Features.Shop
{
    public enum ShopTag
    {
        None,
        Popular,
        BestValue,
    }

    [Serializable]
    public struct ShopReward
    {
        public ResourceType Type;
        [Min(1)] public int Amount;
    }

    [Serializable]
    public struct CoinBundleDefinition
    {
        public string ProductId;

        [Min(1)] public int Coins;

        // Giá hiển thị khi CHƯA lấy được giá thật từ store (stub, offline, editor).
        // Store trả về giá đã bản địa hoá theo region (49.000₫ ở VN, $1.99 ở US)
        // nên đây KHÔNG phải giá bán — chỉ là chỗ đỡ lúc chưa có store.
        public string PriceLabelFallback;

        public Sprite Icon;

        public ShopTag Tag;

        // Gói combo: tên hiển thị + tài nguyên tặng kèm (booster, tim; Coin thì cộng dồn vào
        // Coins). Để trống Items là gói coin thường. Gói combo vẫn phải có Coins >= 1: AddOnce
        // của ví là chốt chống trao trùng cho CẢ gói, kể cả phần item.
        public string Title;
        public ShopReward[] Items;

        public bool HasItems => Items != null && Items.Length > 0;

        /// Coin thật sự nhận: Coins + mọi reward loại Coin trong Items.
        public int TotalCoins
        {
            get
            {
                int total = Coins;
                if (Items != null)
                    for (int i = 0; i < Items.Length; i++)
                        if (Items[i].Type == ResourceType.Coin) total += Items[i].Amount;
                return total;
            }
        }
    }

    /// Sản phẩm mua một lần — bật cờ No-Ads, không tặng coin.
    [Serializable]
    public struct RemoveAdsDefinition
    {
        public string ProductId;

        // Như CoinBundleDefinition.PriceLabelFallback: chỗ đỡ lúc chưa có giá store.
        public string PriceLabelFallback;

        public Sprite Icon;
        public string Title;
        public string Subtitle;
    }

    /// Icon từng loại quà trong hàng quà của ô combo.
    [Serializable]
    public struct RewardIcon
    {
        public ResourceType Type;
        public Sprite Icon;
    }
}
