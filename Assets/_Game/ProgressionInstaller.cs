using LogosGame.Features.Gameplay.Content;
using LogosMeta.Progression;
using Reflex.Core;
using UnityEngine;

namespace WordStack.Meta
{
    /// <summary>
    /// Progression + level addressing. Câu hỏi treo cũ ("WordStack nạp level từ
    /// Resources, LevelService để làm gì") đã chốt: level giờ đi qua Addressables
    /// giống aquapark — catalog giữ AddressKey + Difficulty, LevelService bắc cầu
    /// LevelProgressData.CurrentLevel (int) ↔ catalog (string key).
    ///
    /// Nằm trên ProjectScope prefab nên chỉ tham chiếu được ASSET — LevelCatalog
    /// là ScriptableObject nên hợp lệ (khác UIManager là object trong scene).
    /// </summary>
    public sealed class ProgressionInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private LevelCatalog _levelCatalog;
        [SerializeField] private SO_UnlockSchedule _unlockSchedule;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterType(typeof(ProgressionService),
                new[] { typeof(IProgressionService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            // Lịch mở khoá booster — BoosterPurchaseFlow lấy tên/mô tả/icon cho popup
            // mua từ đây. Chưa gán thì nó tự rơi về tên trong SO_TransactionCatalog và
            // popup không có icon, KHÔNG phải lỗi — nên bỏ qua im lặng. Đăng ký TRƯỚC
            // nhánh return của catalog để thiếu level catalog không kéo theo cái này.
            if (_unlockSchedule != null)
                builder.RegisterValue(_unlockSchedule, new[] { typeof(IUnlockSchedule) });

            if (_levelCatalog == null)
            {
                // Quên gán asset thì AppFlow vẫn chạy nhưng AddressKey rỗng —
                // BoardInitializer sẽ báo lỗi rõ thay vì đứng im.
                Debug.LogError("[ProgressionInstaller] Chưa gán SO_LevelCatalog — bàn chơi sẽ không nạp được.");
                return;
            }

            // Concrete cho AppFlowContext (đọc Difficulty), ILevelCatalog cho module.
            builder.RegisterValue(_levelCatalog, new[] { typeof(LevelCatalog), typeof(ILevelCatalog) });

            builder.RegisterType(typeof(LevelService),
                new[] { typeof(ILevelService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
