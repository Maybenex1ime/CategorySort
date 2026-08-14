using LogosGame.Features.Gameplay.Services;
using LogosSDK.Core.Events;
using LogosSDK.Core.Logging;
using R3;
using UnityEngine;
using WordStack.Contracts;
using WordStack.Meta.AppFlow;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Gameplay.Flow
{
    /// <summary>
    /// Máy phase của một phiên chơi, port từ GameplayViewModel bên aquapark.
    ///
    /// Một instance, hai contract (ISP): HUD lấy <see cref="IGameplayFlowController"/>,
    /// còn GameplayHudView lấy <see cref="IDifficultyStateProvider"/>. Gộp vào một chỗ
    /// vì độ khó đến cùng lúc với level qua GameplayStartContext — tách ra là phải
    /// đồng bộ hai nguồn.
    ///
    /// Khác bản aquapark: bỏ đối chiếu chéo IGameState.Evaluate() (đó là luật bàn chơi
    /// của aquapark), và dùng Bus.Global thay vì inject IEventBus — giống MetaSession.
    /// </summary>
    public sealed class WordStackGameplayViewModel : IGameplayFlowController, IDifficultyStateProvider
    {
        private static readonly ILogger _logger = LogManager.GetLogger<WordStackGameplayViewModel>();

        private readonly ReactiveProperty<GameplayPhase> _currentPhase = new(GameplayPhase.Ready);
        private readonly ReactiveProperty<int> _remainingMoves = new(0);
        private readonly ReactiveProperty<string> _levelTitle = new(string.Empty);
        private readonly ReactiveProperty<string> _levelId = new(string.Empty);
        private readonly ReactiveProperty<string> _addressKey = new(string.Empty);
        private readonly ReactiveProperty<bool> _canOpenSettings = new(false);
        private readonly ReactiveProperty<bool> _canReplay = new(false);
        private readonly ReactiveProperty<bool> _isInputBlocked = new(true);
        private readonly ReactiveProperty<bool> _showSettingsButton = new(true);
        private readonly ReactiveProperty<bool> _showCoinBox = new(true);
        private readonly ReactiveProperty<LevelDifficulty> _difficulty = new(LevelDifficulty.Normal);

        private GameplayStartContext _currentStartContext;
        private bool _hasPublishedOutcome;

        public WordStackGameplayViewModel()
        {
            ApplyPhase(GameplayPhase.Ready);
        }

        public ReadOnlyReactiveProperty<GameplayPhase> CurrentPhase => _currentPhase;
        public ReadOnlyReactiveProperty<int> RemainingMoves => _remainingMoves;
        public ReadOnlyReactiveProperty<string> LevelTitle => _levelTitle;
        public ReadOnlyReactiveProperty<string> LevelId => _levelId;
        public ReadOnlyReactiveProperty<string> AddressKey => _addressKey;
        public ReadOnlyReactiveProperty<bool> CanOpenSettings => _canOpenSettings;
        public ReadOnlyReactiveProperty<bool> CanReplay => _canReplay;
        public ReadOnlyReactiveProperty<bool> IsInputBlocked => _isInputBlocked;
        public ReadOnlyReactiveProperty<bool> ShowSettingsButton => _showSettingsButton;
        public ReadOnlyReactiveProperty<bool> ShowCoinBox => _showCoinBox;

        // IDifficultyStateProvider
        public ReadOnlyReactiveProperty<LevelDifficulty> Difficulty => _difficulty;

        // --- Vòng đời -----------------------------------------------------------

        public Awaitable StartLevelAsync(GameplayStartContext context)
        {
            _currentStartContext = context ?? new GameplayStartContext();

            _hasPublishedOutcome = false;

            _levelTitle.Value = _currentStartContext.LevelTitle ?? string.Empty;
            _levelId.Value = _currentStartContext.LevelId ?? string.Empty;
            _addressKey.Value = _currentStartContext.AddressKey ?? string.Empty;
            _difficulty.Value = _currentStartContext.Difficulty;

            _showSettingsButton.Value = _currentStartContext.ShowSettingsButton;
            _showCoinBox.Value = _currentStartContext.ShowCoinBox;

            _remainingMoves.Value = _currentStartContext.StartingMoves;

            ApplyPhase(GameplayPhase.Ready);
            return Completed();
        }

        public Awaitable ResetLevelAsync(GameplayStartContext context)
        {
            // Xoá TRƯỚC: chơi lại cùng màn thì AddressKey không đổi giá trị,
            // ReactiveProperty sẽ không phát, bên nghe không nạp lại bàn.
            _levelId.Value = string.Empty;
            _addressKey.Value = string.Empty;
            return StartLevelAsync(context);
        }

        // --- Người chơi yêu cầu -------------------------------------------------

        public Awaitable RequestRestartAsync()
        {
            Bus.Global.Fire(new RestartRequestedEvent());
            return Completed();
        }

        public Awaitable RequestReturnToMainMenuAsync()
        {
            // Dùng lại event AppFlow đã có thay vì thêm bản trùng vai của aquapark.
            Bus.Global.Fire(new ReturnToMainMenuRequestedEvent());
            return Completed();
        }

        // --- Gameplay báo cáo ---------------------------------------------------

        public Awaitable NotifyFirstInteractionAsync()
        {
            if (_currentPhase.Value == GameplayPhase.Ready)
                ApplyPhase(GameplayPhase.Playing);

            return Completed();
        }

        public Awaitable NotifyPlayerActionCommittedAsync(GameplayActionContext context)
        {
            if (_currentPhase.Value != GameplayPhase.Playing || context == null)
                return Completed();

            _remainingMoves.Value = context.RemainingMoves;
            ApplyPhase(GameplayPhase.Evaluating);
            return Completed();
        }

        public Awaitable NotifyEvaluationCompletedAsync(GameplayEvaluationResult result)
        {
            if (_currentPhase.Value != GameplayPhase.Evaluating || result == null)
                return Completed();

            _remainingMoves.Value = result.RemainingMoves;

            if (result.IsWin)
            {
                PublishOutcome(true, result);
                return Completed();
            }

            if (result.IsLose)
            {
                PublishOutcome(false, result);
                return Completed();
            }

            ApplyPhase(result.HasPendingAnimation ? GameplayPhase.Animating : GameplayPhase.Playing);
            return Completed();
        }

        public Awaitable NotifyAnimationSequenceCompletedAsync()
        {
            if (_currentPhase.Value == GameplayPhase.Ready || _currentPhase.Value == GameplayPhase.Animating)
                ApplyPhase(GameplayPhase.Playing);

            return Completed();
        }

        public Awaitable ResetOutcomeStateForReviveAsync()
        {
            // Phần làm được: mở lại cờ để lần thua sau còn công bố kết quả.
            _hasPublishedOutcome = false;
            ApplyPhase(GameplayPhase.Playing);

            // ĐỂ TRỐNG: khôi phục trạng thái bàn chơi sau khi hồi sinh (thêm nước đi,
            // dọn ô chết...) là luật của WordStack, chưa thiết kế. Bên aquapark việc đó
            // do IReviveBoosterService lo, không nằm trong ViewModel.
            return Completed();
        }

        // --- Private ------------------------------------------------------------

        private void PublishOutcome(bool isWin, GameplayEvaluationResult result)
        {
            if (_hasPublishedOutcome) return;
            _hasPublishedOutcome = true;

            string title = !string.IsNullOrEmpty(result.Title)
                ? result.Title
                : (isWin ? "Level Complete" : "Try Again");

            string subtitle = !string.IsNullOrEmpty(result.Subtitle)
                ? result.Subtitle
                : (isWin ? "You cleared the level." : "You ran out of chances.");

            GameplayResultViewData outcome = new GameplayResultViewData
            {
                IsWin = isWin,
                Title = title,
                Subtitle = subtitle,
                Stars = result.Stars,
                CanRetry = result.CanRetry,
                CanContinueToNext = result.CanContinueToNext,
            };

            ApplyPhase(isWin ? GameplayPhase.Win : GameplayPhase.Lose);

            _logger.Info($"[GameplayVM] Outcome: {(isWin ? "WIN" : "LOSE")} — {title}");
            Bus.Global.Fire(new GameplayOutcomePublishedEvent(outcome));
        }

        private void ApplyPhase(GameplayPhase phase)
        {
            _currentPhase.Value = phase;
            UpdateDerivedState();
        }

        private void UpdateDerivedState()
        {
            GameplayPhase phase = _currentPhase.Value;

            _canOpenSettings.Value = phase == GameplayPhase.Playing;
            _canReplay.Value = phase is GameplayPhase.Ready
                or GameplayPhase.Playing
                or GameplayPhase.Evaluating
                or GameplayPhase.Animating;
            _isInputBlocked.Value = phase != GameplayPhase.Playing;
        }

        private static Awaitable Completed()
        {
            AwaitableCompletionSource source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }
    }
}
