using LogosSDK.Core.Logging;
using LogosSDK.Save;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosMeta.Progression
{
    /// <summary>
    /// Resolves the current level's Addressables key by reading
    /// <see cref="LevelProgressData.CurrentLevel"/> from <see cref="ISaveManager"/>
    /// and looking it up in the <see cref="ILevelCatalog"/>.
    ///
    /// Clamps the index so play never hard-crashes when CurrentLevel
    /// exceeds the catalog length (e.g. during rapid iteration / QA).
    /// </summary>
    public class LevelService : ILevelService
    {
        private static readonly ILogger Logger = LogManager.GetLogger<LevelService>();

        private readonly ILevelCatalog _catalog;
        private readonly ISaveManager _saveManager;

        public LevelService(ILevelCatalog catalog, ISaveManager saveManager)
        {
            _catalog = catalog;
            _saveManager = saveManager;
        }

        /// <inheritdoc/>
        public string GetCurrentLevelAddressKey()
        {
            if (!TryGetCurrentEntry(out var entry))
                return null;

            if (Logger.IsDebugEnabled)
                Logger.Debug($"[LevelService] Level={GetCurrentIndex()} → AddressKey='{entry.AddressKey}'");

            return entry.AddressKey;
        }

        /// <inheritdoc/>
        public string GetCurrentLevelId()
        {
            if (!TryGetCurrentEntry(out var entry))
                return null;

            return entry.LevelId;
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private bool TryGetCurrentEntry(out LevelEntry entry)
        {
            entry = default;

            if (_catalog == null || _catalog.Count == 0)
            {
                Logger.Warn("[LevelService] Level catalog is null or has no entries.");
                return false;
            }

            int index = GetCurrentIndex();
            int last = _catalog.Count - 1;
            int clamped = index < _catalog.Count ? index : last;

            if (Logger.IsDebugEnabled && clamped != index)
                Logger.Debug($"[LevelService] CurrentLevel={index} clamped → {clamped} (catalog has {_catalog.Count} entries)");

            return _catalog.TryGetEntry(clamped, out entry);
        }

        private int GetCurrentIndex()
        {
            var progress = _saveManager.Load<LevelProgressData>();
            // Clamp to 0 in case data is corrupted (CurrentLevel should never be negative).
            return progress.CurrentLevel < 0 ? 0 : progress.CurrentLevel;
        }
    }
}
