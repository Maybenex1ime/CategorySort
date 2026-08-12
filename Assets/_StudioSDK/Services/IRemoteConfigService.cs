using UnityEngine;

namespace LogosSDK.Services
{
    public interface IRemoteConfigService
    {
        T GetValue<T>(string key, T defaultValue);
        Awaitable FetchAsync();
    }
}
