using System;
using LogosSDK.Core.Logging;
using Reflex.Attributes;
using Reflex.Core;
using Reflex.Exceptions;
using UnityEngine;
using UnityEngine.InputSystem;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.UI.Core
{
    public sealed class BackButtonListener : MonoBehaviour
    {
        private static readonly ILogger _logger = LogManager.GetLogger<BackButtonListener>();

        [Inject] private readonly Container _container;

        private INavigationService _navigationService;
        private bool _didResolveNavigation;

        private void Update()
        {
            if (!WasBackPressedThisFrame())
            {
                return;
            }

            INavigationService navigationService = ResolveNavigationService();
            if (navigationService == null)
            {
                return;
            }

            navigationService.HandleBackButton();
        }

        private INavigationService ResolveNavigationService()
        {
            if (_navigationService != null)
            {
                return _navigationService;
            }

            if (_didResolveNavigation)
            {
                return null;
            }

            if (_container == null)
            {
                _logger.Error("[BackButtonListener] Reflex container is not available.");
                return null;
            }

            try
            {
                _navigationService = _container.Resolve<INavigationService>();
                _didResolveNavigation = true;
            }
            catch (UnknownContractException)
            {
                _logger.Error("[BackButtonListener] INavigationService is not available.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[BackButtonListener] Failed to resolve navigation service.");
            }

            return _navigationService;
        }

        private static bool WasBackPressedThisFrame()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return false;
#endif
        }
    }
}
