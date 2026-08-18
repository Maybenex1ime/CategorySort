using DG.Tweening;
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
        [Inject] private readonly IFeedbackDispatcher _feedbackDispatcher;
        [Inject] private readonly ICurrencyService _currencyService;
        [Inject] private readonly IDifficultyStateProvider _difficultyProvider;
        [Inject] private readonly IGameplayFlowController _flowController;

        // Placeholder — chưa nằm trong prefab, kéo TMP text vào là chạy. Bỏ trống
        // thì HUD đơn giản là không hiện số nước, không lỗi.
        [SerializeField] private TextMeshProUGUI _movesText;

        [Header("Progress bar (Cleared/TotalGroups)")]
        // _progressFill: sprite `Bar - Upper`, Image type Filled/Horizontal.
        // _progressRoot: cụm để ẩn khi total = 0; bỏ trống thì ẩn mỗi _progressFill.
        [SerializeField] private Image _progressFill;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private GameObject _progressRoot;

        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private GameObject _coinBoxRoot;
        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private Button _settingsButton;

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

            if (_flowController != null && _movesText != null)
            {
                _flowController.RemainingMoves
                    .Subscribe(value => _movesText.text = value.ToString())
                    .AddTo(ref _disposables);
            }

            // Tên màn ("Level N", N = số màn hiện tại) tự nghe thẳng ViewModel. Trước
            // đây chỉ GameplayUiRoot gọi SetLevelTitle, mà component đó không nằm trong
            // prefab/scene nào — nên ô LevelTxt đứng im ở chuỗi author sẵn trong prefab.
            if (_flowController != null && _levelTitleText != null)
            {
                _flowController.LevelTitle
                    .Subscribe(SetLevelTitle)
                    .AddTo(ref _disposables);
            }

            if (_flowController != null && (_progressFill != null || _progressText != null))
            {
                _flowController.GroupsCleared.Subscribe(_ => RefreshProgress()).AddTo(ref _disposables);
                _flowController.TotalGroups.Subscribe(_ => RefreshProgress()).AddTo(ref _disposables);
            }
        }

        private void RefreshProgress()
        {
            int cleared = _flowController.GroupsCleared.CurrentValue;
            int total = _flowController.TotalGroups.CurrentValue;

            GameObject root = _progressRoot != null
                ? _progressRoot
                : (_progressFill != null ? _progressFill.gameObject : null);
            if (root != null)
            {
                root.SetActive(total > 0);
            }
            if (total <= 0) return;

            if (_progressFill != null)
            {
                float target = (float)cleared / total;
                _progressFill.DOKill();
                if (cleared == 0)
                {
                    _progressFill.fillAmount = 0f;   // vào màn/chơi lại: snap, khỏi tween tụt về 0
                }
                else
                {
                    _progressFill.DOFillAmount(target, 0.25f);
                }
            }

            if (_progressText != null)
            {
                _progressText.text = $"{cleared}/{total}";
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
    }
}
