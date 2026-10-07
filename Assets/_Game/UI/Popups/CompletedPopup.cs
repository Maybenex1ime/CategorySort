using LitMotion;
using LitMotion.Extensions;
using LogosGame.Features.UI.Common;
using LogosGame.Features.UI.Popups.Args;
using LogosMeta.Economy;
using LogosSDK.UI.Base;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    public sealed class CompletedPopup : PopupBase<CompletedPopupArgs>
    {
        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private TextMeshProUGUI _rewardAmountText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _doubleRewardButton;

        [Header("Coin bay về ô coin (bỏ trống = Claim đóng ngay như cũ)")]
        [SerializeField] private TextMeshProUGUI _coinBoxText;
        [SerializeField] private RectTransform _coinBoxIcon;
        [SerializeField] private CoinFly _coinFly;

        [Inject] private ICurrencyService _currency;

        private CountUpText _coinBox;
        private MotionHandle _iconPunch;
        private bool _claiming;

        private int Coins => _currency != null ? _currency.Coins.CurrentValue : 0;

        protected override void Awake()
        {
            base.Awake();

            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(OnClaimClicked);
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            }
        }

        private void OnDestroy()
        {
            if (_claimButton != null)
            {
                _claimButton.onClick.RemoveListener(OnClaimClicked);
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
            }
        }

        protected override void Initialize(CompletedPopupArgs args)
        {
            if (args.LevelTitle != null)
            {
                SetText(_levelTitleText, args.LevelTitle);
            }

            if (_rewardAmountText != null)
            {
                bool hasReward = args.RewardCoinAmount > 0;
                _rewardAmountText.gameObject.SetActive(hasReward);
                if (hasReward) _rewardAmountText.text = args.RewardCoinAmount.ToString();
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.gameObject.SetActive(args.RewardCoinAmount > 0 && args.OnDoubleReward != null);
            }

            _claiming = false;
            SetButtonsInteractable(true);
            if (_coinBoxText != null)
            {
                _coinBox ??= new CountUpText(_coinBoxText);
                // Coin thắng màn đã cộng trước khi popup mở: ô coin bắt đầu từ số cũ, Claim mới đếm lên.
                _coinBox.SetImmediate(Mathf.Max(0, Coins - args.RewardCoinAmount));
            }
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void OnClaimClicked()
        {
            if (_claiming) return;
            if (!CanFlyCoins()) { FinishClaim(); return; }

            _claiming = true;
            SetButtonsInteractable(false);
            _coinFly.Play(_rewardAmountText.transform.position, _coinBoxIcon.position,
                onFirstArrive: () => _coinBox.Set(Coins),
                onDone: FinishClaim,
                onEachArrive: PunchIcon);
        }

        private bool CanFlyCoins()
        {
            return _coinFly != null && _coinBox != null && _coinBoxIcon != null && _rewardAmountText != null
                && Args != null && Args.RewardCoinAmount > 0;
        }

        private void FinishClaim()
        {
            Dismiss();

            if (Args != null && Args.OnClaim != null)
            {
                Args.OnClaim();
            }
        }

        private void PunchIcon()
        {
            _iconPunch.TryComplete();   // trả scale về gốc trước khi nảy tiếp
            Vector3 baseScale = _coinBoxIcon.localScale;
            _iconPunch = LMotion.Punch.Create(baseScale, baseScale * 0.15f, 0.2f)
                .WithFrequency(6).WithDampingRatio(3.1f).WithCancelOnError()
                .BindToLocalScale(_coinBoxIcon).AddTo(_coinBoxIcon.gameObject);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_claimButton != null) _claimButton.interactable = interactable;
            if (_doubleRewardButton != null) _doubleRewardButton.interactable = interactable;
        }

        // Không Dismiss ở đây: popup chờ kết quả rewarded ad (AppFlow quyết định đóng hay không).
        private void OnDoubleRewardClicked()
        {
            if (Args != null && Args.OnDoubleReward != null)
            {
                Args.OnDoubleReward();
            }
        }
    }
}
