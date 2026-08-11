using LogosMeta.Progression;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta
{
    /// <summary>
    /// Chỉ bind <see cref="ProgressionService"/> — nó tự đủ, chỉ cần ISaveManager,
    /// mỗi lần thắng thì tăng CurrentLevel (0-based, khớp thẳng levelIndex của
    /// BoardController). KHÔNG bind LevelService: cái đó cần ILevelCatalog trả về
    /// address key cho Addressables, còn WordStack nạp level từ Resources — hai
    /// cách hiểu "màn hiện tại" khác nhau, phải chốt thiết kế trước.
    /// </summary>
    public sealed class ProgressionInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterType(typeof(ProgressionService),
                new[] { typeof(IProgressionService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
