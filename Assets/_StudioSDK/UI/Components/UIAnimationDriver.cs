using System.Collections.Generic;
using LogosSDK.UI.Animation;
using LogosSDK.UI.Core;
using LogosSDK.UI.Transitions;
using Reflex.Attributes;
using UnityEngine;

namespace LogosSDK.UI.Components
{
    public sealed class UIAnimationDriver : MonoBehaviour, IUITransition
    {
        [SerializeField] private UIPanelAnimationSO _panelProfile;
        [SerializeField] private UIStaggerAnimationSO _staggerProfile;
        [SerializeField] private List<RectTransform> _staggerElements = new();

        [Inject] private IUIAnimationService _animationService;

        private UIPanelAnimationSO _resolvedPanelProfile;
        private UIStaggerAnimationSO _resolvedStaggerProfile;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            TryGetComponent(out _canvasGroup);
        }

        private void Start()
        {
            if (_animationService == null)
            {
                _resolvedPanelProfile = _panelProfile;
                _resolvedStaggerProfile = _staggerProfile;
                return;
            }
            bool isPopup = GetComponent<IPopup>() != null;
            _resolvedPanelProfile = _panelProfile != null ? _panelProfile : _animationService.GetPanelProfile(isPopup);
            _resolvedStaggerProfile = _staggerProfile;
        }

        public async Awaitable PlayEnter(RectTransform rectTransform)
        {
            if (_animationService == null || _resolvedPanelProfile == null) return;
            await _animationService.PlayPanelEnter(rectTransform, _canvasGroup, _resolvedPanelProfile);
            if (_staggerElements.Count > 0 && _resolvedStaggerProfile != null)
                await _animationService.PlayStagger(_staggerElements, _resolvedStaggerProfile);
        }

        public async Awaitable PlayExit(RectTransform rectTransform)
        {
            if (_animationService == null || _resolvedPanelProfile == null) return;
            await _animationService.PlayPanelExit(rectTransform, _canvasGroup, _resolvedPanelProfile);
        }
    }
}
