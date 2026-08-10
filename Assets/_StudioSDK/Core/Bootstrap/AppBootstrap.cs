using System;
using LogosSDK.Core.AppFlow;
using LogosSDK.Core.Logging;
using Reflex.Attributes;
using Reflex.Core;
using Reflex.Exceptions;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.Core.Bootstrap
{
    public class AppBootstrap : MonoBehaviour
    {
        private static readonly ILogger _logger = LogManager.GetLogger<AppBootstrap>();

        [Inject] private readonly Container _container;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SafeStart();
        }

        private async void SafeStart()
        {
            if (_container == null)
            {
                _logger.Error("[AppBootstrap] Reflex container is not available.");
                return;
            }

            IAppFlowManager appFlowManager;

            try
            {
                appFlowManager = _container.Resolve<IAppFlowManager>();
            }
            catch (UnknownContractException)
            {
                _logger.Error("[AppBootstrap] IAppFlowManager is not available.");
                return;
            }

            try
            {
                await appFlowManager.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[AppBootstrap] Failed to start app flow.");
            }
        }
    }
}
