using System.Collections.Generic;

namespace LogosSDK.Services
{
    public interface IAnalyticsService
    {
        void LogEvent(string eventName, Dictionary<string, object> parameters = null);
    }
}
