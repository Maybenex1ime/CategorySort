using LogosMeta.Economy;
using LogosSDK.Save;
using LogosSDK.Save.Providers;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta
{
    /// <summary>
    /// Đăng ký các domain dữ liệu với SaveManager. Phải chạy TRƯỚC khi bất kỳ
    /// service nào được dựng — service đọc `_save.Load&lt;T&gt;()` ngay trong
    /// constructor, domain chưa đăng ký thì nó nhận giá trị mặc định và mọi
    /// thay đổi về sau rơi vào hư không (SaveManager log lỗi "unregistered domain").
    /// Vì vậy mọi service ở đây bind Resolution.Lazy, không bao giờ Eager.
    /// </summary>
    public sealed class GameSaveInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.OnContainerBuilt += container =>
            {
                var save = container.Resolve<ISaveManager>();
                var json = container.Resolve<JsonFileStorage>();

                save.Register<CurrencyData>(json, "currency");
                save.Register<HeartData>(json, "hearts");
            };
        }
    }
}
