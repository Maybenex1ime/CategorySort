using LitMotion;
using LitMotion.Extensions;
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
        private MotionHandle _activeTween;

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
            _activeTween.TryCancel();
            _activeTween = LMotion.Create(_resolvedTarget.localScale, Vector3.one * _resolvedProfile.PressScale, _resolvedProfile.PressDuration)
                .WithEase(Ease.Linear)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(_resolvedTarget)
                .AddTo(gameObject);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_resolvedTarget == null || _resolvedProfile == null) return;
            _activeTween.TryCancel();
            _activeTween = LMotion.Create(_resolvedTarget.localScale, Vector3.one, _resolvedProfile.ReleaseDuration)
                .WithEase(_resolvedProfile.ReleaseEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(_resolvedTarget)
                .AddTo(gameObject);
        }

        private void OnDisable()
        {
            _activeTween.TryCancel();
            if (_resolvedTarget != null)
                _resolvedTarget.localScale = Vector3.one;
        }
    }
}
