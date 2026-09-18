using LitMotion;
using LitMotion.Extensions;
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

        public void SetState(bool on)
        {
            if (_resolvedProfile == null || _resolvedTarget == null) return;
            _activeTween.TryCancel();
            if (on)
            {
                var up = Vector3.one * _resolvedProfile.ToggleOnScale;
                _activeTween = LSequence.Create()
                    .Insert(0f, LMotion.Create(_resolvedTarget.localScale, up, 0.1f)
                                       .WithEase(_resolvedProfile.ToggleOnEase).WithCancelOnError().BindToLocalScale(_resolvedTarget))
                    .Insert(0.1f, LMotion.Create(up, Vector3.one, 0.08f)
                                         .WithEase(Ease.OutQuad).WithCancelOnError().BindToLocalScale(_resolvedTarget))
                    .Run(b => b.WithCancelOnError().WithScheduler(MotionScheduler.UpdateIgnoreTimeScale))
                    .AddTo(gameObject);
            }
            else if (_resolvedProfile.ToggleOffShake)
            {
                // DOShakeScale(0.25f, 0.12f, vibrato 8, randomness 90). Công thức shake khác. DampingRatio
                // chọn để biên độ tắt còn ~5% lúc hết giờ — điểm khởi đầu cho Task 8 so mắt, chưa chốt.
                _activeTween = LMotion.Shake.Create(_resolvedTarget.localScale, Vector3.one * 0.12f, 0.25f)
                    .WithFrequency(8)
                    .WithDampingRatio(2.4f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .WithCancelOnError()
                    .BindToLocalScale(_resolvedTarget)
                    .AddTo(gameObject);
            }
        }

        private void OnDisable()
        {
            _activeTween.TryCancel();
            if (_resolvedTarget != null)
                _resolvedTarget.localScale = Vector3.one;
        }
    }
}
