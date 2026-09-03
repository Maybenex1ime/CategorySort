using System;
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
    }
}
