using LogosGame.Features.Gameplay.Flow;
using LogosGame.Features.Gameplay.Services;
using LogosMeta.Economy;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordStack.Contracts;

namespace LogosGame.Features.Gameplay.Views
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        [Inject] private readonly IGameplayFlowController _gameplayFlowController;
        [Inject] private readonly IFeedbackDispatcher _feedbackDispatcher;
        [Inject] private readonly ICurrencyService _currencyService;
        [Inject] private readonly IDifficultyStateProvider _difficultyProvider;

        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private GameObject _coinBoxRoot;
        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private Button _settingsButton;

        [Header("Debug — gỡ khi có điều kiện thua thật")]
        [Tooltip("Ép màn hiện tại thành thua để thử chuỗi Result → FailedPopup.")]
        [SerializeField] private Button _forceLoseButton;

        [Header("Level Box Sprite per Difficulty")]
        [SerializeField] private Image _levelBoxImage;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _hardSprite;
        [SerializeField] private Sprite _crazySprite;

        private DisposableBag _disposables;

        private void Awake()
        {
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_forceLoseButton != null)
            {
                _forceLoseButton.onClick.AddListener(OnForceLoseClicked);
            }
        }

        private void Start()
        {
            if (_currencyService != null && _coinText != null)
            {
                _currencyService.Coins
                    .Subscribe(value => _coinText.text = value.ToString())
                    .AddTo(ref _disposables);
            }

            if (_difficultyProvider != null && _levelBoxImage != null)
            {
                _difficultyProvider.Difficulty
                    .Subscribe(ApplyDifficultySprite)
                    .AddTo(ref _disposables);
            }
        }

        private void ApplyDifficultySprite(LevelDifficulty difficulty)
        {
            if (_levelBoxImage == null) return;

            Sprite next = difficulty switch
            {
                LevelDifficulty.Hard => _hardSprite,
                LevelDifficulty.Crazy => _crazySprite,
                _ => _normalSprite
            };

            if (next != null)
            {
                _levelBoxImage.sprite = next;
            }
        }

        private void OnDestroy()
        {
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_forceLoseButton != null)
            {
                _forceLoseButton.onClick.RemoveListener(OnForceLoseClicked);
            }

            _disposables.Dispose();
        }

        public void SetLevelTitle(string levelTitle)
        {
            if (_levelTitleText != null)
            {
                if (levelTitle == null)
                {
                    _levelTitleText.text = string.Empty;
                }
                else
                {
                    _levelTitleText.text = levelTitle;
                }
            }
        }

        public void SetSettingsEnabled(bool isEnabled)
        {
            if (_settingsButton != null)
            {
                _settingsButton.interactable = isEnabled;
            }
        }

        public void SetSettingsVisible(bool isVisible)
        {
            if (_settingsButton != null)
            {
                _settingsButton.gameObject.SetActive(isVisible);
            }
        }

        public void SetCoinBoxVisible(bool isVisible)
        {
            GameObject target = _coinBoxRoot != null
                ? _coinBoxRoot
                : (_coinText != null ? _coinText.gameObject : null);

            if (target != null)
            {
                target.SetActive(isVisible);
            }
        }

        private void OnSettingsClicked()
        {
            if (_feedbackDispatcher != null)
            {
                _feedbackDispatcher.PlayUiButtonClick();
            }
            OpenSettingsInBackground();
        }

        private void OpenSettingsInBackground()
        {
            // Như aquapark: nút settings trong gameplay mở PausePopup. Khác ở chỗ
            // không có phase Paused — AppFlow gate input thay vì đổi state.
            LogosSDK.Core.Events.Bus.Global.Fire(new WordStack.Meta.AppFlow.PauseRequestedEvent());
        }

        private void OnForceLoseClicked()
        {
            if (_feedbackDispatcher != null)
            {
                _feedbackDispatcher.PlayUiButtonClick();
            }
            ForceLoseInBackground();
        }

        // Đi đúng đường của máy phase: phải qua Evaluating thì NotifyEvaluationCompleted
        // mới được chấp nhận (nó bỏ qua nếu phase khác Evaluating).
        private async void ForceLoseInBackground()
        {
            if (_gameplayFlowController == null) return;

            await _gameplayFlowController.NotifyPlayerActionCommittedAsync(new GameplayActionContext
            {
                RemainingMoves = 0,
            });

            await _gameplayFlowController.NotifyEvaluationCompletedAsync(new GameplayEvaluationResult
            {
                IsLose = true,
                RemainingMoves = 0,
                CanRetry = true,
            });
        }
    }
}
