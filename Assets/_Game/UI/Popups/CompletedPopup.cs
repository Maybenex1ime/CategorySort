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

        protected override void Awake()
        {
            base.Awake();

            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(OnClaimClicked);
            }
        }

        private void OnDestroy()
        {
            if (_claimButton != null)
            {
                _claimButton.onClick.RemoveListener(OnClaimClicked);
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
    }
}
