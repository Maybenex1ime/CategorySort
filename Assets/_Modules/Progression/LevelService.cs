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
    /// Past the last level the catalog maps the index back into its loop
    /// (<see cref="ILevelCatalog.ToCatalogIndex"/>), so play never runs out of levels.
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
            int looped = _catalog.ToCatalogIndex(index);

            if (Logger.IsDebugEnabled && looped != index)
                Logger.Debug($"[LevelService] CurrentLevel={index} looped → {looped} (catalog has {_catalog.Count} entries)");

            return _catalog.TryGetEntry(looped, out entry);
        }

        private int GetCurrentIndex()
        {
            var progress = _saveManager.Load<LevelProgressData>();
            // Clamp to 0 in case data is corrupted (CurrentLevel should never be negative).
            return progress.CurrentLevel < 0 ? 0 : progress.CurrentLevel;
        }
    }
}
