using LitMotion;
using LitMotion.Extensions;
using LogosSDK.UI.Animation;
using Reflex.Attributes;
using UnityEngine;

namespace LogosSDK.UI.Components
{
    public sealed class UIIdlePulseDriver : MonoBehaviour
    {
        [SerializeField] private UIButtonFeedbackSO _profile;
        [SerializeField] private RectTransform _target;

        [Inject] private IUIAnimationService _animationService;

        private UIButtonFeedbackSO _resolvedProfile;
        private RectTransform _resolvedTarget;
        private MotionHandle _pulseTween;
        private MotionHandle _wobbleTween;
        private WaitForSeconds _wobbleWait;

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

            // idleThreshold=0 → continuous jiggle, space repeats by animation duration
            // idleThreshold>0 → idle CTA hint, fire every N seconds
            float wobbleInterval = _resolvedProfile.IdleThreshold > 0f
                ? _resolvedProfile.IdleThreshold
                : _resolvedProfile.WobbleDuration + 0.1f;
            _wobbleWait = new WaitForSeconds(wobbleInterval);

            if (gameObject.activeInHierarchy && enabled)
            {
                if (_resolvedProfile.IdlePulseEnabled) StartPulse();
                if (_resolvedProfile.IdleWobbleEnabled) StartCoroutine(WobbleRoutine());
            }
        }

        private void OnEnable()
        {
            if (_resolvedProfile == null) return;
            if (_resolvedProfile.IdlePulseEnabled) StartPulse();
            if (_resolvedProfile.IdleWobbleEnabled) StartCoroutine(WobbleRoutine());
        }

        private void OnDisable()
        {
            _pulseTween.TryCancel();
            _wobbleTween.TryCancel();
            StopAllCoroutines();
            if (_resolvedTarget != null)
                _resolvedTarget.localScale = Vector3.one;
        }

        public void NotifyPressed()
        {
            if (_resolvedProfile == null || !_resolvedProfile.IdleWobbleEnabled) return;
            StopAllCoroutines();
            StartCoroutine(WobbleRoutine());
        }

        private void StartPulse()
        {
            _pulseTween.TryCancel();
            _pulseTween = LMotion.Create(_resolvedTarget.localScale, Vector3.one * _resolvedProfile.PulseScale, _resolvedProfile.PulseDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithCancelOnError()
                .BindToLocalScale(_resolvedTarget)
                .AddTo(gameObject);
        }

        private System.Collections.IEnumerator WobbleRoutine()
        {
            while (true)
            {
                yield return _wobbleWait;
                if (!enabled || !gameObject.activeInHierarchy) yield break;
                _wobbleTween.TryCancel();
                // DOPunchRotation(…, vibrato 10, elasticity 0.5). Công thức punch LitMotion khác
                // bản cũ — Task 8 so bằng mắt.
                _wobbleTween = LMotion.Punch.Create(_resolvedTarget.localEulerAngles, new Vector3(0f, 0f, _resolvedProfile.WobbleAngle), _resolvedProfile.WobbleDuration)
                    .WithFrequency(10)
                    .WithDampingRatio(0.5f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .WithCancelOnError()
                    .BindToLocalEulerAngles(_resolvedTarget)
                    .AddTo(gameObject);
            }
        }
    }
}
