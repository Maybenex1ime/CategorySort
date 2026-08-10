namespace LogosSDK.Core.Logging
{
    public static class LogManager
    {
        public static ILogger GetLogger<T>() => GetLogger(typeof(T).FullName);

        public static ILogger GetLogger(string category)
        {
            return LoggerRegistry.Instance.GetLogger(category);
        }

        public static ILogger GetLogger(object source)
        {
            return source != null ? GetLogger(source.GetType().FullName) : GetLogger("Null");
        }
    }
}
