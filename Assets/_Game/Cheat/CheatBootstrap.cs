using LogosGame.Features.Gameplay.Content;
using LogosSDK.Core.Logging;
using Reflex.Attributes;
using Reflex.Core;
using Reflex.Injectors;
using UnityEngine;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Cheat
{
    /// <summary>
    /// Spawns the persistent CheatRoot canvas. DAT TRONG SCENE (Main.unity) —
    /// KHONG dat len ProjectScope prefab: RootScope khong duoc Instantiate nen
    /// Start() cua MonoBehaviour tren do khong bao gio chay.
    /// </summary>
    public sealed class CheatBootstrap : MonoBehaviour
    {
        private static readonly ILogger _logger = LogManager.GetLogger<CheatBootstrap>();

        [SerializeField] private CheatSettings _cheatSettings;
        [SerializeField] private GameObject _cheatRootPrefab;

        [Inject] private readonly Container _container;

        private GameObject _spawnedRoot;

        private void Start()
        {
            if (_cheatSettings == null || !_cheatSettings.EnableCheats)
                return;

            if (_cheatRootPrefab == null)
            {
                _logger.Warn("[CheatBootstrap] EnableCheats is on but no CheatRoot prefab is assigned.");
                return;
            }

            if (_spawnedRoot != null)
                return;

            _spawnedRoot = Instantiate(_cheatRootPrefab);
            _spawnedRoot.name = _cheatRootPrefab.name;
            DontDestroyOnLoad(_spawnedRoot);

            if (_container != null)
                GameObjectInjector.InjectRecursive(_spawnedRoot, _container);
        }
    }
}
