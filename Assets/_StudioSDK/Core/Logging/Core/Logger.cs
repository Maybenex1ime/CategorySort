using System;

namespace LogosSDK.Core.Logging
{
    /// <summary>
    /// Thread-safe logger implementation.
    /// Routes log calls to LoggerRegistry which manages targets and configuration.
    /// Performs level checks before writing to avoid allocations.
    /// </summary>
    internal sealed class Logger : ILogger
    {
        private readonly string _category;
        private readonly LoggerRegistry _registry;

        public string Name => _category;

        public Logger(string category, LoggerRegistry registry)
        {
            _category = category;
            _registry = registry;
        }

        public bool IsTraceEnabled => _registry.IsLevelEnabled(_category, LogLevel.Trace);
        public bool IsDebugEnabled => _registry.IsLevelEnabled(_category, LogLevel.Debug);
        public bool IsInfoEnabled => _registry.IsLevelEnabled(_category, LogLevel.Info);
        public bool IsWarnEnabled => _registry.IsLevelEnabled(_category, LogLevel.Warn);
        public bool IsErrorEnabled => _registry.IsLevelEnabled(_category, LogLevel.Error);
        public bool IsFatalEnabled => _registry.IsLevelEnabled(_category, LogLevel.Fatal);

        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warn(string message) => Log(LogLevel.Warn, message);
        public void Error(string message) => Log(LogLevel.Error, message);
        public void Fatal(string message) => Log(LogLevel.Fatal, message);

        public void Error(Exception exception, string message) =>
            LogWithException(LogLevel.Error, message, exception);

        public void Fatal(Exception exception, string message) =>
            LogWithException(LogLevel.Fatal, message, exception);

        private void Log(LogLevel level, string message)
        {
            if (!_registry.IsLevelEnabled(_category, level))
            {
                return;
            }

            _registry.WriteLog(level, _category, message);
        }

        private void LogWithException(LogLevel level, string message, Exception exception)
        {
            if (!_registry.IsLevelEnabled(_category, level))
            {
                return;
            }

            _registry.WriteLog(level, _category, message, exception);
        }
    }
}
