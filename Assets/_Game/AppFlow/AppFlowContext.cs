using System;
using LogosGame.Features.Gameplay.Content;
using LogosGame.Features.Gameplay.Flow;
using LogosGame.Features.UI.Popups;
using LogosGame.Features.UI.Popups.Args;
using LogosGame.Features.UI.Screens;
using LogosMeta.Progression;
using LogosSDK.Audio;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using LogosSDK.Services;
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
        private readonly IAudioService _audioService;
        private readonly IHapticService _hapticService;
        private readonly LogosMeta.Economy.IHeartService _heartService;
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
            LevelCatalog levelCatalog = null,
            IAudioService audioService = null,
            IHapticService hapticService = null,
            LogosMeta.Economy.IHeartService heartService = null)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _minLoadingSeconds = minLoadingSeconds;
            _saveManager = saveManager;
            _coinReward = coinReward;
            _flow = flow;
            _levelService = levelService;
            _levelCatalog = levelCatalog;
            _audioService = audioService;
            _hapticService = hapticService;
            _heartService = heartService;
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
        /// đưa vào ViewModel → nó publish AddressKey → BoardInitializer nạp JSON
        /// qua Addressables → LevelCommands đưa xuống BoardController.
        ///
        /// Luôn dùng ResetLevelAsync (không phải StartLevelAsync): chơi lại cùng màn
        /// thì AddressKey không đổi giá trị, phải xoá trước để ReactiveProperty phát lại.
        /// </summary>
        public async Awaitable StartCurrentLevelAsync()
        {
            int index = CurrentLevelIndex;
            _playingLevelIndex = index;

            // Bảo hiểm: gate còn dính từ phiên trước (popup mở đúng lúc chuyển state)
            // thì bàn mới sẽ chết cứng — mở lại vô điều kiện mỗi lần nạp màn.
            LevelCommands.SetInputBlocked(false);

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

        /// <summary>
        /// Cheat ép kết quả màn hiện tại. Đi đúng đường máy phase (Committed →
        /// Evaluation) nên popup + phase chuẩn; đồng thời bắn LevelSignals.Finished
        /// để meta (coin, CurrentLevel++) chạy như kết quả thật.
        /// </summary>
        public async Awaitable ForceOutcomeAsync(bool isWin)
        {
            if (_flow == null)
            {
                _logger.Warn("[AppFlow] Thiếu IGameplayFlowController — không ép kết quả được.");
                return;
            }

            GameplayPhase phase = _flow.CurrentPhase.CurrentValue;

            // Chưa chạm lần nào thì tự "chạm" — máy phase chỉ nhận Committed khi Playing.
            if (phase == GameplayPhase.Ready)
            {
                await _flow.NotifyFirstInteractionAsync();
                phase = _flow.CurrentPhase.CurrentValue;
            }

            if (phase != GameplayPhase.Playing)
            {
                // Đang cascade (Evaluating/Animating) mà ép thì kết quả thật của board
                // sẽ đè lên ngay sau đó — chặn cho khỏi ra hai kết quả lệch nhau.
                _logger.Warn($"[AppFlow] Không ép được kết quả ở phase {phase} — chờ bàn đứng yên rồi bấm lại.");
                return;
            }

            // Kênh meta trước (như board thật: Finished bắn trong lúc settle) —
            // coin cộng xong TRƯỚC khi CompletedPopup đọc LastAwardedAmount.
            LevelSignals.RaiseFinished(isWin, _playingLevelIndex, 0);

            int remaining = _flow.RemainingMoves.CurrentValue;
            await _flow.NotifyPlayerActionCommittedAsync(new GameplayActionContext
            {
                RemainingMoves = remaining,
            });
            await _flow.NotifyEvaluationCompletedAsync(new GameplayEvaluationResult
            {
                IsWin = isWin,
                IsLose = !isWin,
                RemainingMoves = remaining,
                CanRetry = true,
                CanContinueToNext = isWin,
            });
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
                OnStartLevel = () => RunGatedByHearts(
                    () => TriggerDeferred(new StartGameplayTrigger()), returnToMenuOnClose: false),
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
                // WS trừ tim lúc VÀO màn nên sang màn kế cũng cần tim (khác aquapark).
                OnClaim = () => RunGatedByHearts(
                    () => TriggerDeferred(new NextLevelTrigger()), returnToMenuOnClose: true),
            };

            await _uiManager.ShowPopupImmediate<CompletedPopup, CompletedPopupArgs>(args);

            if (_coinReward != null) _coinReward.ResetLastAwarded();
        }

        private Awaitable ShowFailedPopupAsync()
        {
            FailedPopupArgs args = new FailedPopupArgs
            {
                LevelTitle = $"Level {_playingLevelIndex + 1}",
                OnTryAgain = () => RunGatedByHearts(
                    () => TriggerDeferred(new RetryTrigger()), returnToMenuOnClose: true),
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

        // --- Settings -----------------------------------------------------------

        private void OnOpenSettingsRequested() => ShowSettingsPopupInBackground();

        private async void ShowSettingsPopupInBackground()
        {
            try
            {
                await ShowSettingsPopupAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[AppFlow] Mở SettingsPopup thất bại.");
            }
        }

        // Port từ aquapark: chỉ 2 nút Music/Haptic được nối ở ngữ cảnh MainMenu.
        // Resume/Restart/Quit là cho bản Pause — WordStack không có pause, để null.
        public async Awaitable ShowSettingsPopupAsync()
        {
            if (_audioService == null || _hapticService == null)
            {
                _logger.Warn("[AppFlow] Thiếu IAudioService/IHapticService — chưa gắn AudioServicesInstaller lên ProjectScope?");
                return;
            }

            SettingsPopupArgs args = new SettingsPopupArgs
            {
                InitialMusicEnabled = !_audioService.IsMuted,
                InitialHapticEnabled = _hapticService.IsEnabled,
                OnMusicSelected = ToggleAudioMute,
                OnHapticSelected = ToggleHaptic,
            };

            await _uiManager.ShowPopupImmediate<SettingsPopup, SettingsPopupArgs>(args);
        }

        // --- Hearts gate --------------------------------------------------------

        // Không có HeartService (chưa gắn installer) thì không chặn — giữ hành vi cũ.
        private bool HasHearts => _heartService == null || _heartService.Current.CurrentValue > 0;

        /// <summary>Đủ tim thì chạy tiếp, hết tim thì thay bằng NoHeartsPopup.</summary>
        private void RunGatedByHearts(Action proceed, bool returnToMenuOnClose)
        {
            if (HasHearts)
            {
                proceed();
                return;
            }

            ShowNoHeartsPopupInBackground(returnToMenuOnClose);
        }

        private async void ShowNoHeartsPopupInBackground(bool returnToMenuOnClose)
        {
            try
            {
                await ShowNoHeartsPopupAsync(returnToMenuOnClose);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[AppFlow] Mở NoHeartsPopup thất bại.");
            }
        }

        // returnToMenuOnClose = true CHỈ cho gate ở Result (TryAgain/Claim): màn đã
        // kết thúc, sau popup là khoảng trống nên phải về menu chờ tim (có countdown).
        // Gate ở menu và ở pause-restart để false: menu thì đứng yên tại chỗ, còn
        // pause-restart thì bàn phía sau vẫn sống — đóng popup là chơi tiếp lượt dở.
        // Lưu ý: popup gọi OnClose cho MỌI nút (Ok/X lẫn Ad), nên true ở ngữ cảnh
        // giữa màn sẽ khiến nhận tim từ ad xong vẫn bị đá về menu + dính phí quit.
        public async Awaitable ShowNoHeartsPopupAsync(bool returnToMenuOnClose)
        {
            LevelCommands.SetInputBlocked(true);   // popup đè lên board/menu — board đọc raw Pointer

            NoHeartsPopupArgs args = new NoHeartsPopupArgs
            {
                OnClose = () =>
                {
                    LevelCommands.SetInputBlocked(false);
                    if (returnToMenuOnClose &&
                        (_manager.CurrentPhase == AppFlowPhase.Gameplay || _manager.CurrentPhase == AppFlowPhase.Result))
                    {
                        TriggerDeferred(new ReturnToMainMenuTrigger());
                    }
                },
            };

            await _uiManager.ShowPopupImmediate<NoHeartsPopup, NoHeartsPopupArgs>(args);
        }

        // Restart/Quit GIỮA màn mất 1 tim (như aquapark). Try Again sau thua thì
        // KHÔNG — cú thua đã trừ ở MetaSession rồi, trừ nữa là tính hai lần.
        // ConsumeOne tự guard <= 0 nên gọi lúc hết tim là no-op, không âm.
        internal void ConsumeHeart(string reason)
        {
            if (_heartService == null) return;
            _heartService.ConsumeOne();
            _logger.Info($"[AppFlow] {reason} → -1 tim (còn {_heartService.Current.CurrentValue}).");
        }

        /// <summary>Restart giữa màn: gate tim, TRỪ 1 tim, rồi nạp lại — manager gọi cho đường RestartRequestedEvent.</summary>
        // returnToMenuOnClose: FALSE — bàn phía sau vẫn sống, đóng popup (kể cả sau
        // khi xem ad nhận tim) là quay lại chơi tiếp lượt dở. true ở đây từng gây bug:
        // Ad +1 tim → OnClose → về menu → dính phí quit giữa màn → tim vừa nhận bay mất.
        public void RequestRetryGated()
            => RunGatedByHearts(() =>
            {
                ConsumeHeart("Restart giữa màn");
                TriggerDeferred(new RetryTrigger());
            }, returnToMenuOnClose: false);

        // PausePopup như aquapark, nhưng KHÔNG có phase Paused: bàn WordStack là
        // puzzle tĩnh (không timer), "pause" chỉ cần gate input trong lúc popup mở.
        // MỌI đường thoát (Close/Resume/Restart/Quit) đều phải mở lại gate,
        // thiếu một đường là kẹt bàn. Restart/Quit chạy thẳng không qua popup
        // xác nhận — tim WordStack trừ lúc VÀO màn, thoát không tốn thêm gì.
        public async Awaitable ShowPausePopupAsync()
        {
            if (_audioService == null || _hapticService == null)
            {
                _logger.Warn("[AppFlow] Thiếu IAudioService/IHapticService — chưa gắn AudioServicesInstaller lên ProjectScope?");
                return;
            }

            LevelCommands.SetInputBlocked(true);

            PausePopupArgs args = new PausePopupArgs
            {
                InitialMusicEnabled = !_audioService.IsMuted,
                InitialHapticEnabled = _hapticService.IsEnabled,
                OnMusicSelected = ToggleAudioMute,
                OnHapticSelected = ToggleHaptic,
                OnClose = () => LevelCommands.SetInputBlocked(false),
                OnResumeSelected = () => LevelCommands.SetInputBlocked(false),
                OnRestartSelected = () =>
                {
                    LevelCommands.SetInputBlocked(false);
                    // false: gate chặn thì đóng popup là chơi tiếp lượt dở (bàn còn sống),
                    // không đuổi về menu — xem chú thích ở RequestRetryGated.
                    RunGatedByHearts(() =>
                    {
                        ConsumeHeart("Restart giữa màn");
                        TriggerDeferred(new RetryTrigger());
                    }, returnToMenuOnClose: false);
                },
                OnQuitSelected = () =>
                {
                    LevelCommands.SetInputBlocked(false);
                    TriggerDeferred(new ReturnToMainMenuTrigger());
                },
            };

            await _uiManager.ShowPopupImmediate<PausePopup, PausePopupArgs>(args);
        }

        private void ToggleAudioMute()
        {
            _audioService.SetMuted(!_audioService.IsMuted);
        }

        private void ToggleHaptic()
        {
            bool nextEnabled = !_hapticService.IsEnabled;
            _hapticService.SetEnabled(nextEnabled);
            if (nextEnabled)
            {
                _hapticService.Play(HapticLevel.Light);   // rung nhẹ xác nhận vừa bật
            }
        }
    }
}
