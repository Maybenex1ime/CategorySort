using System;
using LogosGame.Features.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>Ô gói coin (tab Coin) — trả tiền thật nên luôn bấm được, không gate theo ví.</summary>
    public sealed class ShopCoinCellView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _priceText;

        [Header("Badge (tuỳ chọn)")]
        [SerializeField] private GameObject _popularBadge;
        [SerializeField] private GameObject _bestValueBadge;

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

        public void Bind(CoinBundleDefinition bundle, Action onClick)
        {
            _onClick = onClick;

            if (_icon != null && bundle.Icon != null) _icon.sprite = bundle.Icon;
            if (_coinsText != null) _coinsText.text = bundle.Coins.ToString("N0");

            if (_priceText != null)
            {
                // Chưa điền fallback thì hiện gạch ngang — số 0 hay chuỗi rỗng dễ
                // bị đọc nhầm thành miễn phí.
                _priceText.text = string.IsNullOrEmpty(bundle.PriceLabelFallback)
                    ? "—"
                    : bundle.PriceLabelFallback;
            }

            if (_popularBadge != null) _popularBadge.SetActive(bundle.Tag == ShopTag.Popular);
            if (_bestValueBadge != null) _bestValueBadge.SetActive(bundle.Tag == ShopTag.BestValue);
        }

        /// Khoá khi đang có giao dịch chạy — chặn bấm chồng thành 2 đơn.
        public void SetInteractable(bool interactable)
        {
            if (_buyButton != null) _buyButton.interactable = interactable;
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
