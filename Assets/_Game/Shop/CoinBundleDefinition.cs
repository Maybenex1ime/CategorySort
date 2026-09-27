using System;
using LogosMeta.Economy;
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

        // Gói combo: tên hiển thị + item tặng kèm coin (booster, tim — xem ItemIds). Để trống
        // Items là gói coin thường. Gói combo vẫn phải có Coins >= 1: AddOnce của ví là chốt
        // chống trao trùng cho CẢ gói, kể cả phần item.
        public string Title;
        public TransactionItem[] Items;

        public bool HasItems => Items != null && Items.Length > 0;
    }
}
