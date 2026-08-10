using UnityEngine;

namespace LogosSDK.Core.Services
{
    public interface IAssetService
    {
        Awaitable<T> LoadAsync<T>(string address) where T : Object;
        void Release<T>(T asset) where T : Object;
    }
}
