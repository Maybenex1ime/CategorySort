using UnityEngine;

namespace LogosSDK.Core.AppFlow
{
    public interface IAppFlowManager
    {
        Awaitable StartAsync();
        Awaitable HandleRootBackAsync();
    }
}
