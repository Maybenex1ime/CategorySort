using System;
using UnityEngine;

namespace LogosSDK.UI.Core
{
    public interface IPopup
    {
        void SetArgs(object args);
        Awaitable Show();
        Awaitable Hide();
        event Action OnDismissed;
    }
}
