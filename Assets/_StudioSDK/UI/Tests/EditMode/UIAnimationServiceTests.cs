using NUnit.Framework;
using LogosSDK.UI.Animation;
using UnityEngine;

namespace LogosSDK.UI.Tests.EditMode
{
    [Category("UnitTest")]
    public class UIAnimationServiceTests
    {
        private UIAnimationSettingsSO _settings;
        private UIPanelAnimationSO _panelProfile;
        private UIPanelAnimationSO _popupProfile;
        private UIButtonFeedbackSO _buttonProfile;
        private UIAnimationService _service;

        [SetUp]
        public void SetUp()
        {
            _panelProfile  = ScriptableObject.CreateInstance<UIPanelAnimationSO>();
            _popupProfile  = ScriptableObject.CreateInstance<UIPanelAnimationSO>();
            _buttonProfile = ScriptableObject.CreateInstance<UIButtonFeedbackSO>();
            _settings      = ScriptableObject.CreateInstance<UIAnimationSettingsSO>();
            _settings.SetForTest(_panelProfile, _popupProfile, _buttonProfile);
            _service = new UIAnimationService(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_panelProfile);
            Object.DestroyImmediate(_popupProfile);
            Object.DestroyImmediate(_buttonProfile);
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void GetPanelProfile_ReturnsPanelProfile_WhenNotPopup()
        {
            var result = _service.GetPanelProfile(isPopup: false);
            Assert.AreSame(_panelProfile, result);
        }

        [Test]
        public void GetPanelProfile_ReturnsPopupProfile_WhenIsPopup()
        {
            var result = _service.GetPanelProfile(isPopup: true);
            Assert.AreSame(_popupProfile, result);
        }

        [Test]
        public void GetButtonProfile_ReturnsButtonProfile()
        {
            var result = _service.GetButtonProfile();
            Assert.AreSame(_buttonProfile, result);
        }

        [Test]
        public void GetPanelProfile_ReturnsSameInstance_OnRepeatedCalls()
        {
            var first  = _service.GetPanelProfile(isPopup: false);
            var second = _service.GetPanelProfile(isPopup: false);
            Assert.AreSame(first, second);
        }

        [Test]
        public void GetButtonProfile_ReturnsSameInstance_OnRepeatedCalls()
        {
            var first  = _service.GetButtonProfile();
            var second = _service.GetButtonProfile();
            Assert.AreSame(first, second);
        }
    }
}
