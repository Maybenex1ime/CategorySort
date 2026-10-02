using System;
using R3;

namespace LogosGame.Features.Shop
{
    /// <summary>
    /// Quyền "không quảng cáo" đã mua (sản phẩm remove_ads). Chỉ là cờ: code ads tích hợp sau
    /// đọc IsNoAds để bỏ interstitial / banner; rewarded ad do người chơi tự bấm nên không chặn.
    /// </summary>
    public interface INoAdsService
    {
        ReadOnlyReactiveProperty<bool> IsNoAds { get; }

        /// Bật cờ và ghi đĩa ngay. Đã bật thì không làm gì.
        void Grant();
    }

    [Serializable]
    public class NoAdsData
    {
        public int SchemaVersion = 1;
        public bool Owned;
    }
}
