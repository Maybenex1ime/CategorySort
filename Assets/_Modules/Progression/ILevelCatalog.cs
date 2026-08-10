namespace LogosMeta.Progression
{
    // Implemented by the game's level catalog asset (ScriptableObject).
    public interface ILevelCatalog
    {
        int Count { get; }
        bool TryGetEntry(int index, out LevelEntry entry);
    }
}
