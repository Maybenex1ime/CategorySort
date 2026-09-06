using System;
using LogosMeta.Progression;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using WordStack.Contracts;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Gameplay.Flow
{
    /// <summary>
    /// Nghe AddressKey từ ViewModel, nạp level qua Addressables, đưa cho gameplay
    /// qua LevelCommands (C# thuần — board ở WordStack.Board, Meta không ref nó).
    ///
    /// Plain C# đăng ký Eager trong AppFlowInstaller, KHÔNG phải MonoBehaviour:
    /// bản Mono cũ (BoardInitializerView) chưa từng được đặt vào scene nào nên
    /// AddressKey publish vào hư không và bàn không bao giờ nạp — cùng lớp bug
    /// với BoosterManager trước đây.
    /// </summary>
    public sealed class BoardInitializer : IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<BoardInitializer>();

        private readonly ISaveManager _saveManager;
        private readonly ILevelCatalog _catalog;

        private DisposableBag _disposables;
        private AsyncOperationHandle<TextAsset> _handle;
        private bool _hasHandle;

        // ILevelCatalog chỉ để lấy AddressKey của màn 1 làm phao. ProgressionInstaller
        // không đăng ký nó khi quên gán SO_LevelCatalog — lúc đó AddressKey cũng rỗng
        // nên bàn vốn không nạp được, resolve fail ở đây không làm mất gì thêm.
        public BoardInitializer(IGameplayFlowController flow, ISaveManager saveManager, ILevelCatalog catalog)
        {
            _saveManager = saveManager;
            _catalog = catalog;
            flow.AddressKey
                .Where(key => !string.IsNullOrEmpty(key))   // ResetLevelAsync xoá key trước khi set — bỏ qua nhịp rỗng
                .Subscribe(LoadLevelInBackground)
                .AddTo(ref _disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            ReleaseHandle();
        }

        private async void LoadLevelInBackground(string addressKey)
        {
            try
            {
                TextAsset asset = await LoadAssetAsync(addressKey);

                // Không có level → cảnh báo rồi rơi về màn 1 thay vì để bàn trống.
                // Chỉ rơi một lần: màn 1 cũng hỏng thì báo lỗi, không lặp vô hạn.
                if (asset == null)
                {
                    string fallbackKey = FallbackAddressKey();
                    _logger.Warn($"Ko tìm thấy id-level '{addressKey}' — nạp level 1 ('{fallbackKey}').");
                    if (!string.IsNullOrEmpty(fallbackKey) && fallbackKey != addressKey)
                        asset = await LoadAssetAsync(fallbackKey);
                }
                if (asset == null)
                {
                    _logger.Error($"[BoardInitializer] Không nạp được '{addressKey}' lẫn level 1 — kiểm tra address trong catalog.");
                    return;
                }

                // Lúc màn BẮT ĐẦU, CurrentLevel luôn là chỉ số màn đang chơi
                // (ProgressionService chỉ tăng nó khi KẾT THÚC thắng). Rơi về màn 1
                // vẫn giữ chỉ số này: thắng thì tiến độ vẫn nhích, chỉ nội dung là mượn.
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
            catch (Exception ex)
            {
                _logger.Error(ex, $"[BoardInitializer] Nạp '{addressKey}' thất bại.");
            }
        }

        // Trả null thay vì ném: key sai thì Addressables đặt handle Failed và Task trả
        // null, nhưng vài bản ném InvalidKeyException — gom cả hai về một đường.
        private async System.Threading.Tasks.Task<TextAsset> LoadAssetAsync(string addressKey)
        {
            // Mỗi LoadAssetAsync phải có Release tương ứng — giữ đúng một handle sống.
            ReleaseHandle();
            try
            {
                _handle = Addressables.LoadAssetAsync<TextAsset>(addressKey);
                _hasHandle = true;
                return await _handle.Task;
            }
            catch (Exception ex)
            {
                _logger.Warn($"[BoardInitializer] Addressables từ chối '{addressKey}': {ex.Message}");
                return null;
            }
        }

        private string FallbackAddressKey()
        {
            if (_catalog == null || !_catalog.TryGetEntry(0, out LevelEntry first)) return null;
            return first.AddressKey;
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
