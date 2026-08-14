using System;
using LogosGame.Features.Gameplay.Flow;
using LogosSDK.Core.AppFlow;
using LogosSDK.Core.Events;
using LogosSDK.Core.FSM;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using LogosSDK.UI.Core;
using UnityEngine;
using WordStack.Meta.AppFlow.Triggers;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta.AppFlow
{
    /// <summary>
    /// Điều phối cấp app: sở hữu FSM, giữ phase hiện tại, chặn intent sai ngữ cảnh
    /// trước khi bắn trigger, và nghe kết quả màn chơi từ bus.
    /// KHÔNG chứa logic màn hình — cái đó ở <see cref="AppFlowContext"/>.
    ///
    /// AppBootstrap (SDK) resolve interface này rồi gọi StartAsync().
    /// </summary>
    public sealed class WordStackAppFlowManager : IAppFlowManager, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<WordStackAppFlowManager>();

        private readonly AppFlowContext _context;
        private readonly StateMachine<IAppFlowState, IAppFlowTrigger> _stateMachine;

        private bool _hasStarted;
        private bool _isStarting;

        public WordStackAppFlowManager(
            UIManager uiManager,
            float minLoadingSeconds = 2f,
            ISaveManager saveManager = null,
            ICoinRewardService coinReward = null,
            IGameplayFlowController flow = null,
            LogosMeta.Progression.ILevelService levelService = null,
            LogosGame.Features.Gameplay.Content.LevelCatalog levelCatalog = null,
            LogosSDK.Audio.IAudioService audioService = null,
            LogosSDK.Services.IHapticService hapticService = null,
            LogosMeta.Economy.IHeartService heartService = null)
        {
            if (uiManager == null)
                throw new ArgumentNullException(nameof(uiManager));

            _stateMachine = new StateMachine<IAppFlowState, IAppFlowTrigger>();
            _context = new AppFlowContext(this, uiManager, minLoadingSeconds,
                saveManager, coinReward, flow, levelService, levelCatalog,
                audioService, hapticService, heartService);

            // Nguồn kết quả DUY NHẤT của AppFlow là ViewModel — nó công bố sau khi
            // máy phase chốt Win/Lose. MetaSession vẫn nghe LevelSignals.Finished
            // riêng cho tim/coin/progression; hai bên không đụng nhau.
            //
            // Nhờ đi qua ViewModel, nút debug "ép thua" trên HUD cũng vào được
            // ResultState mà không phải giả lập tín hiệu từ board.
            Bus.Global.On<GameplayOutcomePublishedEvent>(OnOutcomePublished);
            Bus.Global.On<ReturnToMainMenuRequestedEvent>(OnReturnToMainMenuRequested);
            Bus.Global.On<RestartRequestedEvent>(OnRestartRequested);
            Bus.Global.On<JumpToLevelRequestedEvent>(OnJumpToLevelRequested);
            Bus.Global.On<PauseRequestedEvent>(OnPauseRequested);
            Bus.Global.On<ForceOutcomeRequestedEvent>(OnForceOutcomeRequested);
        }

        public AppFlowPhase CurrentPhase { get; private set; }

        public async Awaitable StartAsync()
        {
            if (_hasStarted || _isStarting)
                return;

            _isStarting = true;

            try
            {
                await _stateMachine.InitializeAsync(new States.BootState(_context));
                _hasStarted = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[AppFlow] Khởi tạo app flow thất bại.");
                throw;
            }
            finally
            {
                _isStarting = false;
            }
        }

        public async Awaitable HandleRootBackAsync()
        {
            switch (CurrentPhase)
            {
                case AppFlowPhase.Gameplay:
                case AppFlowPhase.Result:
                    await TriggerAsync(new ReturnToMainMenuTrigger());
                    break;
                default:
                    _logger.Info($"[AppFlow] Root back bị bỏ qua ở phase {CurrentPhase}.");
                    break;
            }
        }

        // --- Intent API (có guard theo phase) ---------------------------------

        public async Awaitable StartGameplayAsync()
        {
            if (CurrentPhase != AppFlowPhase.MainMenu)
            {
                _logger.Warn($"[AppFlow] Bỏ qua StartGameplay ngoài MainMenu (đang {CurrentPhase}).");
                return;
            }

            await TriggerAsync(new StartGameplayTrigger());
        }

        public async Awaitable ReturnToMainMenuAsync()
        {
            if (CurrentPhase != AppFlowPhase.Gameplay && CurrentPhase != AppFlowPhase.Result)
            {
                _logger.Warn($"[AppFlow] Bỏ qua ReturnToMainMenu ngoài Gameplay/Result (đang {CurrentPhase}).");
                return;
            }

            await TriggerAsync(new ReturnToMainMenuTrigger());
        }

        public void Dispose()
        {
            Bus.Global.Off<GameplayOutcomePublishedEvent>(OnOutcomePublished);
            Bus.Global.Off<ReturnToMainMenuRequestedEvent>(OnReturnToMainMenuRequested);
            Bus.Global.Off<RestartRequestedEvent>(OnRestartRequested);
            Bus.Global.Off<JumpToLevelRequestedEvent>(OnJumpToLevelRequested);
            Bus.Global.Off<PauseRequestedEvent>(OnPauseRequested);
            Bus.Global.Off<ForceOutcomeRequestedEvent>(OnForceOutcomeRequested);
        }

        internal void SetPhase(AppFlowPhase phase)
        {
            if (CurrentPhase == phase) return;

            CurrentPhase = phase;

            // Chỗ DUY NHẤT phase đổi — bắn ra để view trong scene tự phản ứng
            // (HUD ẩn/hiện, overlay debug) mà không phải inject AppFlow.
            Bus.Global.Fire(new AppFlowPhaseChangedEvent(phase));
        }

        internal async Awaitable<bool> TriggerAsync(IAppFlowTrigger trigger)
        {
            bool didTransition = await _stateMachine.TriggerAsync(trigger);

            if (!didTransition)
                _logger.Warn($"[AppFlow] Trigger '{trigger.GetType().Name}' bị state hiện tại bỏ qua.");

            return didTransition;
        }

        private void OnOutcomePublished(GameplayOutcomePublishedEvent evt)
        {
            if (CurrentPhase != AppFlowPhase.Gameplay)
            {
                _logger.Warn($"[AppFlow] Bỏ qua kết quả ngoài phase Gameplay (đang {CurrentPhase}).");
                return;
            }

            _context.SetLastResult(evt.Result);
            _context.TriggerDeferred(new LevelFinishedTrigger());
        }

        private void OnRestartRequested(RestartRequestedEvent evt)
        {
            if (CurrentPhase != AppFlowPhase.Gameplay && CurrentPhase != AppFlowPhase.Result)
            {
                _logger.Warn($"[AppFlow] Bỏ qua yêu cầu chơi lại ở phase {CurrentPhase}.");
                return;
            }

            _context.RequestRetryGated();   // hết tim → NoHeartsPopup thay vì vào màn
        }

        private void OnPauseRequested(PauseRequestedEvent evt)
        {
            if (CurrentPhase != AppFlowPhase.Gameplay)
            {
                _logger.Warn($"[AppFlow] Bỏ qua Pause ở phase {CurrentPhase}.");
                return;
            }

            ShowPausePopupInBackground();
        }

        private async void ShowPausePopupInBackground()
        {
            try
            {
                await _context.ShowPausePopupAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[AppFlow] Mở PausePopup thất bại.");
            }
        }

        private void OnForceOutcomeRequested(ForceOutcomeRequestedEvent evt)
        {
            if (CurrentPhase != AppFlowPhase.Gameplay)
            {
                _logger.Warn($"[AppFlow] Bỏ qua ForceOutcome ở phase {CurrentPhase}.");
                return;
            }

            ForceOutcomeInBackground(evt.IsWin);
        }

        private async void ForceOutcomeInBackground(bool isWin)
        {
            try
            {
                await _context.ForceOutcomeAsync(isWin);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[AppFlow] Ép kết quả thất bại.");
            }
        }

        private void OnJumpToLevelRequested(JumpToLevelRequestedEvent evt)
        {
            int index = evt.OneBasedLevelNumber - 1;
            if (index < 0) index = 0;

            switch (CurrentPhase)
            {
                case AppFlowPhase.MainMenu:
                    _context.SetCurrentLevelIndex(index);
                    _context.TriggerDeferred(new StartGameplayTrigger());
                    break;

                case AppFlowPhase.Gameplay:
                case AppFlowPhase.Result:
                    _context.SetCurrentLevelIndex(index);
                    // GameplayState nhận RetryTrigger bằng instance mới → OnEnter
                    // chạy lại → StartCurrentLevelAsync đọc CurrentLevel vừa ghi.
                    _context.TriggerDeferred(new RetryTrigger());
                    break;

                default:
                    _logger.Warn($"[AppFlow] Bỏ qua JumpToLevel ở phase {CurrentPhase}.");
                    break;
            }
        }

        private void OnReturnToMainMenuRequested(ReturnToMainMenuRequestedEvent evt)
        {
            if (CurrentPhase != AppFlowPhase.Gameplay && CurrentPhase != AppFlowPhase.Result)
            {
                _logger.Warn($"[AppFlow] Bỏ qua yêu cầu về MainMenu ở phase {CurrentPhase}.");
                return;
            }

            _context.TriggerDeferred(new ReturnToMainMenuTrigger());
        }
    }
}
