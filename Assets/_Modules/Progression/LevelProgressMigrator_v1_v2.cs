using LogosSDK.Save;

namespace LogosMeta.Progression
{
    public class LevelProgressMigrator_v1_v2 : ISaveMigrator<LevelProgressData>
    {
        public int FromVersion => 1;
        public int ToVersion => 2;

        public LevelProgressData Migrate(LevelProgressData data)
        {
            // Simple migration example
            data.SchemaVersion = 2;
            return data;
        }
    }
}
