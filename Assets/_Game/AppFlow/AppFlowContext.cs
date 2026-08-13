using System;
using LogosGame.Features.UI.Popups;
using LogosGame.Features.UI.Popups.Args;
using LogosGame.Features.UI.Screens;
using LogosMeta.Progression;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using LogosSDK.UI.Core;
using UnityEngine;
using WordStack.Contracts;
using WordStack.Meta.AppFlow.Triggers;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta.AppFlow
{
    /// <summary>
    /// Nơi chứa "làm bằng cách nào" của app flow: đẩy màn hình, mở popup, ra lệnh
    /// nạp màn. State chỉ quyết định "đi đâu tiếp" và gọi vào đây.
    /// </summary>
    internal sealed class AppFlowContext
    {
        private static readonly ILogger _logger = LogManager.GetLogger<AppFlowContext>();

        private readonly WordStackAppFlowManager _manager;
        private readonly UIManager _uiManager;
        private readonly ISaveManager _saveManager;
        private readonly ICoinRewardService _coinReward;
        private readonly float _minLoadingSeconds;

        private LevelResultEvent _lastResult;

        public AppFlowContext(
            WordStackAppFlowManager manager,
            UIManager uiManager,
            float minLoadingSeconds,
            ISaveManager saveManager = null,
            ICoinRewardService coinReward = null)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _minLoadingSeconds = minLoadingSeconds;
            _saveManager = saveManager;
            _coinReward = coinReward;
        }

        public float MinLoadingSeconds => _minLoadingSeconds;

        public void SetPhase(AppFlowPhase phase) => _manager.SetPhase(phase);

        // --- Level ------------------------------------------------------------

        /// <summary>
        /// Màn đang tới lượt chơi. Nguồn sự thật là LevelProgressData.CurrentLevel —
        /// ProgressionService tăng nó mỗi lần thắng, nên sau khi thắng con số này
        /// đã trỏ sang màn kế.
        /// </summary>
        public int CurrentLevelIndex
        {
            get
            {
                if (_saveManager == null) return 0;
                LevelProgressData progress = _saveManager.Load<LevelProgressData>();
                if (progress == null || progress.CurrentLevel < 0) return 0;
                return progress.CurrentLevel;
            }
        }

        /// <summary>Bảo gameplay nạp màn hiện tại. BoardController là bên nghe.</summary>
        public void StartCurrentLevel()
        {
            int index = CurrentLevelIndex;
            _logger.Info($"[AppFlow] Nạp màn {index}");
            LevelCommands.RequestLoad(index);
        }

        public void SetLastResult(LevelResultEvent result) => _lastResult = result;

        // --- Screens ----------------------------------------------------------

        public Awaitable ShowLoadingScreenAsync() => _uiManager.PushScreen<LoadingScreen>();

        public Awaitable PreWarmMainMenuScreenAsync() => _uiManager.PreWarmScreen<MainMenuScreen>();

        public Awaitable RemoveCurrentScreenAsync() => _uiManager.RemoveCurrentScreen();

        public Awaitable ShowMainMenuScreenAsync()
        {
            MainMenuScreenArgs args = new MainMenuScreenArgs
            {
                LevelTitle = $"Level {CurrentLevelIndex + 1}",
                OnStartLevel = () => TriggerDeferred(new StartGameplayTrigger()),
                OnOpenSettings = OnOpenSettingsRequested,
            };

            return _uiManager.PushScreen<MainMenuScreen>(args);
        }

        // --- Popups -----------------------------------------------------------

        public Awaitable ShowResultPopupAsync()
            => _lastResult.IsWin ? ShowCompletedPopupAsync() : ShowFailedPopupAsync();

        public void DismissActivePopup() => _uiManager.DismissCurrentPopup();

        private async Awaitable ShowCompletedPopupAsync()
        {
            CompletedPopupArgs args = new CompletedPopupArgs
            {
                // _lastResult.LevelIndex là màn VỪA chơi — CurrentLevel lúc này đã tăng.
                LevelTitle = $"Level {_lastResult.LevelIndex + 1}",
                RewardCoinAmount = _coinReward != null ? _coinReward.LastAwardedAmount : 0,
                OnClaim = () => TriggerDeferred(new NextLevelTrigger()),
            };

            await _uiManager.ShowPopupImmediate<CompletedPopup, CompletedPopupArgs>(args);

            if (_coinReward != null) _coinReward.ResetLastAwarded();
        }

        private Awaitable ShowFailedPopupAsync()
        {
            FailedPopupArgs args = new FailedPopupArgs
            {
                LevelTitle = $"Level {_lastResult.LevelIndex + 1}",
                OnTryAgain = () => TriggerDeferred(new RetryTrigger()),
                OnGoHome = () => TriggerDeferred(new ReturnToMainMenuTrigger()),
            };

            return _uiManager.ShowPopupImmediate<FailedPopup, FailedPopupArgs>(args);
        }

        // --- Triggers ---------------------------------------------------------

        /// <summary>
        /// Bắn trigger từ BÊN TRONG OnEnterAsync của một state, hoặc từ callback UI.
        /// </summary>
        public void TriggerDeferred(IAppFlowTrigger trigger) => TriggerDeferredInBackground(trigger);

        private async void TriggerDeferredInBackground(IAppFlowTrigger trigger)
        {
            try
            {
                // BẮT BUỘC. Lúc OnEnterAsync đang chạy thì StateMachine đang ở
                // _isTransitioning = true, nên TriggerAsync sẽ đẩy trigger vào
                // _pendingTriggers và ProcessPendingTriggers() xử lý nó bằng
                // Trigger() ĐỒNG BỘ — tức là chỉ gọi OnEnter() chứ KHÔNG gọi
                // OnEnterAsync() của state kế tiếp. Đợi sang frame sau thì
                // transition đã xong, TriggerAsync mới đi đúng nhánh async.
                await Awaitable.NextFrameAsync();
                await _manager.TriggerAsync(trigger);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[AppFlowContext] Trigger '{trigger.GetType().Name}' thất bại.");
            }
        }

        // --- Intent từ UI ------------------------------------------------------

        private void OnOpenSettingsRequested()
        {
            // WordStack chưa có SettingsPopup — cần cả tầng audio/haptic kèm theo.
            _logger.Info("[AppFlow] Settings tapped — chưa port SettingsPopup.");
        }
    }
}
