using System;
// using LogosGame.Features.Gameplay.Tutorial;   // tạm tắt: chưa port hệ tutorial sang WordStack
using LogosMeta.Progression;
using UnityEngine;
using WordStack.Contracts;

namespace LogosGame.Features.Gameplay.Content
{
    [CreateAssetMenu(fileName = "SO_LevelCatalog", menuName = "WordStack/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject, ILevelCatalog
    {
        [Serializable]
        public struct Entry
        {
            public string LevelId;
            public string AddressKey;
            public string DisplayName;
            public LevelDifficulty Difficulty;
            // public TutorialConfig TutorialConfig;   // tạm tắt cùng using ở đầu file

            [Tooltip("Số nước đi cho phép. 0 = dùng mặc định của GameplayStartContext " +
                     "(đừng chép số mặc định vào đây, hai chỗ sẽ trôi khỏi nhau).")]
            public int Moves;

            [Tooltip("Hide the in-game settings button for this level (e.g. onboarding levels).")]
            public bool HideSettingsButton;

            [Tooltip("Hide the in-game coin counter for this level (e.g. onboarding levels).")]
            public bool HideCoinBox;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        [Tooltip("Chơi hết màn cuối thì quay lại từ màn này (đánh số như trong game, 1 = màn đầu). " +
                 "Lớn hơn số màn thì lặp mỗi màn cuối.")]
        [SerializeField, Min(1)] private int _loopFromLevel = 6;

        public Entry[] Entries => _entries;

        // ILevelCatalog — the game-agnostic slice LogosMeta.Progression consumes.
        // Game-only fields (TutorialConfig, HideCoinBox...) stay on Entry.
        public int Count => _entries != null ? _entries.Length : 0;

        public bool TryGetEntry(int index, out LevelEntry entry)
        {
            if (_entries == null || index < 0 || index >= _entries.Length)
            {
                entry = default;
                return false;
            }

            Entry source = _entries[index];
            entry = new LevelEntry(source.LevelId, source.AddressKey, source.DisplayName);
            return true;
        }

        public int ToCatalogIndex(int levelIndex) => ToCatalogIndex(levelIndex, Count, _loopFromLevel);

        /// Màn 0..count-1 đi thẳng; từ count trở đi lặp vòng [loopFromLevel-1 .. count-1].
        public static int ToCatalogIndex(int levelIndex, int count, int loopFromLevel)
        {
            if (count <= 0) return 0;
            if (levelIndex < 0) levelIndex = 0;
            if (levelIndex < count) return levelIndex;
            int start = Mathf.Clamp(loopFromLevel - 1, 0, count - 1);
            return start + (levelIndex - count) % (count - start);
        }

        public bool TryGetByLevelId(string levelId, out Entry entry)
        {
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                    if (_entries[i].LevelId == levelId) { entry = _entries[i]; return true; }
            }
            entry = default;
            return false;
        }

#if UNITY_INCLUDE_TESTS
        internal void SetEntries_ForTest(Entry[] entries) => _entries = entries;
#endif
    }
}
