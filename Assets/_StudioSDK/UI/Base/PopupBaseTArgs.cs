using System;
using UnityEngine;
using LogosSDK.UI.Animation;
using LogosSDK.UI.Core;
using LogosSDK.UI.Transitions;
using Reflex.Attributes;

namespace LogosSDK.UI.Base
{
    public abstract class PopupBase<TArgs> : MonoBehaviour, IPopup<TArgs>
    {
        private IUITransition _transition;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;

        [Inject] private IUIAnimationService _animationService;

        protected TArgs Args { get; private set; }

        public event Action OnDismissed;

        protected virtual void Awake()
        {
            _rectTransform = transform as RectTransform;
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            TryGetComponent(out _transition);
            TryGetComponent(out _canvasGroup);
        }

        public void SetArgs(object args)
        {
            Args = args != null ? (TArgs)args : default;
            Initialize(Args);
        }

        protected virtual void Initialize(TArgs args) { }

        public virtual async Awaitable Show()
        {
            gameObject.SetActive(true);
            if (_transition != null)
            {
                await _transition.PlayEnter(_rectTransform);
            }
            else if (_animationService != null)
            {
                var profile = _animationService.GetPanelProfile(isPopup: true);
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
                var profile = _animationService.GetPanelProfile(isPopup: true);
                await _animationService.PlayPanelExit(_rectTransform, _canvasGroup, profile);
            }
            gameObject.SetActive(false);
        }

        protected void Dismiss() => OnDismissed?.Invoke();
    }
}
