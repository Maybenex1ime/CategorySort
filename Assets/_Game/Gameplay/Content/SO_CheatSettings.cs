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

        public bool EnableCheats => _enableCheats;
        public int CoinIncrement => _coinIncrement;
        public IReadOnlyList<BoosterEntry> BoosterEntries => _boosterEntries;

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
    }
}
