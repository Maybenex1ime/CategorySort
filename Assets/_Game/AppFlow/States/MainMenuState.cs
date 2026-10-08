using LogosSDK.Core.Logging;
using UnityEngine;
using WordStack.Meta.AppFlow.Triggers;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta.AppFlow.States
{
    internal sealed class MainMenuState : AppFlowStateBase
    {
        private static readonly ILogger _logger = LogManager.GetLogger<MainMenuState>();

        public MainMenuState(AppFlowContext context) : base(context)
        {
            RegisterTriggerHandler<StartGameplayTrigger>(_ => new GameplayState(Context));
        }

        public override async Awaitable OnEnterAsync()
        {
            _logger.Info("[AppFlow] Enter MainMenu");

            // Từ màn chơi về thì che màn; từ Splash vào thì không (loading đã tự đóng, che thêm là chớp đen).
            bool fade = Context.IsInGame;
            if (fade) await Context.CoverScreenAsync();
            try
            {
                Context.SetPhase(AppFlowPhase.MainMenu);
                await Context.ShowMainMenuScreenAsync();
            }
            finally
            {
                if (fade) await Context.RevealScreenAsync();
            }
        }
    }
}
