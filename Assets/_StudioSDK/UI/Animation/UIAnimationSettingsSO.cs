using UnityEngine;

namespace LogosSDK.UI.Animation
{
    [CreateAssetMenu(menuName = "LogosSDK/UI/Animation Settings", fileName = "SO_UIAnimationSettings")]
    public sealed class UIAnimationSettingsSO : ScriptableObject
    {
        [Header("Panel Profiles")]
        [SerializeField] private UIPanelAnimationSO _defaultPanelProfile;
        [SerializeField] private UIPanelAnimationSO _defaultPopupProfile;

        [Header("Stagger Profile")]
        [SerializeField] private UIStaggerAnimationSO _defaultStaggerProfile;

        [Header("Button Profile")]
        [SerializeField] private UIButtonFeedbackSO _defaultButtonProfile;

        public UIPanelAnimationSO DefaultPanelProfile => _defaultPanelProfile;
        public UIPanelAnimationSO DefaultPopupProfile => _defaultPopupProfile;
        public UIStaggerAnimationSO DefaultStaggerProfile => _defaultStaggerProfile;
        public UIButtonFeedbackSO DefaultButtonProfile => _defaultButtonProfile;

#if UNITY_EDITOR
        public void SetForTest(
            UIPanelAnimationSO panelProfile,
            UIPanelAnimationSO popupProfile,
            UIButtonFeedbackSO buttonProfile,
            UIStaggerAnimationSO staggerProfile = null)
        {
            _defaultPanelProfile = panelProfile;
            _defaultPopupProfile = popupProfile;
            _defaultButtonProfile = buttonProfile;
            _defaultStaggerProfile = staggerProfile;
        }
#endif
    }
}
