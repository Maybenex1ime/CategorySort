using DG.Tweening;
using LogosSDK.UI.Animation;
using Reflex.Attributes;
using UnityEngine;

namespace LogosSDK.UI.Components
{
    public sealed class UIToggleFeedbackDriver : MonoBehaviour
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

        public void SetState(bool on)
        {
            if (_resolvedProfile == null || _resolvedTarget == null) return;
            _activeTween?.Kill();
            if (on)
            {
                _activeTween = DOTween.Sequence()
                    .Append(_resolvedTarget.DOScale(_resolvedProfile.ToggleOnScale, 0.1f).SetEase(_resolvedProfile.ToggleOnEase))
                    .Append(_resolvedTarget.DOScale(1f, 0.08f).SetEase(Ease.OutQuad))
                    .SetLink(gameObject)
                    .SetUpdate(true);
            }
            else if (_resolvedProfile.ToggleOffShake)
            {
                _activeTween = _resolvedTarget
                    .DOShakeScale(0.25f, 0.12f, 8, 90f)
                    .SetLink(gameObject)
                    .SetUpdate(true);
            }
        }

        private void OnDisable()
        {
            _activeTween?.Kill();
            if (_resolvedTarget != null)
                _resolvedTarget.localScale = Vector3.one;
        }
    }
}
