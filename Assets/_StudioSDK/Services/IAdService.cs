using System;

namespace LogosSDK.Services
{
    public interface IAdService
    {
        bool IsRewardedReady { get; }
        void ShowRewarded(Action<bool> onComplete);
        void ShowInterstitial();
    }
}
