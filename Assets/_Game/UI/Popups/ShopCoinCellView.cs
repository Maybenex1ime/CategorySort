using System;
using System.Text;
using LogosGame.Features.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>
    /// Ô một gói tiền thật — dùng cho cả gói coin (tab Coin) lẫn gói combo (tab Item). Trả tiền thật
    /// nên luôn bấm được, không gate theo ví.
    /// </summary>
    public sealed class ShopCoinCellView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _priceText;

        [Header("Gói combo (tuỳ chọn) — tên gói + danh sách item, tự ẩn với gói coin")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _itemsText;

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

        public void Bind(CoinBundleDefinition bundle, string priceLabel, Action onClick)
        {
            _onClick = onClick;

            if (_icon != null && bundle.Icon != null) _icon.sprite = bundle.Icon;
            if (_coinsText != null) _coinsText.text = bundle.Coins.ToString("N0");

            SetPrice(priceLabel);
            SetOptionalText(_titleText, bundle.Title);
            SetOptionalText(_itemsText, bundle.HasItems ? DescribeItems(bundle) : null);

            if (_popularBadge != null) _popularBadge.SetActive(bundle.Tag == ShopTag.Popular);
            if (_bestValueBadge != null) _bestValueBadge.SetActive(bundle.Tag == ShopTag.BestValue);
        }

        private static void SetOptionalText(TextMeshProUGUI label, string text)
        {
            if (label == null) return;
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
            label.text = text ?? string.Empty;
        }

        // ponytail: hiện thẳng đuôi ItemId ("booster.shuffle" → "+5 shuffle"). Cần tên bản địa hoá
        // hay icon từng item thì đổi ở đây.
        private static string DescribeItems(CoinBundleDefinition bundle)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bundle.Items.Length; i++)
            {
                string id = bundle.Items[i].ItemId ?? string.Empty;
                if (sb.Length > 0) sb.Append("  ");
                sb.Append('+').Append(bundle.Items[i].Amount).Append(' ').Append(id.Substring(id.LastIndexOf('.') + 1));
            }
            return sb.ToString();
        }

        /// Giá từ store (đã bản địa hoá) hoặc nhãn dự phòng của catalog — IShopService quyết.
        public void SetPrice(string priceLabel)
        {
            if (_priceText == null) return;

            // Chưa có giá nào thì hiện gạch ngang — số 0 hay chuỗi rỗng dễ bị đọc nhầm
            // thành miễn phí.
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
