using LogosSDK.Core.Logging;
using UnityEngine;
using WordStack.Meta.AppFlow.Triggers;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta.AppFlow.States
{
    internal sealed class GameplayState : AppFlowStateBase
    {
        private static readonly ILogger _logger = LogManager.GetLogger<GameplayState>();

        public GameplayState(AppFlowContext context) : base(context)
        {
            RegisterTriggerHandler<LevelFinishedTrigger>(_ => new ResultState(Context));
            // Quit GIỮA màn mất 1 tim — trừ ở đây để mọi đường về menu từ gameplay
            // (PausePopup Quit, nút back, bus event) cùng qua một cửa, không né được
            // bằng Quit + Play lại. Về menu từ ResultState là handler khác, không dính.
            // NoHeartsPopup đóng lúc 0 tim cũng đi đường này: ConsumeOne no-op ở 0.
            RegisterTriggerHandler<ReturnToMainMenuTrigger>(_ =>
            {
                Context.ConsumeHeart("Quit giữa màn");
                return new MainMenuState(Context);
            });

            // Chơi lại GIỮA màn (không qua Result). Instance mới nên StateMachine
            // coi là chuyển state thật: OnExit rồi OnEnter → nạp lại bàn.
            RegisterTriggerHandler<RetryTrigger>(_ => new GameplayState(Context));
        }

        public override async Awaitable OnEnterAsync()
        {
            _logger.Info("[AppFlow] Enter Gameplay");
            Context.SetPhase(AppFlowPhase.Gameplay);

            // Gỡ màn hình menu để lộ bàn chơi. Gameplay nằm cùng scene nên
            // không có gì để nạp — khác aquapark (nó load GameplayScene qua Addressables).
            await Context.RemoveCurrentScreenAsync();

            // Đây là NƠI DUY NHẤT ra lệnh nạp màn — cả vào lần đầu, chơi lại,
            // lẫn sang màn kế đều đi qua đây.
            await Context.StartCurrentLevelAsync();
        }
    }
}
