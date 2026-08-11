using UnityEngine;

namespace LogosSDK.UI.Transitions
{
    public interface IUITransition
    {
        Awaitable PlayEnter(RectTransform rectTransform);
        Awaitable PlayExit(RectTransform rectTransform);
    }
}
