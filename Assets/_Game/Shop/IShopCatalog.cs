using System.Collections.Generic;
using LogosGame.Features.Currency;
using UnityEngine;

namespace LogosGame.Features.Shop
{
    /// <summary>
    /// Tách interface khỏi ScriptableObject để ShopService test được bằng fake
    /// thuần C# — cùng lý do ITransactionCatalog tồn tại bên _Modules/Economy.
    /// </summary>
    public interface IShopCatalog
    {
        /// Gói coin thường + gói combo (có Items), đều bán bằng tiền thật.
        IReadOnlyList<CoinBundleDefinition> CoinBundles { get; }

        /// Gói combo của mục Special Offer — cùng kiểu dữ liệu và cùng cách trao như combo thường.
        IReadOnlyList<CoinBundleDefinition> SpecialBundles { get; }

        /// Sản phẩm mua một lần. ProductId rỗng = shop không bán Remove Ads.
        RemoveAdsDefinition RemoveAds { get; }

        /// Icon của một loại quà; thiếu thì ô quà chỉ hiện chữ.
        bool TryGetRewardIcon(ResourceType type, out Sprite icon);
    }
}
