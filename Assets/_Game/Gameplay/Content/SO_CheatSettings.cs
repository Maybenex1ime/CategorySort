using System.Collections.Generic;
using BoosterModule;
using LogosMeta.CheatPanel;
using UnityEngine;

namespace LogosGame.Features.Gameplay.Content
{
    /// <summary>
    /// Master configuration for the in-game cheat menu. The whole cheat HUD is
    /// gated behind <see cref="EnableCheats"/> — flip it off for release builds.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_CheatSettings", menuName = "WordStack/Cheat Settings")]
    public sealed class CheatSettings : ScriptableObject, ICheatPanelConfig
    {
        [SerializeField, Tooltip("Master switch — when off the cheat HUD never spawns. MUST be off for release builds.")]
        private bool _enableCheats;

        [SerializeField, Tooltip("Amount added each time the COIN +N button is pressed.")]
        private int _coinIncrement = 1000;

        [SerializeField, Tooltip("Boosters listed in the cheat menu. Each entry produces a row with 0 / +1 buttons.")]
        private List<BoosterEntry> _boosterEntries = new List<BoosterEntry>();

        [SerializeField, Tooltip("Android screen sizes (portrait, pixels) for the SCREEN section. Each entry produces one button that resizes the Windows build window to that aspect ratio.")]
        private List<ScreenPreset> _screenPresets = new List<ScreenPreset>
        {
            new ScreenPreset("HD 16:9 low-end", 720, 1280),
            new ScreenPreset("FHD 16:9", 1080, 1920),
            new ScreenPreset("HD+ 20:9 Galaxy A0x", 720, 1600),
            new ScreenPreset("FHD+ 19.5:9 Galaxy A5x", 1080, 2340),
            new ScreenPreset("FHD+ 20:9 most common", 1080, 2400),
            new ScreenPreset("QHD+ 20:9 Galaxy S Ultra", 1440, 3200),
            new ScreenPreset("21:9 Xperia", 1080, 2520),
            new ScreenPreset("Fold open Z Fold", 1812, 2176),
            new ScreenPreset("Tablet 16:10 Galaxy Tab", 1600, 2560),
            new ScreenPreset("Tablet 5:3 Tab A7 Lite", 800, 1340)
        };

        public bool EnableCheats => _enableCheats;
        public int CoinIncrement => _coinIncrement;
        public IReadOnlyList<BoosterEntry> BoosterEntries => _boosterEntries;
        public IReadOnlyList<ScreenPreset> ScreenPresets => _screenPresets;

#if UNITY_INCLUDE_TESTS
        internal void SetEnableCheats_ForTest(bool value) => _enableCheats = value;
#endif

        [System.Serializable]
        public struct BoosterEntry
        {
            [SerializeField] private BoosterId _id;
            [SerializeField] private string _displayName;

            public BoosterId Id => _id;
            public string DisplayName => _displayName;
        }

        [System.Serializable]
        public struct ScreenPreset
        {
            [SerializeField] private string _label;
            [SerializeField] private int _width;
            [SerializeField] private int _height;

            public ScreenPreset(string label, int width, int height)
            {
                _label = label;
                _width = width;
                _height = height;
            }

            public string Label => _label;
            public int Width => _width;
            public int Height => _height;
        }
    }
}
