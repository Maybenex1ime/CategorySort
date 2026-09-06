using LogosGame.Features.Gameplay.Boosters.Services;
using LogosGame.Features.Gameplay.Boosters.Services.Impl;
using LogosGame.Features.Gameplay.Boosters.ViewModels;
using LogosGame.Features.Gameplay.Flow;
using LogosGame.Features.Gameplay.Services;
using LogosSDK.Core.AppFlow;
using LogosSDK.Save;
using LogosSDK.UI.Core;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta.AppFlow.Installers
{
    /// <summary>
    /// Gắn lên SceneScope, CẠNH UIInstaller — không đặt lên ProjectScope.prefab
    /// vì manager cần UIManager, mà UIManager là object trong scene.
    /// </summary>
    public sealed class AppFlowInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField, Min(0f)] private float _minLoadingSeconds = 2f;

        public void InstallBindings(ContainerBuilder builder)
        {
            // Một instance, hai contract (ISP): HUD lấy IGameplayFlowController,
            // GameplayHudView lấy IDifficultyStateProvider. Gộp vì độ khó đến cùng
            // lúc với level qua GameplayStartContext.
            builder.RegisterType(typeof(WordStackGameplayViewModel),
                new[] { typeof(IGameplayFlowController), typeof(IDifficultyStateProvider) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            // Có audio thì phát tiếng + rung thật; chưa gắn AudioServicesInstaller
            // thì rơi về bản rỗng — View không cần biết khác biệt.
            builder.RegisterFactory<IFeedbackDispatcher>(
                c => c.TryGetResolver<LogosSDK.Audio.IAudioService>(out _)
                    ? new WordStackFeedbackDispatcher(
                        c.Resolve<LogosSDK.Audio.IAudioService>(),
                        c.TryGetResolver<LogosSDK.Services.IHapticService>(out _)
                            ? c.Resolve<LogosSDK.Services.IHapticService>()
                            : null)
                    : (IFeedbackDispatcher)new NullFeedbackDispatcher(),
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            // Inventory booster: plain C# thay cho MonoBehaviour singleton cũ —
            // bản cũ không ai đặt vào scene nên BoosterAddedEvent sau khi mua rơi
            // vào hư không (coin mất, count đứng im). Eager BẮT BUỘC: không ai
            // inject nó, toàn bộ việc nằm ở constructor (load save + nghe bus).
            builder.RegisterType(typeof(BoosterModule.BoosterManager),
                new[] { typeof(BoosterModule.BoosterManager), typeof(System.IDisposable) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Eager);

            // Booster: mỗi ViewModel bọc một slot của BoosterModule, view inject theo kiểu cụ thể.
            // Lazy được — các *BoosterButtonView inject nên chúng tự bị dựng khi
            // prefab HUD xuất hiện.
            foreach (System.Type vm in new[]
                     {
                         typeof(MagnetBoosterViewModel),
                         typeof(ShuffleBoosterViewModel),
                         typeof(UndoBoosterViewModel),
                     })
            {
                builder.RegisterType(vm, new[] { vm, typeof(System.IDisposable) },
                    Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Lazy);
            }

            builder.RegisterType(typeof(BoosterUiAnchors),
                new[] { typeof(IBoosterUiAnchors) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            // Nghe PurchaseRequestedEvent (nút booster bắn khi count = 0) → mở popup
            // mua. Eager như GameplayFlowAdapter: không ai inject nó, toàn bộ việc
            // nằm ở constructor (đăng ký bus) — Lazy là luồng mua im lặng biến mất.
            builder.RegisterFactory<LogosGame.Features.Currency.UI.Impl.BoosterPurchaseFlow>(
                c => new LogosGame.Features.Currency.UI.Impl.BoosterPurchaseFlow(
                    c.TryGetResolver<UIManager>(out _) ? c.Resolve<UIManager>() : null,
                    // Vắng khi CurrencyInstaller chưa được gán SO_TransactionCatalog
                    // — flow rơi về stub log, popup vẫn mở với giá "—".
                    c.TryGetResolver<LogosMeta.Economy.IPurchaseService>(out _)
                        ? c.Resolve<LogosMeta.Economy.IPurchaseService>()
                        : null,
                    c.TryGetResolver<LogosMeta.Economy.ICurrencyService>(out _)
                        ? c.Resolve<LogosMeta.Economy.ICurrencyService>()
                        : null,
                    c.TryGetResolver<LogosGame.Features.Gameplay.Content.IUnlockSchedule>(out _)
                        ? c.Resolve<LogosGame.Features.Gameplay.Content.IUnlockSchedule>()
                        : null),
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Eager);

            // Eager BẮT BUỘC: không ai inject adapter này, toàn bộ việc của nó nằm
            // trong constructor (đăng ký nghe LevelSignals). Để Lazy là nó không bao
            // giờ được dựng và gameplay không báo gì cho ViewModel — im lặng, không lỗi.
            builder.RegisterType(typeof(GameplayFlowAdapter),
                new[] { typeof(GameplayFlowAdapter), typeof(System.IDisposable) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Eager);

            // Nạp level: nghe AddressKey → Addressables → LevelCommands.RequestLoad.
            // Eager BẮT BUỘC cùng lý do trên; bản MonoBehaviour cũ (BoardInitializerView)
            // chưa từng nằm trong scene nào nên bàn không bao giờ nạp.
            builder.RegisterType(typeof(BoardInitializer),
                new[] { typeof(BoardInitializer), typeof(System.IDisposable) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Eager);

            // Lazy: UIManager do UIInstaller đăng ký, thứ tự component trên
            // SceneScope không đảm bảo. AppBootstrap resolve ở Start nên đủ trễ.
            builder.RegisterFactory<IAppFlowManager>(
                c =>
                {
                    // Catalog/LevelService chỉ tồn tại khi ProgressionInstaller được
                    // gán asset — resolve tùy chọn để thiếu asset không sập cả app
                    // (AppFlow vẫn lên menu, lỗi hiện rõ lúc nạp màn).
                    LogosGame.Features.Gameplay.Content.LevelCatalog catalog =
                        c.TryGetResolver<LogosGame.Features.Gameplay.Content.LevelCatalog>(out _)
                            ? c.Resolve<LogosGame.Features.Gameplay.Content.LevelCatalog>()
                            : null;
                    LogosMeta.Progression.ILevelService levelService =
                        c.TryGetResolver<LogosMeta.Progression.ILevelService>(out _)
                            ? c.Resolve<LogosMeta.Progression.ILevelService>()
                            : null;

                    // Audio/haptic chỉ tồn tại khi AudioServicesInstaller được gắn
                    // lên ProjectScope — thiếu thì settings popup từ chối mở (có warn),
                    // app không sập.
                    LogosSDK.Audio.IAudioService audio =
                        c.TryGetResolver<LogosSDK.Audio.IAudioService>(out _)
                            ? c.Resolve<LogosSDK.Audio.IAudioService>()
                            : null;
                    LogosSDK.Services.IHapticService haptic =
                        c.TryGetResolver<LogosSDK.Services.IHapticService>(out _)
                            ? c.Resolve<LogosSDK.Services.IHapticService>()
                            : null;

                    return new WordStackAppFlowManager(
                        c.Resolve<UIManager>(),
                        _minLoadingSeconds,
                        c.Resolve<ISaveManager>(),
                        c.Resolve<ICoinRewardService>(),
                        c.Resolve<IGameplayFlowController>(),
                        levelService,
                        catalog,
                        audio,
                        haptic,
                        c.Resolve<LogosMeta.Economy.IHeartService>());
                },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
