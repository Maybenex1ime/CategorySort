using System;
using LogosMeta.Economy;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>Ô item (tab Item) — mua bằng coin nên nút khoá theo số dư ví.</summary>
    public sealed class ShopItemCellView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _buyButton;

        private Action _onClick;
        private IDisposable _coinSubscription;

        private void Awake()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _coinSubscription?.Dispose();
            if (_buyButton != null) _buyButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(TransactionDefinition entry, ReadOnlyReactiveProperty<int> coins, Action onClick)
        {
            _coinSubscription?.Dispose();
            _onClick = onClick;

            if (_nameText != null)
                _nameText.text = string.IsNullOrEmpty(entry.Name) ? entry.TransactionId : entry.Name;

            if (_descriptionText != null)
                _descriptionText.text = entry.Description ?? string.Empty;

            if (_priceText != null)
                // Giá 0 nghĩa là "chưa cấu hình", không phải miễn phí — theo đúng
                // cách BoosterPurchasePopup đang hiển thị.
                _priceText.text = entry.Price > 0 ? entry.Price.ToString() : "—";

            if (coins == null)
            {
                if (_buyButton != null) _buyButton.interactable = false;
                return;
            }

            int price = entry.Price;
            _coinSubscription = coins.Subscribe(current =>
            {
                if (_buyButton != null) _buyButton.interactable = price > 0 && current >= price;
            });
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
