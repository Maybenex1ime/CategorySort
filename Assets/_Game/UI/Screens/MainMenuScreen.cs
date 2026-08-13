using LogosMeta.Economy;
using LogosSDK.Core.Logging;
using LogosSDK.UI.Base;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordStack.Contracts;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.UI.Screens
{
    public sealed class MainMenuScreen : ScreenBase
    {
        private static readonly ILogger _logger = LogManager.GetLogger<MainMenuScreen>();

        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;

        [Header("Play Button Difficulty Sprites")]
        [SerializeField] private Image _playButtonImage;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _hardSprite;
        [SerializeField] private Sprite _superHardSprite;

        [Header("Hearts UI")]
        [SerializeField] private Image _heartIcon;
        [SerializeField] private TextMeshProUGUI _heartCountText;
        [SerializeField] private TextMeshProUGUI _heartCountdownText;
        [SerializeField] private GameObject _heartFullLabel;

        [Header("Coins UI")]
        [SerializeField] private TextMeshProUGUI _coinCountText;

        [Inject] private IHeartService _heartService;
        [Inject] private ICurrencyService _currencyService;

        private MainMenuScreenArgs _args;
        private DisposableBag _disposables;
        private bool _isHeartUIBound;
        private bool _isCoinUIBound;

        protected override void Awake()
        {
            base.Awake();
            _logger.Info("[MainMenuScreen] Awake fired");

            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnLevelButtonClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnLevelButtonClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            _disposables.Dispose();
        }

        public override Awaitable Show(object args = null)
        {
            _logger.Info($"[MainMenuScreen] Show fired. _isHeartUIBound={_isHeartUIBound}");
            ApplyArgs(args as MainMenuScreenArgs);
            if (!_isHeartUIBound)
            {
                BindHeartUI();
                _isHeartUIBound = true;
            }
            if (!_isCoinUIBound)
            {
                BindCoinUI();
                _isCoinUIBound = true;
            }
            return base.Show(args);
        }

        private void OnLevelButtonClicked()
        {
            RequestStartLevel();
        }

        private void OnSettingsClicked()
        {
            RequestOpenSettings();
        }

        private void ApplyArgs(MainMenuScreenArgs args)
        {
            _args = args;

            if (_args != null && _levelTitleText != null && _args.LevelTitle != null)
            {
                _levelTitleText.text = _args.LevelTitle;
            }

            if (_args != null)
            {
                ApplyDifficultySprite(_args.Difficulty);
            }
        }

        private void ApplyDifficultySprite(LevelDifficulty difficulty)
        {
            if (_playButtonImage == null)
                return;

            Sprite next = difficulty switch
            {
                LevelDifficulty.Hard => _hardSprite,
                LevelDifficulty.Crazy => _superHardSprite,
                _ => _normalSprite,
            };

            if (next != null)
                _playButtonImage.sprite = next;
        }

        private void RequestStartLevel()
        {
            if (_args != null && _args.OnStartLevel != null)
            {
                _args.OnStartLevel();
            }
        }

        private void RequestOpenSettings()
        {
            if (_args != null && _args.OnOpenSettings != null)
            {
                _args.OnOpenSettings();
            }
        }

        private void BindHeartUI()
        {
            _logger.Info($"[MainMenuScreen] BindHeartUI: _heartService={(_heartService == null ? "NULL" : "instance=" + _heartService.GetHashCode())} _heartCountText={(_heartCountText == null ? "NULL" : "OK")} _heartFullLabel={(_heartFullLabel == null ? "NULL" : "OK")}");
            if (_heartService == null) return;

            _heartService.IsFull
                .Subscribe(SetHeartLayoutForFullState)
                .AddTo(ref _disposables);

            _heartService.Current
                .Subscribe(count =>
                {
                    _logger.Info($"[MainMenuScreen] Current.Subscribe fired with count={count}");
                    if (_heartCountText != null)
                        _heartCountText.text = count.ToString();
                })
                .AddTo(ref _disposables);

            _heartService.TimeUntilNext
                .Subscribe(t =>
                {
                    if (_heartCountdownText != null)
                        _heartCountdownText.text = $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
                })
                .AddTo(ref _disposables);
        }

        private void BindCoinUI()
        {
            if (_currencyService == null || _coinCountText == null) return;
            _currencyService.Coins
                .Subscribe(value => _coinCountText.text = value.ToString())
                .AddTo(ref _disposables);
        }

        private void SetHeartLayoutForFullState(bool isFull)
        {
            // Icon and count stay visible at all times.
            // Countdown shows only when not full; "Full" label shows only when full.
            if (_heartCountdownText != null) _heartCountdownText.gameObject.SetActive(!isFull);
            if (_heartFullLabel != null)     _heartFullLabel.SetActive(isFull);
        }
    }
}
