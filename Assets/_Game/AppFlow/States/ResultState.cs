using LogosSDK.Core.Logging;
using UnityEngine;
using WordStack.Meta.AppFlow.Triggers;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta.AppFlow.States
{
    internal sealed class ResultState : AppFlowStateBase
    {
        private static readonly ILogger _logger = LogManager.GetLogger<ResultState>();

        public ResultState(AppFlowContext context) : base(context)
        {
            // NextLevel và Retry đi cùng một đường: quay lại Gameplay, ở đó
            // StartCurrentLevel() đọc LevelProgressData.CurrentLevel. Thắng thì
            // ProgressionService đã tăng nó nên nạp màn kế; thua thì giữ nguyên
            // nên nạp lại màn cũ. Không cần rẽ nhánh ở đây.
            RegisterTriggerHandler<NextLevelTrigger>(_ => new GameplayState(Context));
            RegisterTriggerHandler<RetryTrigger>(_ => new GameplayState(Context));
            RegisterTriggerHandler<ReturnToMainMenuTrigger>(_ => new MainMenuState(Context));
        }

        public override async Awaitable OnEnterAsync()
        {
            _logger.Info("[AppFlow] Enter Result");
            Context.SetPhase(AppFlowPhase.Result);
            await Context.ShowResultPopupAsync();
        }

        public override Awaitable OnExitAsync()
        {
            Context.DismissActivePopup();
            return AwaitableUtility.Completed();
        }
    }
}
