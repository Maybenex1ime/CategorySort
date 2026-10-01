using System;
using System.Collections.Generic;
using LogosGame.Features.Currency;
using UnityEngine;

namespace LogosGame.Features.Shop
{
    [CreateAssetMenu(fileName = "SO_ShopCatalog", menuName = "WordStack/Config/Shop Catalog")]
    public sealed class ShopCatalog : ScriptableObject, IShopCatalog
    {
        [Header("Gói bán bằng tiền thật — để trống Items là gói coin, có Items là gói combo")]
        [SerializeField] private CoinBundleDefinition[] _coinBundles = Array.Empty<CoinBundleDefinition>();

        [Header("Mua một lần — bật cờ No-Ads")]
        [SerializeField] private RemoveAdsDefinition _removeAds;

        [Header("Icon từng loại quà (hàng quà trong ô combo)")]
        [SerializeField] private List<RewardIcon> _rewardIcons = new List<RewardIcon>();

        public IReadOnlyList<CoinBundleDefinition> CoinBundles =>
            _coinBundles ?? (IReadOnlyList<CoinBundleDefinition>)Array.Empty<CoinBundleDefinition>();

        public RemoveAdsDefinition RemoveAds => _removeAds;

        public bool TryGetRewardIcon(ResourceType type, out Sprite icon)
        {
            icon = null;
            if (_rewardIcons == null) return false;
            for (int i = 0; i < _rewardIcons.Count; i++)
            {
                if (_rewardIcons[i].Type == type && _rewardIcons[i].Icon != null)
                {
                    icon = _rewardIcons[i].Icon;
                    return true;
                }
            }
            return false;
        }
    }
}
