using UnityEngine;

namespace LogosSDK.Services
{
    public interface IIAPService
    {
        Awaitable<bool> Purchase(string productId);
        Awaitable RestorePurchases();
        bool IsOwned(string productId);
    }
}
