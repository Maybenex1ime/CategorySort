using LogosSDK.Core.Logging;
using UnityEngine;
using WordStack.Meta.AppFlow.Triggers;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace WordStack.Meta.AppFlow.States
{
    internal sealed class BootState : AppFlowStateBase
    {
        private static readonly ILogger _logger = LogManager.GetLogger<BootState>();

        public BootState(AppFlowContext context) : base(context)
        {
            RegisterTriggerHandler<BootToSplashTrigger>(_ => new SplashState(Context));
        }

        public override Awaitable OnEnterAsync()
        {
            _logger.Info("[AppFlow] Enter Boot");
            Context.SetPhase(AppFlowPhase.Boot);
            Context.TriggerDeferred(new BootToSplashTrigger());
            return AwaitableUtility.Completed();
        }
    }
}
