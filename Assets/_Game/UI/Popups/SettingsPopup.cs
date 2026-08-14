using LogosGame.Features.UI.Popups.Args;
using LogosSDK.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    public sealed class SettingsPopup : PopupBase<SettingsPopupArgs>
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _musicButton;
        [SerializeField] private Button _hapticButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _supportButton;
        [SerializeField] private Button _termsButton;
        [SerializeField] private Button _privacyButton;

        [Header("Toggle Icons")]
        [SerializeField] private Image _musicIcon;
        [SerializeField] private Sprite _musicOnSprite;
        [SerializeField] private Sprite _musicOffSprite;
        [SerializeField] private Image _hapticIcon;
        [SerializeField] private Sprite _hapticOnSprite;
        [SerializeField] private Sprite _hapticOffSprite;

        private bool _musicEnabled = true;
        private bool _hapticEnabled = true;

        protected override void Awake()
        {
            base.Awake();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (_musicButton != null)
            {
                _musicButton.onClick.AddListener(OnMusicClicked);
            }

            if (_hapticButton != null)
            {
                _hapticButton.onClick.AddListener(OnHapticClicked);
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (_supportButton != null)
            {
                _supportButton.onClick.AddListener(OnSupportClicked);
            }

            if (_termsButton != null)
            {
                _termsButton.onClick.AddListener(OnTermsClicked);
            }

            if (_privacyButton != null)
            {
                _privacyButton.onClick.AddListener(OnPrivacyClicked);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            if (_musicButton != null)
            {
                _musicButton.onClick.RemoveListener(OnMusicClicked);
            }

            if (_hapticButton != null)
            {
                _hapticButton.onClick.RemoveListener(OnHapticClicked);
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
            }

            if (_supportButton != null)
            {
                _supportButton.onClick.RemoveListener(OnSupportClicked);
            }

            if (_termsButton != null)
            {
                _termsButton.onClick.RemoveListener(OnTermsClicked);
            }

            if (_privacyButton != null)
            {
                _privacyButton.onClick.RemoveListener(OnPrivacyClicked);
            }
        }

        protected override void Initialize(SettingsPopupArgs args)
        {
            SetOptionalButtonVisible(_resumeButton, args.OnResumeSelected != null);
            SetOptionalButtonVisible(_restartButton, args.OnRestartSelected != null);
            SetOptionalButtonVisible(_quitButton, args.OnQuitSelected != null);
            SetOptionalButtonVisible(_supportButton, args.OnSupportSelected != null);
            SetOptionalButtonVisible(_termsButton, args.OnTermsSelected != null);
            SetOptionalButtonVisible(_privacyButton, args.OnPrivacySelected != null);

            _musicEnabled = args.InitialMusicEnabled;
            _hapticEnabled = args.InitialHapticEnabled;
            ApplyMusicIcon();
            ApplyHapticIcon();
        }

        private void ApplyMusicIcon()
        {
            if (_musicIcon == null) return;
            Sprite next = _musicEnabled ? _musicOnSprite : _musicOffSprite;
            if (next != null) _musicIcon.sprite = next;
        }

        private void ApplyHapticIcon()
        {
            if (_hapticIcon == null) return;
            Sprite next = _hapticEnabled ? _hapticOnSprite : _hapticOffSprite;
            if (next != null) _hapticIcon.sprite = next;
        }

        private void OnCloseClicked()
        {
            Dismiss();

            if (Args != null && Args.OnClose != null)
            {
                Args.OnClose();
            }
        }

        private void OnMusicClicked()
        {
            _musicEnabled = !_musicEnabled;
            ApplyMusicIcon();

            if (Args != null && Args.OnMusicSelected != null)
            {
                Args.OnMusicSelected();
            }
        }

        private void OnHapticClicked()
        {
            _hapticEnabled = !_hapticEnabled;
            ApplyHapticIcon();

            if (Args != null && Args.OnHapticSelected != null)
            {
                Args.OnHapticSelected();
            }
        }

        private void OnResumeClicked()
        {
            if (Args != null && Args.OnResumeSelected != null)
            {
                Dismiss();
                Args.OnResumeSelected();
            }
        }

        private void OnRestartClicked()
        {
            if (Args != null && Args.OnRestartSelected != null)
            {
                Dismiss();
                Args.OnRestartSelected();
            }
        }

        private void OnQuitClicked()
        {
            if (Args != null && Args.OnQuitSelected != null)
            {
                Dismiss();
                Args.OnQuitSelected();
            }
        }

        private void OnSupportClicked()
        {
            if (Args != null && Args.OnSupportSelected != null)
            {
                Args.OnSupportSelected();
            }
        }

        private void OnTermsClicked()
        {
            if (Args != null && Args.OnTermsSelected != null)
            {
                Args.OnTermsSelected();
            }
        }

        private void OnPrivacyClicked()
        {
            if (Args != null && Args.OnPrivacySelected != null)
            {
                Args.OnPrivacySelected();
            }
        }

        private static void SetOptionalButtonVisible(Button button, bool isVisible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(isVisible);
            }
        }
    }
}
