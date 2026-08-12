using UnityEngine;

namespace LogosSDK.UI.Core
{
    public interface IScreen
    {
        Awaitable Show(object args = null);
        Awaitable Hide();
        void OnBecameTop();
        void OnLostTop();
    }
}
