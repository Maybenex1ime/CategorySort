using System;
using System.Collections.Generic;
using UnityEngine;

namespace LogosGame.Features.Shop
{
    [CreateAssetMenu(fileName = "SO_ShopCatalog", menuName = "WordStack/Config/Shop Catalog")]
    public sealed class ShopCatalog : ScriptableObject, IShopCatalog
    {
        [Header("Tab Coin — gói coin trả tiền thật")]
        [SerializeField] private CoinBundleDefinition[] _coinBundles = Array.Empty<CoinBundleDefinition>();

        [Header("Tab Item — mã giao dịch mua bằng coin (khớp SO_TransactionCatalog)")]
        [SerializeField] private string[] _itemTransactionIds = Array.Empty<string>();

        public IReadOnlyList<CoinBundleDefinition> CoinBundles =>
            _coinBundles ?? (IReadOnlyList<CoinBundleDefinition>)Array.Empty<CoinBundleDefinition>();

        public IReadOnlyList<string> ItemTransactionIds =>
            _itemTransactionIds ?? (IReadOnlyList<string>)Array.Empty<string>();
    }
}
