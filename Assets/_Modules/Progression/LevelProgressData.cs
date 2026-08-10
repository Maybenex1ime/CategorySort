using System;
using System.Collections.Generic;

namespace LogosMeta.Progression
{
    [Serializable]
    public class LevelProgressData
    {
        public int SchemaVersion = 1;
        // 0-based catalog index of the level the player is currently up to.
        // ProgressionService increments this on each win, so on first run it points to catalog[0].
        public int CurrentLevel = 0;
        public int[] Stars = Array.Empty<int>();
        public Dictionary<int, bool> UnlockedLevels = new();
    }
}
