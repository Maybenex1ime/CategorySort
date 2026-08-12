using DG.Tweening;
using UnityEngine;

namespace LogosSDK.UI.Animation
{
    [CreateAssetMenu(menuName = "LogosSDK/UI/Panel Animation", fileName = "SO_Panel_")]
    public sealed class UIPanelAnimationSO : ScriptableObject
    {
        [Header("Enter")]
        [SerializeField] private PanelEnterType _enterType = PanelEnterType.ScaleFade;
        [SerializeField] private float _enterDuration = 0.22f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;

        [Header("Exit")]
        [SerializeField] private PanelExitType _exitType = PanelExitType.ScaleFade;
        [SerializeField] private float _exitDuration = 0.18f;
        [SerializeField] private Ease _exitEase = Ease.InBack;

        public PanelEnterType EnterType => _enterType;
        public float EnterDuration => _enterDuration;
        public Ease EnterEase => _enterEase;

        public PanelExitType ExitType => _exitType;
        public float ExitDuration => _exitDuration;
        public Ease ExitEase => _exitEase;

#if UNITY_EDITOR
        public void SetForTest(
            PanelEnterType enterType = PanelEnterType.ScaleFade,
            float enterDuration = 0.22f,
            Ease enterEase = Ease.OutBack,
            PanelExitType exitType = PanelExitType.ScaleFade,
            float exitDuration = 0.18f,
            Ease exitEase = Ease.InBack)
        {
            _enterType = enterType;
            _enterDuration = enterDuration;
            _enterEase = enterEase;
            _exitType = exitType;
            _exitDuration = exitDuration;
            _exitEase = exitEase;
        }
#endif
    }
}
