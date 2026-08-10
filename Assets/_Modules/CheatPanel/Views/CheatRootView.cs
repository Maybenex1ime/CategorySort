using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace LogosMeta.CheatPanel
{
    /// <summary>
    /// Top-level cheat HUD. A small CHEAT button is always visible (top-left).
    /// Tapping it expands the full menu panel; tapping again collapses it.
    /// Stays on top of every scene because the GameObject is DontDestroyOnLoad
    /// (spawned by the game's cheat bootstrap) and its Canvas overrides sorting.
    /// </summary>
    public sealed class CheatRootView : MonoBehaviour
    {
        [Inject] private readonly ICheatPanelConfig _config;

        [SerializeField] private Button _toggleButton;
        [SerializeField] private GameObject _menuPanel;
        [SerializeField] private bool _startExpanded;

        private bool _isExpanded;

        private void Awake()
        {
            // Self-hide if cheats are off (bootstrap should already gate, but
            // double-check in case the prefab is dropped into a scene manually).
            if (_config != null && !_config.EnableCheats)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        private void Start()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(OnToggleClicked);

            SetExpanded(_startExpanded);
        }

        private void OnDestroy()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.RemoveListener(OnToggleClicked);
        }

        private void OnToggleClicked()
        {
            SetExpanded(!_isExpanded);
        }

        private void SetExpanded(bool expanded)
        {
            _isExpanded = expanded;
            if (_menuPanel != null)
                _menuPanel.SetActive(expanded);
        }
    }
}
