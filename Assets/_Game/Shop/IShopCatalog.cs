using System.Collections.Generic;

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
    }
}
