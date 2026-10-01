using LogosGame.Features.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    /// <summary>Một món quà trong hàng quà của ô combo: icon + số lượng.</summary>
    public sealed class ShopRewardItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _amountText;

        public void Bind(Sprite icon, string amount)
        {
            if (_icon != null)
            {
                _icon.gameObject.SetActive(icon != null);
                _icon.sprite = icon;
            }
            if (_amountText != null) _amountText.text = amount ?? string.Empty;
        }

        /// Coin: "2,000". Tim vô hạn (Amount = phút): "1h" nếu chia hết 60, ngược lại "30m". Còn lại: "x5".
        public static string FormatAmount(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Coin:
                    return amount.ToString("N0");
                case ResourceType.UnlimitedHeart:
                    return amount % 60 == 0 ? (amount / 60) + "h" : amount + "m";
                default:
                    return "x" + amount;
            }
        }
    }
}
