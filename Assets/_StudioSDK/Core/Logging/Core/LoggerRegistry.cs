using System;
using System.Collections.Concurrent;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LogosSDK.Core.Logging
{
    /// <summary>
    /// Thread-safe singleton that manages logger instances and configuration.
    /// Lazily initializes on first use - works in edit mode, play mode, and builds.
    ///
    /// Responsibilities:
    /// - Load and cache configuration (from Resources or defaults)
    /// - Maintain logger instance cache (ConcurrentDictionary)
    /// - Manage log output targets (console, file, etc.)
    /// - Provide thread-safe access to all logging infrastructure
    /// </summary>
    internal sealed class LoggerRegistry
    {
        private static readonly Lazy<LoggerRegistry> SInstance = new(() => new LoggerRegistry(), isThreadSafe: true);

        public static LoggerRegistry Instance => SInstance.Value;

        private readonly ConcurrentDictionary<string, ILogger> _loggers = new();
        private readonly object _configLock = new();

        private bool _isInitialized;
        private LogConfig _config;
        private ILogTarget[] _targets;

        private LoggerRegistry()
        {
#if UNITY_EDITOR
            // Clear caches on domain reload to prevent stale references
            AssemblyReloadEvents.beforeAssemblyReload += HandleDomainReload;
#endif
        }

#if UNITY_EDITOR
        ~LoggerRegistry()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= HandleDomainReload;
        }
#endif

        private void EnsureInitialized()
        {
            if (_isInitialized) return;

            lock (_configLock)
            {
                if (_isInitialized) return;

                _config = LoadConfiguration();
                _targets = new ILogTarget[] { new UnityConsoleTarget() };
                _isInitialized = true;

                var mode = GetEnvironmentMode();
                Debug.Log($"[LoggerRegistry] Initialized. Mode: {mode}, Level: {_config.MinimumLogLevel}, Enabled: {_config.LoggingEnabled}");
            }
        }

        private LogConfig LoadConfiguration()
        {
            // Resources.Load is only safe to call from the main thread and not in constructors
            var configAsset = Resources.Load<LogConfigSO>("LogConfig");

            if (configAsset != null)
            {
                return LogConfig.FromScriptableObject(configAsset);
            }

            Debug.LogWarning("[LoggerRegistry] No LogConfig found at Resources/LogConfig.asset. Using environment defaults.");
            return CreateDefaultConfig();
        }

        private LogConfig CreateDefaultConfig()
        {
            var config = new LogConfig
            {
                LoggingEnabled = true,
                LogToConsole = true
            };

            // Environment-specific defaults
#if UNITY_EDITOR
            config.MinimumLogLevel = LogLevel.Debug;
#elif DEBUG
            config.MinimumLogLevel = LogLevel.Debug;
#else
            config.MinimumLogLevel = LogLevel.Warn;
#endif

            return config;
        }

        public ILogger GetLogger(string category)
        {
            return _loggers.GetOrAdd(category, key => new Logger(key, this));
        }

        public bool IsLevelEnabled(string category, LogLevel level)
        {
            EnsureInitialized();

            if (!_config.LoggingEnabled)
            {
                return false;
            }

            if (level == LogLevel.Off)
            {
                return false;
            }

            var threshold = _config.MinimumLogLevel;

            if (_config.CategoryOverrides == null)
            {
                return level >= threshold;
            }

            foreach (var overrideConfig in _config.CategoryOverrides)
            {
                if (string.IsNullOrEmpty(overrideConfig.CategoryName))
                {
                    continue;
                }

                // Match exact or hierarchical (e.g., "Combat" matches "Combat.Damage")
                if (category != overrideConfig.CategoryName &&
                    !category.StartsWith(overrideConfig.CategoryName + "."))
                {
                    continue;
                }

                threshold = overrideConfig.MinLevel;
                break;
            }

            return level >= threshold;
        }

        /// <summary>
        /// Writes a log message to all enabled targets.
        /// Called by Logger after level checks pass.
        /// </summary>
        public void WriteLog(LogLevel level, string category, string message)
        {
            EnsureInitialized();

            foreach (var target in _targets)
            {
                if (!target.IsEnabled)
                {
                    continue;
                }

                try
                {
                    target.Write(level, category, message);
                }
                catch (Exception ex)
                {
                    // Prevent logging errors from crashing the application
                    Debug.LogError($"[LoggerRegistry] Target {target.GetType().Name} failed: {ex}");
                }
            }
        }

        /// <summary>
        /// Writes a log message with exception to all enabled targets.
        /// </summary>
        public void WriteLog(LogLevel level, string category, string message, Exception exception)
        {
            EnsureInitialized();

            foreach (var target in _targets)
            {
                if (!target.IsEnabled)
                {
                    continue;
                }

                try
                {
                    target.Write(level, category, message, exception);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LoggerRegistry] Target {target.GetType().Name} failed: {ex}");
                }
            }
        }

        /// <summary>
        /// Updates the logging configuration at runtime.
        /// Useful for debug UI or console commands.
        /// </summary>
        public void UpdateConfiguration(LogConfig newConfig)
        {
            EnsureInitialized();
            lock (_configLock)
            {
                _config = newConfig;
                Debug.Log($"[LoggerRegistry] Configuration updated. Level: {_config.MinimumLogLevel}");
            }
        }

        /// <summary>
        /// Adds a custom log target (e.g., file logger, remote logger).
        /// </summary>
        public void AddTarget(ILogTarget target)
        {
            EnsureInitialized();
            lock (_configLock)
            {
                var newTargets = new ILogTarget[_targets.Length + 1];
                Array.Copy(_targets, newTargets, _targets.Length);
                newTargets[_targets.Length] = target;
                _targets = newTargets;
            }
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Resets the registry state for unit tests.
        /// DO NOT CALL IN PRODUCTION CODE.
        /// </summary>
        internal void Reset()
        {
            lock (_configLock)
            {
                _loggers.Clear();
                _config = CreateDefaultConfig();
                _targets = new ILogTarget[] { new UnityConsoleTarget() };
                _isInitialized = false;
                Debug.Log("[LoggerRegistry] Reset for testing");
            }
        }
#endif

#if UNITY_EDITOR
        private void HandleDomainReload()
        {
            _loggers.Clear();
            _isInitialized = false;
            Debug.Log("[LoggerRegistry] Logger cache cleared for domain reload");
        }
#endif

        private string GetEnvironmentMode()
        {
            if (Application.isEditor) return "Editor";
            return Debug.isDebugBuild ? "Development" : "Release";
        }
    }
}
