using System;
using LogosSDK.Core.AppFlow;
using LogosSDK.Core.Events;
using LogosSDK.Core.FSM;
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
            ICoinRewardService coinReward = null)
        {
            if (uiManager == null)
                throw new ArgumentNullException(nameof(uiManager));

            _stateMachine = new StateMachine<IAppFlowState, IAppFlowTrigger>();
            _context = new AppFlowContext(this, uiManager, minLoadingSeconds, saveManager, coinReward);

            // MetaSession chuyển tiếp LevelSignals.Finished lên bus. Nó lo tim/coin/
            // progression; AppFlow chỉ nghe để chuyển sang Result — hai bên không đụng nhau.
            Bus.Global.On<LevelResultEvent>(OnLevelResult);
            Bus.Global.On<ReturnToMainMenuRequestedEvent>(OnReturnToMainMenuRequested);
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
            Bus.Global.Off<LevelResultEvent>(OnLevelResult);
            Bus.Global.Off<ReturnToMainMenuRequestedEvent>(OnReturnToMainMenuRequested);
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

        private void OnLevelResult(LevelResultEvent evt)
        {
            if (CurrentPhase != AppFlowPhase.Gameplay)
            {
                _logger.Warn($"[AppFlow] Bỏ qua LevelResultEvent ngoài phase Gameplay (đang {CurrentPhase}).");
                return;
            }

            _context.SetLastResult(evt);
            _context.TriggerDeferred(new LevelFinishedTrigger());
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
