using UnityEngine;

namespace LogosSDK.Core.Logging.Examples
{
    public class InstanceLoggerExample : MonoBehaviour
    {
        private readonly ILogger _instanceLogger = LogManager.GetLogger<InstanceLoggerExample>();

        void Start()
        {
            _instanceLogger.Info("✅ Instance logger works!");
        }
    }
}
