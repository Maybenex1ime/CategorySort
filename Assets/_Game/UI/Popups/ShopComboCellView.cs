using System;
using System.Collections.Generic;
using LogosGame.Features.Currency;
using LogosGame.Features.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>
    /// Ô gói combo (gói có Items): icon gói, tên, hàng quà (coin đứng đầu), nút giá. Trả tiền thật
    /// nên luôn bấm được, không gate theo ví.
    /// </summary>
    public sealed class ShopComboCellView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Transform _rewardRoot;
        [SerializeField] private ShopRewardItemView _rewardItemPrefab;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _buyButton;

        private readonly List<ShopRewardItemView> _rewards = new List<ShopRewardItemView>();
        private Action _onClick;

        private void Awake()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_buyButton != null) _buyButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(CoinBundleDefinition bundle, string priceLabel, IShopService shop, Action onClick)
        {
            _onClick = onClick;

            if (_icon != null && bundle.Icon != null) _icon.sprite = bundle.Icon;
            if (_titleText != null) _titleText.text = bundle.Title ?? string.Empty;
            SetPrice(priceLabel);

            ClearRewards();
            // Coin của gói (cộng cả reward loại Coin) gộp thành món đầu tiên.
            AddReward(shop, ResourceType.Coin, bundle.TotalCoins);
            if (bundle.Items != null)
            {
                for (int i = 0; i < bundle.Items.Length; i++)
                {
                    if (bundle.Items[i].Type == ResourceType.Coin) continue;
                    AddReward(shop, bundle.Items[i].Type, bundle.Items[i].Amount);
                }
            }
        }

        private void AddReward(IShopService shop, ResourceType type, int amount)
        {
            if (_rewardItemPrefab == null || _rewardRoot == null || amount <= 0) return;

            Sprite icon = null;
            shop?.TryGetRewardIcon(type, out icon);
            ShopRewardItemView item = Instantiate(_rewardItemPrefab, _rewardRoot);
            // Thiếu icon thì hiện tên loại để không mất thông tin.
            string amountText = ShopRewardItemView.FormatAmount(type, amount);
            item.Bind(icon, icon != null ? amountText : type + " " + amountText);
            _rewards.Add(item);
        }

        private void ClearRewards()
        {
            for (int i = 0; i < _rewards.Count; i++)
                if (_rewards[i] != null) Destroy(_rewards[i].gameObject);
            _rewards.Clear();
        }

        public void SetPrice(string priceLabel)
        {
            if (_priceText == null) return;
            // Chưa có giá thì gạch ngang — "0" hay rỗng dễ bị đọc thành miễn phí.
            _priceText.text = string.IsNullOrEmpty(priceLabel) ? "—" : priceLabel;
        }

        /// Khoá khi đang có giao dịch chạy — chặn bấm chồng thành 2 đơn.
        public void SetInteractable(bool interactable)
        {
            if (_buyButton != null) _buyButton.interactable = interactable;
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
