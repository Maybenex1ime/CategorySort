using LogosGame.Features.Currency;
using LogosGame.Features.Currency.Services;
using LogosGame.Features.UI.Popups;
using LogosGame.Features.UI.Common;
using LogosMeta.Economy;
using LitMotion;
using LitMotion.Extensions;
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
        [SerializeField] private Button _shopButton;
        [Tooltip("Nút + cạnh số coin ở Home — mở Shop và cuộn thẳng tới Coin Packs.")]
        [SerializeField] private Button _moreCoinsButton;
        [SerializeField] private Button _homeButton;

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
        [Tooltip("Nút \"+\" ở ô tim — chỉ hiện khi tim chưa đầy, bấm mở NoHeartsPopup (feedback #12). Trống = Button nằm cạnh Heart Icon")]
        [SerializeField] private Button _addHeartsButton;

        [Header("Coins UI")]
        [SerializeField] private TextMeshProUGUI _coinCountText;

        // Bottom bar: each button has its own "selected" background.
        // Only the background of the last clicked button is active.
        [Header("Bottom Bar Selected Backgrounds")]
        [SerializeField] private GameObject _homeSelectedBg;
        [SerializeField] private GameObject _shopSelectedBg;
        [SerializeField] private GameObject _settingsSelectedBg;

        // Bottom bar panels: only the panel of the last clicked button is active (Home = none).
        [Header("Panels")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _shopPanel;

        [Inject] private IHeartService _heartService;
        [Inject] private IResourceService _resources;

        private MainMenuScreenArgs _args;
        private DisposableBag _disposables;
        private CountUpText _coinCount;
        private MotionHandle _panelFade, _panelScale;
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

            if (_shopButton != null)
            {
                _shopButton.onClick.AddListener(OnShopClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.AddListener(OnHomeClicked);
            }

            if (_moreCoinsButton != null)
            {
                _moreCoinsButton.onClick.AddListener(OnMoreCoinsClicked);
            }

            if (_addHeartsButton == null && _heartIcon != null)
            {
                _addHeartsButton = _heartIcon.transform.parent.GetComponentInChildren<Button>(true);
            }

            if (_addHeartsButton != null)
            {
                _addHeartsButton.onClick.AddListener(OnAddHeartsClicked);
            }

            ShowSelectedBg(_homeSelectedBg);
            ShowPanel(null);
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

            if (_shopButton != null)
            {
                _shopButton.onClick.RemoveListener(OnShopClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeClicked);
            }

            if (_moreCoinsButton != null)
            {
                _moreCoinsButton.onClick.RemoveListener(OnMoreCoinsClicked);
            }

            if (_addHeartsButton != null)
            {
                _addHeartsButton.onClick.RemoveListener(OnAddHeartsClicked);
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
            ShowSelectedBg(_settingsSelectedBg);
            ShowPanel(_settingsPanel);
        }

        private void OnShopClicked()
        {
            ShowSelectedBg(_shopSelectedBg);
            ShowPanel(_shopPanel);
        }

        private void OnMoreCoinsClicked()
        {
            OnShopClicked();
            var shop = _shopPanel != null ? _shopPanel.GetComponentInChildren<ShopPopup>(true) : null;
            if (shop != null) shop.ScrollToCoinPacks();
        }

        private void OnAddHeartsClicked()
        {
            if (_args != null && _args.OnOpenHearts != null)
            {
                _args.OnOpenHearts();
            }
        }

        private void OnHomeClicked()
        {
            ShowSelectedBg(_homeSelectedBg);
            ShowPanel(null);
        }

        private void ShowPanel(GameObject panel)
        {
            SetPanel(_settingsPanel, panel);
            SetPanel(_shopPanel, panel);
        }

        // Tab vừa bật thì diễn vào (report UI animation, BasePanel: fade + scale Y lớn hơn → 1, OutBack); tab tắt thì tắt ngay.
        private void SetPanel(GameObject p, GameObject shown)
        {
            if (p == null) return;
            if (p != shown) { p.SetActive(false); return; }
            if (p.activeSelf) return;
            p.SetActive(true);

            _panelFade.TryComplete();
            _panelScale.TryComplete();
            var group = p.GetComponent<CanvasGroup>();
            if (group == null) group = p.AddComponent<CanvasGroup>();
            _panelFade = LMotion.Create(0f, 1f, PanelFadeDuration).WithEase(Ease.OutQuad).WithCancelOnError()
                .Bind(group, (a, g) => g.alpha = a).AddTo(p);
            _panelScale = LMotion.Create(new Vector3(1f, PanelStartScaleY, 1f), Vector3.one, PanelScaleDuration)
                .WithEase(Ease.OutBack).WithCancelOnError().BindToLocalScale(p.transform).AddTo(p);
        }

        private const float PanelFadeDuration = 0.25f, PanelScaleDuration = 0.3f, PanelStartScaleY = 1.15f;

        private void ShowSelectedBg(GameObject selected)
        {
            if (_homeSelectedBg != null)     _homeSelectedBg.SetActive(_homeSelectedBg == selected);
            if (_shopSelectedBg != null)     _shopSelectedBg.SetActive(_shopSelectedBg == selected);
            if (_settingsSelectedBg != null) _settingsSelectedBg.SetActive(_settingsSelectedBg == selected);
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

        private void BindHeartUI()
        {
            _logger.Info($"[MainMenuScreen] BindHeartUI: _heartService={(_heartService == null ? "NULL" : "instance=" + _heartService.GetHashCode())} _heartCountText={(_heartCountText == null ? "NULL" : "OK")} _heartFullLabel={(_heartFullLabel == null ? "NULL" : "OK")}");
            if (_heartService == null || _resources == null) return;

            _heartService.IsFull
                .Subscribe(SetHeartLayoutForFullState)
                .AddTo(ref _disposables);

            _resources.Observe(ResourceType.Heart)
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
            if (_resources == null || _coinCountText == null) return;
            _coinCount = new CountUpText(_coinCountText, onDecrease: spent => FloatingText.Spawn(_coinCountText, "-" + spent));
            _resources.Observe(ResourceType.Coin)
                .Subscribe(value => _coinCount.Set(value))
                .AddTo(ref _disposables);
        }

        private void SetHeartLayoutForFullState(bool isFull)
        {
            // Icon and count stay visible at all times.
            // Countdown shows only when not full; "Full" label shows only when full.
            if (_heartCountdownText != null) _heartCountdownText.gameObject.SetActive(!isFull);
            if (_heartFullLabel != null)     _heartFullLabel.SetActive(isFull);
            if (_addHeartsButton != null)    _addHeartsButton.gameObject.SetActive(!isFull);   // đầy tim thì không cho mở popup mua
        }
    }
}
