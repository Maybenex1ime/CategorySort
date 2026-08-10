using System;
using System.Collections.Generic;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosMeta.Progression
{
    public class ProgressionService : IProgressionService
    {
        private static readonly ILogger Logger = LogManager.GetLogger<ProgressionService>();
        private readonly ISaveManager _saveManager;

        public ProgressionService(ISaveManager saveManager)
        {
            _saveManager = saveManager;
        }

        // Must NEVER throw — callers fire follow-up events immediately after
        // this; an exception here would block the game flow from advancing.
        public void ReportResult(bool isWin)
        {
            try
            {
                Logger.Debug($"ReportResult called. IsWin: {isWin}");

                if (!isWin)
                    return;

                var progress = _saveManager.Load<LevelProgressData>();
                if (progress == null)
                {
                    Logger.Warn("[ProgressionService] LevelProgressData is null — skipping save.");
                    return;
                }

                // Defensive: an old save file may deserialize with UnlockedLevels = null
                // (Newtonsoft overwrites the field initializer when JSON has explicit null).
                if (progress.UnlockedLevels == null)
                    progress.UnlockedLevels = new Dictionary<int, bool>();

                progress.UnlockedLevels[progress.CurrentLevel] = true;
                progress.CurrentLevel++;
                progress.UnlockedLevels[progress.CurrentLevel] = true;

                _saveManager.Save(progress);
                _saveManager.SaveAll();
                Logger.Debug($"Level Progressed! New Level: {progress.CurrentLevel}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[ProgressionService] ReportResult failed — progress may not be saved this session.");
            }
        }
    }
}
