using LogosSDK.Core.Logging;
using UnityEngine;
using WordStack.Meta.AppFlow.Triggers;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta.AppFlow.States
{
    internal sealed class SplashState : AppFlowStateBase
    {
        private static readonly ILogger _logger = LogManager.GetLogger<SplashState>();

        public SplashState(AppFlowContext context) : base(context)
        {
            RegisterTriggerHandler<SplashToMainMenuTrigger>(_ => new MainMenuState(Context));
        }

        public override async Awaitable OnEnterAsync()
        {
            _logger.Info("[AppFlow] Enter Splash");
            Context.SetPhase(AppFlowPhase.Splash);

            await Context.ShowLoadingScreenAsync();

            // Nạp sẵn MainMenu trong lúc loading đang che, tránh khựng lúc chuyển.
            float start = Time.realtimeSinceStartup;
            await Context.PreWarmMainMenuScreenAsync();

            // Giữ loading tối thiểu — prewarm xong quá nhanh thì màn hình chỉ chớp một cái.
            float remaining = Context.MinLoadingSeconds - (Time.realtimeSinceStartup - start);
            if (remaining > 0f)
                await Awaitable.WaitForSecondsAsync(remaining);

            Context.TriggerDeferred(new SplashToMainMenuTrigger());
        }

        public override async Awaitable OnExitAsync()
        {
            // Gỡ LoadingScreen khi rời Splash — MainMenuState đẩy màn của nó lên sau.
            await Context.RemoveCurrentScreenAsync();
        }
    }
}
