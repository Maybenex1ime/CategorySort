using LogosGame.Features.UI.Popups.Args;
using LogosSDK.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    public sealed class FailedPopup : PopupBase<FailedPopupArgs>
    {
        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private Button _tryAgainButton;
        [SerializeField] private Button _homeButton;

        protected override void Awake()
        {
            base.Awake();

            if (_tryAgainButton != null)
            {
                _tryAgainButton.onClick.AddListener(OnTryAgainClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.AddListener(OnHomeClicked);
            }
        }

        private void OnDestroy()
        {
            if (_tryAgainButton != null)
            {
                _tryAgainButton.onClick.RemoveListener(OnTryAgainClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeClicked);
            }
        }

        protected override void Initialize(FailedPopupArgs args)
        {
            if (args.LevelTitle != null)
            {
                SetText(_levelTitleText, args.LevelTitle);
            }
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void OnTryAgainClicked()
        {
            if (Args != null && Args.OnTryAgain != null)
            {
                Dismiss();
                Args.OnTryAgain();
            }
        }

        private void OnHomeClicked()
        {
            if (Args != null && Args.OnGoHome != null)
            {
                Dismiss();
                Args.OnGoHome();
            }
        }
    }
}
