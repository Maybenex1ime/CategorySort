using LogosGame.Features.UI.Popups.Args;
using LogosSDK.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace LogosGame.Features.UI.Popups
{
    public sealed class PausePopup : PopupBase<PausePopupArgs>
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _hapticSlider;
        [SerializeField] private Slider _soundSlider;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;

        private bool _musicEnabled = true;
        private bool _hapticEnabled = true;
        private bool _soundEnabled = true;

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
        }

        protected override void Initialize(PausePopupArgs args)
        {
            _musicEnabled = args.InitialMusicEnabled;
            _hapticEnabled = args.InitialHapticEnabled;
            _soundEnabled = args.InitialSoundEnabled;
            // State is set first, so the value-changed event this fires is ignored by the handlers
            // (no toggle) but still reaches listeners like SliderHandleSprite.
            if (_musicSlider != null) _musicSlider.value = _musicEnabled ? 1f : 0f;
            if (_hapticSlider != null) _hapticSlider.value = _hapticEnabled ? 1f : 0f;
            if (_soundSlider != null) _soundSlider.value = _soundEnabled ? 1f : 0f;
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
    }
}
