namespace LogosMeta.Progression
{
    /// <summary>
    /// Provides level addressing information by bridging the save-system's
    /// integer CurrentLevel index with the catalog's string-keyed entries.
    /// </summary>
    public interface ILevelService
    {
        /// <summary>
        /// Returns the Addressables address key for the current level.
        /// Returns <c>null</c> if the catalog contains no matching entry.
        /// </summary>
        string GetCurrentLevelAddressKey();

        /// <summary>
        /// Returns the designer-facing LevelId for the current level (e.g. for
        /// debug HUDs or analytics). Returns <c>null</c> if no matching entry.
        /// </summary>
        string GetCurrentLevelId();
    }
}
