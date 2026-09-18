using LogosGame.Features.UI.Popups.Args;
using LogosSDK.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    public sealed class SettingsPopup : PopupBase<SettingsPopupArgs>
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _hapticSlider;
        [SerializeField] private Slider _soundSlider;
        [SerializeField] private Slider _notificationSlider;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _supportButton;
        [SerializeField] private Button _termsButton;
        [SerializeField] private Button _privacyButton;

        private bool _musicEnabled = true;
        private bool _hapticEnabled = true;
        private bool _soundEnabled = true;
        private bool _notificationEnabled = true;

        protected override void Awake()
        {
            base.Awake();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }

            if (_hapticSlider != null)
            {
                _hapticSlider.onValueChanged.AddListener(OnHapticChanged);
            }

            if (_soundSlider != null)
            {
                _soundSlider.onValueChanged.AddListener(OnSoundChanged);
            }

            if (_notificationSlider != null)
            {
                _notificationSlider.onValueChanged.AddListener(OnNotificationChanged);
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

            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            }

            if (_hapticSlider != null)
            {
                _hapticSlider.onValueChanged.RemoveListener(OnHapticChanged);
            }

            if (_soundSlider != null)
            {
                _soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
            }

            if (_notificationSlider != null)
            {
                _notificationSlider.onValueChanged.RemoveListener(OnNotificationChanged);
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
            _soundEnabled = args.InitialSoundEnabled;
            _notificationEnabled = args.InitialNotificationEnabled;
            // State is set first, so the value-changed event this fires is ignored by the handlers
            // (no toggle) but still reaches listeners like SliderHandleSprite.
            if (_musicSlider != null) _musicSlider.value = _musicEnabled ? 1f : 0f;
            if (_hapticSlider != null) _hapticSlider.value = _hapticEnabled ? 1f : 0f;
            if (_soundSlider != null) _soundSlider.value = _soundEnabled ? 1f : 0f;
            if (_notificationSlider != null) _notificationSlider.value = _notificationEnabled ? 1f : 0f;
        }

        private void OnCloseClicked()
        {
            Dismiss();

            if (Args != null && Args.OnClose != null)
            {
                Args.OnClose();
            }
        }

        private void OnMusicChanged(float value)
        {
            bool enabled = value > 0.5f;
            if (enabled == _musicEnabled) return;
            _musicEnabled = enabled;

            if (Args != null && Args.OnMusicSelected != null)
            {
                Args.OnMusicSelected();
            }
        }

        private void OnHapticChanged(float value)
        {
            bool enabled = value > 0.5f;
            if (enabled == _hapticEnabled) return;
            _hapticEnabled = enabled;

            if (Args != null && Args.OnHapticSelected != null)
            {
                Args.OnHapticSelected();
            }
        }

        private void OnSoundChanged(float value)
        {
            bool enabled = value > 0.5f;
            if (enabled == _soundEnabled) return;
            _soundEnabled = enabled;

            if (Args != null && Args.OnSoundSelected != null)
            {
                Args.OnSoundSelected();
            }
        }

        private void OnNotificationChanged(float value)
        {
            bool enabled = value > 0.5f;
            if (enabled == _notificationEnabled) return;
            _notificationEnabled = enabled;

            if (Args != null && Args.OnNotificationSelected != null)
            {
                Args.OnNotificationSelected();
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
