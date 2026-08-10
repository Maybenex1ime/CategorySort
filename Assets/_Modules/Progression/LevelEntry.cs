namespace LogosMeta.Progression
{
    // Minimal, game-agnostic slice of a catalog entry. Game catalogs keep
    // whatever extra per-level fields they need (tutorial config, UI flags...)
    // on their own asset type and expose only this via ILevelCatalog.
    public readonly struct LevelEntry
    {
        public string LevelId { get; }
        public string AddressKey { get; }
        public string DisplayName { get; }

        public LevelEntry(string levelId, string addressKey, string displayName)
        {
            LevelId = levelId;
            AddressKey = addressKey;
            DisplayName = displayName;
        }
    }
}
