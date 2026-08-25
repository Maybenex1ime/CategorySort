using LogosGame.Features.Shop;
using LogosGame.Features.Shop.Impl;
using LogosSDK.Services;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta
{
    /// <summary>
    /// Gắn lên ProjectScope.prefab, cạnh CurrencyInstaller. Tách riêng vì Shop là
    /// feature độc lập (cùng lối HeartInstaller tách khỏi CurrencyInstaller dù
    /// Heart cũng nằm trong _Modules/Economy), và vì khi thay StubIAPService bằng
    /// Unity IAP thật thì tầng init store sống hết ở đây.
    /// </summary>
    public sealed class ShopInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private ShopCatalog _shopCatalog;

        public void InstallBindings(ContainerBuilder builder)
        {
            // Catalog có thể vắng (chưa tạo asset) nhưng IShopService thì LUÔN phải
            // đăng ký: ShopPopup lấy nó qua [Inject], mà Reflex ném FieldInjectorException
            // khi contract chưa bind — vắng là popup không mở nổi chứ không phải
            // "mở với list rỗng". Thiếu catalog thì service tự trả list rỗng.
            if (_shopCatalog != null)
            {
                builder.RegisterValue(_shopCatalog, new[] { typeof(IShopCatalog) });
            }

            // ĐỔI Ở ĐÂY khi lên store thật: StubIAPService → impl Unity IAP.
            builder.RegisterType(typeof(StubIAPService),
                new[] { typeof(IIAPService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            // Factory chứ không RegisterType: catalog và IPurchaseService đều tuỳ chọn
            // (IPurchaseService vắng khi CurrencyInstaller chưa có SO_TransactionCatalog)
            // nên phải resolve mềm, không thì cả ShopService sập theo.
            builder.RegisterFactory<IShopService>(
                c => new ShopService(
                    c.TryGetResolver<IShopCatalog>(out _) ? c.Resolve<IShopCatalog>() : null,
                    c.Resolve<IIAPService>(),
                    c.TryGetResolver<LogosMeta.Economy.ICurrencyService>(out _)
                        ? c.Resolve<LogosMeta.Economy.ICurrencyService>()
                        : null,
                    c.TryGetResolver<LogosMeta.Economy.IPurchaseService>(out _)
                        ? c.Resolve<LogosMeta.Economy.IPurchaseService>()
                        : null),
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
