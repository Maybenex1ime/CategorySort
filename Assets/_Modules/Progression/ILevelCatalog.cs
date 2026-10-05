namespace LogosMeta.Progression
{
    // Implemented by the game's level catalog asset (ScriptableObject).
    public interface ILevelCatalog
    {
        int Count { get; }
        bool TryGetEntry(int index, out LevelEntry entry);

        // Level index (0-based, keeps growing in the save) → catalog index. Past the last level the
        // catalog decides how to loop.
        int ToCatalogIndex(int levelIndex);
    }
}
