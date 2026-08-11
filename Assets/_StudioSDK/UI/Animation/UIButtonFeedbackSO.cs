using DG.Tweening;
using UnityEngine;

namespace LogosSDK.UI.Animation
{
    [CreateAssetMenu(menuName = "LogosSDK/UI/Button Feedback", fileName = "SO_Button_")]
    public sealed class UIButtonFeedbackSO : ScriptableObject
    {
        [Header("Press / Release")]
        [SerializeField] private float _pressScale = 0.88f;
        [SerializeField] private float _pressDuration = 0.08f;
        [SerializeField] private float _releaseDuration = 0.14f;
        [SerializeField] private Ease _releaseEase = Ease.OutBack;

        [Header("Idle Pulse (CTA breathe)")]
        [SerializeField] private bool _idlePulseEnabled = false;
        [SerializeField] private float _pulseScale = 1.06f;
        [SerializeField] private float _pulseDuration = 0.9f;

        [Header("Wobble / Jiggle")]
        [SerializeField] private bool _idleWobbleEnabled = false;
        [SerializeField] private float _wobbleAngle = 8f;
        [SerializeField] private float _wobbleDuration = 0.4f;
        [Tooltip("0 = loop continuously (jiggle for images). >0 = fire once after this delay (idle hint for CTA buttons).")]
        [SerializeField] private float _idleThreshold = 3f;

        [Header("Toggle (music / haptic)")]
        [SerializeField] private float _toggleOnScale = 1.12f;
        [SerializeField] private Ease _toggleOnEase = Ease.OutBack;
        [SerializeField] private bool _toggleOffShake = true;

        public float PressScale => _pressScale;
        public float PressDuration => _pressDuration;
        public float ReleaseDuration => _releaseDuration;
        public Ease ReleaseEase => _releaseEase;

        public bool IdlePulseEnabled => _idlePulseEnabled;
        public float PulseScale => _pulseScale;
        public float PulseDuration => _pulseDuration;

        public bool IdleWobbleEnabled => _idleWobbleEnabled;
        public float WobbleAngle => _wobbleAngle;
        public float WobbleDuration => _wobbleDuration;
        public float IdleThreshold => _idleThreshold;

        public float ToggleOnScale => _toggleOnScale;
        public Ease ToggleOnEase => _toggleOnEase;
        public bool ToggleOffShake => _toggleOffShake;

#if UNITY_EDITOR
        public void SetForTest(
            float pressScale = 0.88f,
            float pressDuration = 0.08f,
            float releaseDuration = 0.14f,
            Ease releaseEase = Ease.OutBack,
            bool idlePulseEnabled = false,
            float pulseScale = 1.06f,
            float pulseDuration = 0.9f,
            bool idleWobbleEnabled = false,
            float wobbleAngle = 8f,
            float wobbleDuration = 0.4f,
            float idleThreshold = 3f,
            float toggleOnScale = 1.12f,
            Ease toggleOnEase = Ease.OutBack,
            bool toggleOffShake = true)
        {
            _pressScale = pressScale;
            _pressDuration = pressDuration;
            _releaseDuration = releaseDuration;
            _releaseEase = releaseEase;
            _idlePulseEnabled = idlePulseEnabled;
            _pulseScale = pulseScale;
            _pulseDuration = pulseDuration;
            _idleWobbleEnabled = idleWobbleEnabled;
            _wobbleAngle = wobbleAngle;
            _wobbleDuration = wobbleDuration;
            _idleThreshold = idleThreshold;
            _toggleOnScale = toggleOnScale;
            _toggleOnEase = toggleOnEase;
            _toggleOffShake = toggleOffShake;
        }
#endif
    }
}
