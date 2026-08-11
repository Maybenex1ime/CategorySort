// Replaced by UIPanelAnimationSO, UIStaggerAnimationSO, UIButtonFeedbackSO.
// Delete this file from Unity Editor (right-click → Delete) once existing prefab
// references have been re-wired to the new SO types.
using DG.Tweening;
using UnityEngine;

namespace LogosSDK.UI.Animation
{
    [CreateAssetMenu(menuName = "LogosSDK/UI/Animation Profile", fileName = "SO_Profile_")]
    public sealed class UIAnimationProfileSO : ScriptableObject
    {
        [Header("Panel Enter")]
        [SerializeField] private PanelEnterType _enterType = PanelEnterType.ScaleFade;
        [SerializeField] private float _enterDuration = 0.22f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;

        [Header("Panel Exit")]
        [SerializeField] private PanelExitType _exitType = PanelExitType.ScaleFade;
        [SerializeField] private float _exitDuration = 0.18f;
        [SerializeField] private Ease _exitEase = Ease.InBack;

        [Header("Element Stagger")]
        [SerializeField] private StaggerType _staggerType = StaggerType.Pop;
        [SerializeField] private float _delayBetweenElements = 0.07f;
        [SerializeField] private float _elementDuration = 0.18f;
        [SerializeField] private Ease _elementEase = Ease.OutBack;

        [Header("Button Press")]
        [SerializeField] private float _pressScale = 0.88f;
        [SerializeField] private float _pressDuration = 0.08f;
        [SerializeField] private float _releaseDuration = 0.14f;
        [SerializeField] private Ease _releaseEase = Ease.OutBack;

        [Header("Button Idle Pulse")]
        [SerializeField] private bool _idlePulseEnabled = true;
        [SerializeField] private float _pulseScale = 1.06f;
        [SerializeField] private float _pulseDuration = 0.9f;

        [Header("Button Idle Wobble Hint")]
        [SerializeField] private bool _idleWobbleEnabled = false;
        [SerializeField] private float _wobbleAngle = 8f;
        [SerializeField] private float _wobbleDuration = 0.4f;
        [SerializeField] private float _idleThreshold = 3f;

        [Header("Toggle Feedback")]
        [SerializeField] private float _toggleOnScale = 1.12f;
        [SerializeField] private Ease _toggleOnEase = Ease.OutBack;
        [SerializeField] private bool _toggleOffShake = true;

        public PanelEnterType EnterType => _enterType;
        public float EnterDuration => _enterDuration;
        public Ease EnterEase => _enterEase;

        public PanelExitType ExitType => _exitType;
        public float ExitDuration => _exitDuration;
        public Ease ExitEase => _exitEase;

        public StaggerType StaggerType => _staggerType;
        public float DelayBetweenElements => _delayBetweenElements;
        public float ElementDuration => _elementDuration;
        public Ease ElementEase => _elementEase;

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
            PanelEnterType enterType = PanelEnterType.ScaleFade,
            float enterDuration = 0.22f,
            PanelExitType exitType = PanelExitType.ScaleFade,
            float exitDuration = 0.18f,
            StaggerType staggerType = StaggerType.Pop,
            float delayBetweenElements = 0.07f,
            float elementDuration = 0.18f,
            float pressScale = 0.88f,
            float pressDuration = 0.08f,
            float releaseDuration = 0.14f)
        {
            _enterType = enterType;
            _enterDuration = enterDuration;
            _exitType = exitType;
            _exitDuration = exitDuration;
            _staggerType = staggerType;
            _delayBetweenElements = delayBetweenElements;
            _elementDuration = elementDuration;
            _pressScale = pressScale;
            _pressDuration = pressDuration;
            _releaseDuration = releaseDuration;
        }
#endif
    }
}
