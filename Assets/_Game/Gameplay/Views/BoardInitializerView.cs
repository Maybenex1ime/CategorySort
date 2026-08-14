using LogosGame.Features.Gameplay.Flow;
using LogosMeta.Progression;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using R3;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using WordStack.Contracts;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Gameplay.Views
{
    /// <summary>
    /// Port ý tưởng BoardInitializerView của aquapark: nghe AddressKey từ ViewModel,
    /// nạp level qua Addressables, đưa cho gameplay.
    ///
    /// Khác bản gốc: level WordStack là TextAsset JSON (không phải BoardDefinition),
    /// và board nằm ở Assembly-CSharp không inject được — nên bước cuối đi qua
    /// LevelCommands (C# thuần) thay vì gọi IGameplayLifecycle.
    ///
    /// Đặt trong Main.unity, cùng scene với SceneScope.
    /// </summary>
    public sealed class BoardInitializerView : MonoBehaviour
    {
        private static readonly ILogger _logger = LogManager.GetLogger<BoardInitializerView>();

        [Inject] private readonly IGameplayFlowController _flow;
        [Inject] private readonly ISaveManager _saveManager;

        private DisposableBag _disposables;
        private AsyncOperationHandle<TextAsset> _handle;
        private bool _hasHandle;

        private void Start()
        {
            _flow.AddressKey
                .Where(key => !string.IsNullOrEmpty(key))   // ResetLevelAsync xoá key trước khi set — bỏ qua nhịp rỗng
                .Subscribe(LoadLevelInBackground)
                .AddTo(ref _disposables);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
            ReleaseHandle();
        }

        private async void LoadLevelInBackground(string addressKey)
        {
            try
            {
                // Mỗi LoadAssetAsync phải có Release tương ứng — giữ đúng một handle sống.
                ReleaseHandle();

                _handle = Addressables.LoadAssetAsync<TextAsset>(addressKey);
                _hasHandle = true;
                TextAsset asset = await _handle.Task;

                if (asset == null)
                {
                    _logger.Error($"[BoardInitializer] Addressables không tìm thấy '{addressKey}' — kiểm tra address trong catalog.");
                    return;
                }

                // Lúc màn BẮT ĐẦU, CurrentLevel luôn là chỉ số màn đang chơi
                // (ProgressionService chỉ tăng nó khi KẾT THÚC thắng).
                int levelIndex = 0;
                if (_saveManager != null)
                {
                    LevelProgressData progress = _saveManager.Load<LevelProgressData>();
                    if (progress != null && progress.CurrentLevel > 0)
                        levelIndex = progress.CurrentLevel;
                }

                _logger.Info($"[BoardInitializer] '{addressKey}' → màn {levelIndex} ({asset.text.Length} ký tự JSON)");
                LevelCommands.RequestLoad(levelIndex, asset.text);
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, $"[BoardInitializer] Nạp '{addressKey}' thất bại.");
            }
        }

        private void ReleaseHandle()
        {
            if (!_hasHandle) return;
            _hasHandle = false;
            if (_handle.IsValid())
                Addressables.Release(_handle);
        }
    }
}
