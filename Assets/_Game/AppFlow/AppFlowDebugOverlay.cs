using LogosGame.Features.Gameplay.Services;
using LogosMeta.Economy;
using LogosMeta.Progression;
using LogosSDK.Core.Events;
using LogosSDK.Save;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;
using WordStack.Contracts;

namespace WordStack.Meta.AppFlow
{
    /// <summary>
    /// Overlay dev: hiện phase hiện tại + vài số liệu meta ngay trên màn hình.
    /// Dùng OnGUI cố ý — không đụng Canvas, không lọt vào screen stack của UIManager,
    /// và không cần prefab. Gắn lên một GameObject bất kỳ trong scene.
    /// </summary>
    public sealed class AppFlowDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool _visible = true;
        [SerializeField] private Key _toggleKey = Key.F1;

        [Inject] private readonly ISaveManager _saveManager;
        [Inject] private readonly ICurrencyService _currencyService;
        [Inject] private readonly IHeartService _heartService;
        [Inject] private readonly IDifficultyStateProvider _difficultyProvider;

        private AppFlowPhase _phase = AppFlowPhase.None;
        private int _levelIndex = -1;
        private GUIStyle _style;

        private void Awake()
        {
            Bus.Global.On<AppFlowPhaseChangedEvent>(OnPhaseChanged);
            LevelSignals.Started += OnLevelStarted;
        }

        private void OnDestroy()
        {
            Bus.Global.Off<AppFlowPhaseChangedEvent>(OnPhaseChanged);
            LevelSignals.Started -= OnLevelStarted;
        }

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null && k[_toggleKey].wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnPhaseChanged(AppFlowPhaseChangedEvent evt) => _phase = evt.Phase;

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            _levelIndex = evt.LevelIndex;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white },
            };

            string saved = _saveManager != null
                ? _saveManager.Load<LevelProgressData>()?.CurrentLevel.ToString() ?? "?"
                : "n/a";
            LevelDifficulty difficulty = _difficultyProvider != null
                ? _difficultyProvider.Difficulty.CurrentValue
                : LevelDifficulty.Normal;
            string board = _levelIndex >= 0 ? $"{_levelIndex} ({difficulty})" : "-";
            string coins = _currencyService != null ? _currencyService.Coins.CurrentValue.ToString() : "n/a";
            string hearts = _heartService != null ? _heartService.Current.CurrentValue.ToString() : "n/a";

            GUI.Box(new Rect(10, 10, 250, 112), GUIContent.none);
            GUILayout.BeginArea(new Rect(20, 16, 240, 100));
            GUILayout.Label($"AppFlow : {_phase}", _style);
            GUILayout.Label($"Bàn     : {board}", _style);
            GUILayout.Label($"Đã lưu  : {saved}", _style);
            GUILayout.Label($"Coin {coins}   Tim {hearts}", _style);
            GUILayout.EndArea();
        }
    }
}
