using R3;

namespace LogosGame.Features.Currency.Services
{
    /// <summary>
    /// Đọc số lượng mọi tài nguyên theo ResourceType — UI chỉ cần biết enum, không cần biết
    /// coin nằm ở ví, tim ở HeartService, booster ở BoosterModule.
    /// Chỉ ĐỌC; cộng/trừ vẫn đi qua service gốc của từng loại.
    /// </summary>
    public interface IResourceService
    {
        ReadOnlyReactiveProperty<int> Observe(ResourceType type);
    }
}
