using LogosGame.Features.UI.Popups.Args;
using LogosSDK.UI.Base;
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
                if (hasReward) _rewardAmountText.text = "+" + args.RewardCoinAmount;
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.gameObject.SetActive(args.RewardCoinAmount > 0 && args.OnDoubleReward != null);
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
            Dismiss();

            if (Args != null && Args.OnClaim != null)
            {
                Args.OnClaim();
            }
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
