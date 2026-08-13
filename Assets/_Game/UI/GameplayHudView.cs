using LogosMeta.Economy;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordStack.Contracts;
using WordStack.Meta.AppFlow;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta.UI
{
    /// <summary>
    /// HUD trong lúc chơi. Là MonoBehaviour thường nằm trong scene, KHÔNG phải IScreen —
    /// giống GameplayHudView bên aquapark (screen stack để trống suốt phase Gameplay).
    ///
    /// Rút gọn so với bản aquapark: bỏ nút pause (WordStack không có pause) và
    /// IFeedbackDispatcher (chưa có audio) — chỗ phát tiếng bấm để trống có TODO.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour
    {
        private static readonly ILogger _logger = LogManager.GetLogger<GameplayHudView>();

        [Header("Coin")]
        [SerializeField] private TextMeshProUGUI _coinText;

        [Header("Hearts")]
        [SerializeField] private TextMeshProUGUI _heartText;

        [Header("Level box")]
        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private Image _levelBoxImage;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _hardSprite;
        [SerializeField] private Sprite _crazySprite;

        [Header("Buttons")]
        [SerializeField] private Button _homeButton;

        [Header("Ẩn/hiện theo phase")]
        [Tooltip("Các nhánh con chỉ hiện khi đang chơi, ví dụ 'Gameplay Panel' và 'Booster Root'. " +
                 "KHÔNG gán chính object đang giữ script này — tắt nó là mất luôn listener.")]
        [SerializeField] private GameObject[] _gameplayOnlyRoots;

        [Inject] private readonly ICurrencyService _currencyService;
        [Inject] private readonly IHeartService _heartService;

        private DisposableBag _disposables;

        private void Awake()
        {
            if (_homeButton != null)
                _homeButton.onClick.AddListener(OnHomeClicked);

            LevelSignals.Started += OnLevelStarted;
            Bus.Global.On<AppFlowPhaseChangedEvent>(OnPhaseChanged);

            // Mặc định ẩn: Awake chạy trước khi AppFlow vào phase đầu tiên.
            SetGameplayRootsVisible(false);
        }

        private void Start()
        {
            // [Inject] chỉ sẵn sàng từ Start, không phải Awake.
            if (_currencyService != null && _coinText != null)
            {
                _currencyService.Coins
                    .Subscribe(value => _coinText.text = value.ToString())
                    .AddTo(ref _disposables);
            }

            if (_heartService != null && _heartText != null)
            {
                _heartService.Current
                    .Subscribe(value => _heartText.text = value.ToString())
                    .AddTo(ref _disposables);
            }
        }

        private void OnDestroy()
        {
            if (_homeButton != null)
                _homeButton.onClick.RemoveListener(OnHomeClicked);

            LevelSignals.Started -= OnLevelStarted;
            Bus.Global.Off<AppFlowPhaseChangedEvent>(OnPhaseChanged);
            _disposables.Dispose();
        }

        private void OnPhaseChanged(AppFlowPhaseChangedEvent evt)
        {
            // Giữ HUD ở cả Result: popup kết quả hiện đè lên, thấy coin/tim phía sau.
            bool inGame = evt.Phase == AppFlowPhase.Gameplay || evt.Phase == AppFlowPhase.Result;
            SetGameplayRootsVisible(inGame);
        }

        private void SetGameplayRootsVisible(bool visible)
        {
            if (_gameplayOnlyRoots == null) return;

            for (int i = 0; i < _gameplayOnlyRoots.Length; i++)
            {
                if (_gameplayOnlyRoots[i] != null)
                    _gameplayOnlyRoots[i].SetActive(visible);
            }
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (_levelTitleText != null)
                _levelTitleText.text = $"Level {evt.LevelIndex + 1}";

            ApplyDifficultySprite(evt.Difficulty);
        }

        private void ApplyDifficultySprite(LevelDifficulty difficulty)
        {
            if (_levelBoxImage == null) return;

            Sprite next = difficulty switch
            {
                LevelDifficulty.Hard  => _hardSprite,
                LevelDifficulty.Crazy => _crazySprite,
                _                     => _normalSprite,
            };

            if (next != null)
                _levelBoxImage.sprite = next;
        }

        private void OnHomeClicked()
        {
            PlayButtonFeedback();
            Bus.Global.Fire(new ReturnToMainMenuRequestedEvent());
        }

        /// <summary>
        /// Giữ nguyên vị trí gọi như aquapark (IFeedbackDispatcher.PlayUiButtonClick).
        /// TODO: nối vào IAudioService khi WordStack có âm thanh.
        /// </summary>
        private void PlayButtonFeedback()
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("[GameplayHud] Button click — chưa có audio.");
        }
    }
}
