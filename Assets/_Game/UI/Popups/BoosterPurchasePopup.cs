using LogosGame.Features.UI.Popups.Args;
using LogosSDK.UI.Base;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    public sealed class BoosterPurchasePopup : PopupBase<BoosterPurchasePopupArgs>
    {
        [Header("Display")]
        [SerializeField] private Image _boosterIcon;
        [SerializeField] private TextMeshProUGUI _boosterNameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _totalCoinText;

        [Header("Buttons")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _closeButton;

        private DisposableBag _disposables;

        protected override void Awake()
        {
            base.Awake();
            if (_buyButton != null) _buyButton.onClick.AddListener(OnBuyClicked);
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
            if (_buyButton != null) _buyButton.onClick.RemoveListener(OnBuyClicked);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        protected override void Initialize(BoosterPurchasePopupArgs args)
        {
            if (args == null) return;

            if (_boosterIcon != null && args.Icon != null)
                _boosterIcon.sprite = args.Icon;

            if (_boosterNameText != null)
                _boosterNameText.text = args.BoosterName ?? string.Empty;

            if (_descriptionText != null)
                _descriptionText.text = args.Description ?? string.Empty;

            if (_priceText != null)
                // WordStack chưa có catalog giao dịch nên Price = 0 nghĩa là "chưa có giá",
                // không phải miễn phí — hiện gạch ngang thay vì số 0.
                _priceText.text = args.Price > 0 ? args.Price.ToString() : "—";

            if (args.Coins != null)
            {
                args.Coins
                    .Subscribe(coins =>
                    {
                        if (_totalCoinText != null)
                            _totalCoinText.text = coins.ToString();

                        if (_buyButton != null)
                            _buyButton.interactable = coins >= args.Price;
                    })
                    .AddTo(ref _disposables);
            }
            else
            {
                if (_buyButton != null)
                    _buyButton.interactable = false;
            }
        }

        private void OnBuyClicked()
        {
            Dismiss();
            Args?.OnPurchaseConfirmed?.Invoke();
        }

        private void OnCloseClicked()
        {
            Dismiss();
            Args?.OnClose?.Invoke();
        }
    }
}
