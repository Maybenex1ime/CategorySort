using System.Collections.Generic;
using UnityEngine;

namespace LogosSDK.UI.Animation
{
    public interface IUIAnimationService
    {
        UIPanelAnimationSO GetPanelProfile(bool isPopup);
        UIButtonFeedbackSO GetButtonProfile();
        Awaitable PlayPanelEnter(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile);
        Awaitable PlayPanelExit(RectTransform rt, CanvasGroup cg, UIPanelAnimationSO profile);
        Awaitable PlayStagger(IReadOnlyList<RectTransform> elements, UIStaggerAnimationSO profile);
    }
}
