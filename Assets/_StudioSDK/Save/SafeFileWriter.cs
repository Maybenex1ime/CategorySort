using System;
using System.IO;
using LogosSDK.Core.Logging;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.Save
{
    public class SafeFileWriter
    {
        private static readonly ILogger Logger = LogManager.GetLogger<SafeFileWriter>();

        public void Write(string path, string content)
        {
            var tmpPath = path + ".tmp";
            var bakPath = path + ".bak";

            try
            {
                File.WriteAllText(tmpPath, content);
                
                // Validate before renaming
                var readBack = File.ReadAllText(tmpPath);
                if (readBack != content)
                {
                    Logger.Error($"File validation failed for {tmpPath}");
                    return;
                }

                if (File.Exists(path))
                {
                    if (File.Exists(bakPath))
                    {
                        File.Delete(bakPath);
                    }
                    File.Move(path, bakPath);
                }

                File.Move(tmpPath, path);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to safe write file: {path}. Error: {ex.Message}");
            }
        }

        public string Read(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to read file: {path}. Trying backup. Error: {ex.Message}");
                }
            }

            var bakPath = path + ".bak";
            if (File.Exists(bakPath))
            {
                try
                {
                    var content = File.ReadAllText(bakPath);
                    Logger.Warn($"Recovered from backup: {path}");
                    return content;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to read backup file: {bakPath}. Error: {ex.Message}");
                }
            }

            return null;
        }

        public void Delete(string path)
        {
            var tmpPath = path + ".tmp";
            var bakPath = path + ".bak";

            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            if (File.Exists(bakPath)) File.Delete(bakPath);
        }
    }
}
