using System;
using LogosGame.Features.Gameplay.Content;
using LogosGame.Features.Gameplay.Flow;
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
        private readonly IGameplayFlowController _flow;
        private readonly ILevelService _levelService;
        private readonly LevelCatalog _levelCatalog;   // concrete: LevelEntry không mang Difficulty
        private readonly float _minLoadingSeconds;

        private GameplayResultViewData _lastResult;

        // Màn ĐANG chơi, chốt lúc bắt đầu. Không đọc CurrentLevelIndex ở thời điểm
        // kết thúc được: thắng thì ProgressionService đã tăng nó rồi, popup sẽ hiện
        // sai số màn.
        private int _playingLevelIndex;

        public AppFlowContext(
            WordStackAppFlowManager manager,
            UIManager uiManager,
            float minLoadingSeconds,
            ISaveManager saveManager = null,
            ICoinRewardService coinReward = null,
            IGameplayFlowController flow = null,
            ILevelService levelService = null,
            LevelCatalog levelCatalog = null)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _minLoadingSeconds = minLoadingSeconds;
            _saveManager = saveManager;
            _coinReward = coinReward;
            _flow = flow;
            _levelService = levelService;
            _levelCatalog = levelCatalog;
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

        /// <summary>
        /// Nạp màn hiện tại theo đường aquapark: dựng GameplayStartContext từ catalog,
        /// đưa vào ViewModel → nó publish AddressKey → BoardInitializerView nạp JSON
        /// qua Addressables → LevelCommands đưa xuống BoardController.
        ///
        /// Luôn dùng ResetLevelAsync (không phải StartLevelAsync): chơi lại cùng màn
        /// thì AddressKey không đổi giá trị, phải xoá trước để ReactiveProperty phát lại.
        /// </summary>
        public async Awaitable StartCurrentLevelAsync()
        {
            int index = CurrentLevelIndex;
            _playingLevelIndex = index;

            if (_flow == null)
            {
                _logger.Error("[AppFlow] Thiếu IGameplayFlowController — không nạp màn được.");
                return;
            }

            GameplayStartContext context = new GameplayStartContext
            {
                LevelTitle = $"Level {index + 1}",
                LevelId = _levelService?.GetCurrentLevelId() ?? string.Empty,
                AddressKey = _levelService?.GetCurrentLevelAddressKey() ?? string.Empty,
                Difficulty = GetCurrentLevelDifficulty(index),
            };

            _logger.Info($"[AppFlow] Nạp màn {index} (AddressKey='{context.AddressKey}')");
            await _flow.ResetLevelAsync(context);
        }

        /// <summary>Cheat nhảy màn: ghi thẳng CurrentLevel vào save.</summary>
        public void SetCurrentLevelIndex(int index)
        {
            if (_saveManager == null)
            {
                _logger.Warn("[AppFlow] Không có ISaveManager — bỏ qua SetCurrentLevelIndex.");
                return;
            }

            LevelProgressData progress = _saveManager.Load<LevelProgressData>() ?? new LevelProgressData();
            progress.CurrentLevel = index < 0 ? 0 : index;
            _saveManager.Save(progress);
            _logger.Info($"[AppFlow] Cheat: CurrentLevel = {progress.CurrentLevel}");
        }

        private LevelDifficulty GetCurrentLevelDifficulty(int index)
        {
            if (_levelCatalog == null || _levelCatalog.Entries == null || _levelCatalog.Entries.Length == 0)
                return LevelDifficulty.Normal;

            // Clamp như aquapark: index vượt catalog thì lấy entry cuối, QA không crash.
            int count = _levelCatalog.Entries.Length;
            int clamped = index < count ? index : count - 1;
            return _levelCatalog.Entries[clamped].Difficulty;
        }

        public void SetLastResult(GameplayResultViewData result) => _lastResult = result;

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
            => _lastResult != null && _lastResult.IsWin
                ? ShowCompletedPopupAsync()
                : ShowFailedPopupAsync();

        public void DismissActivePopup() => _uiManager.DismissCurrentPopup();

        private async Awaitable ShowCompletedPopupAsync()
        {
            CompletedPopupArgs args = new CompletedPopupArgs
            {
                LevelTitle = $"Level {_playingLevelIndex + 1}",
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
                LevelTitle = $"Level {_playingLevelIndex + 1}",
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
