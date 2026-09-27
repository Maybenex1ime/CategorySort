using System;
using System.Collections.Generic;
using UnityEngine;

namespace LogosGame.Features.Shop
{
    [CreateAssetMenu(fileName = "SO_ShopCatalog", menuName = "WordStack/Config/Shop Catalog")]
    public sealed class ShopCatalog : ScriptableObject, IShopCatalog
    {
        [Header("Gói bán bằng tiền thật — để trống Items là gói coin, có Items là gói combo")]
        [SerializeField] private CoinBundleDefinition[] _coinBundles = Array.Empty<CoinBundleDefinition>();

        public IReadOnlyList<CoinBundleDefinition> CoinBundles =>
            _coinBundles ?? (IReadOnlyList<CoinBundleDefinition>)Array.Empty<CoinBundleDefinition>();
    }
}
