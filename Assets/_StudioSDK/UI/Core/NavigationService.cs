using System;
using LogosSDK.Core.AppFlow;
using LogosSDK.Core.Logging;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.UI.Core
{
    public sealed class NavigationService : INavigationService
    {
        private static readonly ILogger _logger = LogManager.GetLogger<NavigationService>();

        private readonly UIManager _uiManager;
        private readonly IAppFlowManager _appFlowManager;

        public NavigationService(UIManager uiManager, IAppFlowManager appFlowManager)
        {
            if (uiManager == null)
            {
                throw new ArgumentNullException(nameof(uiManager));
            }

            if (appFlowManager == null)
            {
                throw new ArgumentNullException(nameof(appFlowManager));
            }

            _uiManager = uiManager;
            _appFlowManager = appFlowManager;
        }

        public void HandleBackButton()
        {
            if (_uiManager.IsPopupActive)
            {
                _uiManager.DismissCurrentPopup();
                return;
            }

            if (_uiManager.CanPopScreen)
            {
                PopScreenInBackground();
                return;
            }

            HandleRootBackInBackground();
        }

        private async void PopScreenInBackground()
        {
            try
            {
                await _uiManager.PopScreen();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[NavigationService] Back navigation failed.");
            }
        }

        private async void HandleRootBackInBackground()
        {
            try
            {
                await _appFlowManager.HandleRootBackAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[NavigationService] Back navigation failed.");
            }
        }
    }
}
