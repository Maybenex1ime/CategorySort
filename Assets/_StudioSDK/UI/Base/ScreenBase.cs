using LogosSDK.UI.Animation;
using LogosSDK.UI.Core;
using LogosSDK.UI.Transitions;
using Reflex.Attributes;
using UnityEngine;

namespace LogosSDK.UI.Base
{
    public abstract class ScreenBase : MonoBehaviour, IScreen
    {
        private IUITransition _transition;    // cached in Awake
        private RectTransform _rectTransform; // cached in Awake
        private CanvasGroup _canvasGroup;     // cached in Awake, may be null

        [Inject] private IUIAnimationService _animationService; // injected in Start, may be null

        protected virtual void Awake()
        {
            _rectTransform = transform as RectTransform;
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            TryGetComponent(out _transition);
            TryGetComponent(out _canvasGroup);
        }

        public virtual async Awaitable Show(object args = null)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            gameObject.SetActive(true);
            if (_transition != null)
            {
                await _transition.PlayEnter(_rectTransform);
            }
            else if (_animationService != null)
            {
                bool isPopup = this is IPopup;
                var profile = _animationService.GetPanelProfile(isPopup);
                await _animationService.PlayPanelEnter(_rectTransform, _canvasGroup, profile);
            }
        }

        public virtual async Awaitable Hide()
        {
            if (_transition != null)
            {
                await _transition.PlayExit(_rectTransform);
            }
            else if (_animationService != null)
            {
                bool isPopup = this is IPopup;
                var profile = _animationService.GetPanelProfile(isPopup);
                await _animationService.PlayPanelExit(_rectTransform, _canvasGroup, profile);
            }
            gameObject.SetActive(false);
        }

        public virtual void OnBecameTop() { }
        public virtual void OnLostTop() { }
    }
}
