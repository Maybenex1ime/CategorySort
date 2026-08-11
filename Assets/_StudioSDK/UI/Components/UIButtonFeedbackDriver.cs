using DG.Tweening;
using LogosSDK.UI.Animation;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LogosSDK.UI.Components
{
    public sealed class UIButtonFeedbackDriver : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private UIButtonFeedbackSO _profile;
        [SerializeField] private RectTransform _target;

        [Inject] private IUIAnimationService _animationService;

        private UIButtonFeedbackSO _resolvedProfile;
        private RectTransform _resolvedTarget;
        private Tween _activeTween;

        private void Awake()
        {
            _resolvedTarget = _target != null ? _target : GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (_animationService == null)
            {
                _resolvedProfile = _profile;
                return;
            }
            _resolvedProfile = _profile != null ? _profile : _animationService.GetButtonProfile();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_resolvedTarget == null || _resolvedProfile == null) return;
            _activeTween?.Kill();
            _activeTween = _resolvedTarget
                .DOScale(_resolvedProfile.PressScale, _resolvedProfile.PressDuration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_resolvedTarget == null || _resolvedProfile == null) return;
            _activeTween?.Kill();
            _activeTween = _resolvedTarget
                .DOScale(1f, _resolvedProfile.ReleaseDuration)
                .SetEase(_resolvedProfile.ReleaseEase)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        private void OnDisable()
        {
            _activeTween?.Kill();
            if (_resolvedTarget != null)
                _resolvedTarget.localScale = Vector3.one;
        }
    }
}
