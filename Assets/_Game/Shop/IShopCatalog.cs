using System.Collections.Generic;

namespace LogosGame.Features.Shop
{
    /// <summary>
    /// Tách interface khỏi ScriptableObject để ShopService test được bằng fake
    /// thuần C# — cùng lý do ITransactionCatalog tồn tại bên _Modules/Economy.
    /// </summary>
    public interface IShopCatalog
    {
        IReadOnlyList<CoinBundleDefinition> CoinBundles { get; }

        /// Chỉ giữ mã giao dịch; tên/giá/nội dung resolve qua IPurchaseService
        /// từ SO_TransactionCatalog — không nhân bản data sang đây.
        IReadOnlyList<string> ItemTransactionIds { get; }
    }
}
