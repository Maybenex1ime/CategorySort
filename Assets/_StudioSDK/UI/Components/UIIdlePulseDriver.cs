using DG.Tweening;
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
        private Tween _pulseTween;
        private Tween _wobbleTween;
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
            _pulseTween?.Kill();
            _wobbleTween?.Kill();
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
            _pulseTween?.Kill();
            _pulseTween = _resolvedTarget
                .DOScale(_resolvedProfile.PulseScale, _resolvedProfile.PulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        private System.Collections.IEnumerator WobbleRoutine()
        {
            while (true)
            {
                yield return _wobbleWait;
                if (!enabled || !gameObject.activeInHierarchy) yield break;
                _wobbleTween?.Kill();
                _wobbleTween = _resolvedTarget
                    .DOPunchRotation(new Vector3(0f, 0f, _resolvedProfile.WobbleAngle), _resolvedProfile.WobbleDuration, 10, 0.5f)
                    .SetLink(gameObject)
                    .SetUpdate(true);
            }
        }
    }
}
