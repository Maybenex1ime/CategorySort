using System;
using LogosGame.Features.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>Banner Remove Ads đầu trang shop. ShopPopup ẩn cả banner khi đã sở hữu.</summary>
    public sealed class ShopRemoveAdsView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Button _buyButton;

        private Action _onClick;

        private void Awake()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_buyButton != null) _buyButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(RemoveAdsDefinition definition, string priceLabel, Action onClick)
        {
            _onClick = onClick;
            if (_icon != null && definition.Icon != null) _icon.sprite = definition.Icon;
            if (_titleText != null) _titleText.text = definition.Title ?? string.Empty;
            if (_subtitleText != null) _subtitleText.text = definition.Subtitle ?? string.Empty;
            SetPrice(priceLabel);
        }

        public void SetPrice(string priceLabel)
        {
            if (_priceText == null) return;
            _priceText.text = string.IsNullOrEmpty(priceLabel) ? "—" : priceLabel;
        }

        public void SetInteractable(bool interactable)
        {
            if (_buyButton != null) _buyButton.interactable = interactable;
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
