using System.Collections.Generic;
using LogosSDK.Core.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosSDK.Core.Services.Impl
{
    public class AddressableAssetService : IAssetService
    {
        private static readonly ILogger Logger = LogManager.GetLogger<AddressableAssetService>();

        // Track loaded handles for leak detection
        private readonly Dictionary<Object, AsyncOperationHandle> _handles = new();

        public async Awaitable<T> LoadAsync<T>(string address) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            
            // Wait for it treating as standard Task using .Task wrapper
            var asset = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Logger.Error($"Failed to load asset: {address}");
                return null;
            }

            _handles[asset] = handle;

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug($"Loaded asset: {address} ({typeof(T).Name})");
            }

            return asset;
        }

        public void Release<T>(T asset) where T : Object
        {
            if (asset == null) return;

            if (_handles.TryGetValue(asset, out var handle))
            {
                Addressables.Release(handle);
                _handles.Remove(asset);
                
                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug($"Released asset: {asset.name} ({typeof(T).Name})");
                }
            }
            else
            {
                Logger.Warn($"Release called for untracked asset: {asset.name}");
            }
        }
    }
}
