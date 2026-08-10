namespace LogosSDK.Save
{
    public interface ISaveMigrator<T> where T : class
    {
        int FromVersion { get; }
        int ToVersion { get; }
        T Migrate(T data);
    }
}
