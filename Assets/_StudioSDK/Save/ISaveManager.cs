namespace LogosSDK.Save
{
    public interface ISaveManager
    {
        void Register<T>(IStorageProvider provider, string key) where T : class, new();
        T Load<T>() where T : class, new();
        void Save<T>(T data) where T : class, new();
        void SaveImmediate<T>(T data) where T : class, new();
        void SaveAll();
        void DeleteDomain<T>() where T : class, new();
        bool HasDomain<T>() where T : class, new();
    }
}
