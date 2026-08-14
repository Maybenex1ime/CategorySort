using System;
using System.Collections.Generic;
using BoosterModule;
using UnityEngine;

namespace LogosGame.Features.Gameplay.Content
{
    [CreateAssetMenu(menuName = "WordStack/Unlock Schedule", fileName = "SO_UnlockSchedule")]
    public sealed class SO_UnlockSchedule : ScriptableObject, IUnlockSchedule
    {
        [Serializable]
        private struct BoosterUnlockEntry
        {
            public int LevelIndex;
            public BoosterId BoosterId;
            public string DisplayName;
            [TextArea] public string Description;
            public Sprite Icon;
            public int InitialCount;
        }

        [Serializable]
        private struct MechanicUnlockEntry
        {
            public int LevelIndex;
            public string MechanicId;
            [TextArea] public string Description;
            public Sprite TitleImage;
            public float TitleScale;
            public Sprite Illustration;
            public Sprite BoxImage;
        }

        [SerializeField] private List<BoosterUnlockEntry> _boosterUnlocks = new();
        [SerializeField] private List<MechanicUnlockEntry> _mechanicUnlocks = new();

        [Header("Cheat")]
        [SerializeField] private bool _cheatGrantBoosters;
        [SerializeField] private int _cheatGrantAmount = 5;

        private Dictionary<int, BoosterUnlockEntry> _boosterByLevel;
        private Dictionary<BoosterId, BoosterUnlockEntry> _boosterById;
        private Dictionary<int, MechanicUnlockEntry> _mechanicByLevel;

        private void OnEnable() => RebuildCaches();

        private void RebuildCaches()
        {
            _boosterByLevel = new Dictionary<int, BoosterUnlockEntry>(_boosterUnlocks.Count);
            _boosterById = new Dictionary<BoosterId, BoosterUnlockEntry>(_boosterUnlocks.Count);
            foreach (BoosterUnlockEntry entry in _boosterUnlocks)
            {
                _boosterByLevel[entry.LevelIndex] = entry;
                _boosterById[entry.BoosterId] = entry;
            }

            _mechanicByLevel = new Dictionary<int, MechanicUnlockEntry>(_mechanicUnlocks.Count);
            foreach (MechanicUnlockEntry entry in _mechanicUnlocks)
                _mechanicByLevel[entry.LevelIndex] = entry;
        }

        public bool TryGetBoosterUnlock(int levelIndex, out BoosterId boosterId,
            out string displayName, out string description, out Sprite icon)
        {
            if (_boosterByLevel != null && _boosterByLevel.TryGetValue(levelIndex, out BoosterUnlockEntry entry))
            {
                boosterId = entry.BoosterId;
                displayName = entry.DisplayName;
                description = entry.Description;
                icon = entry.Icon;
                return true;
            }

            boosterId = BoosterId.None;
            displayName = string.Empty;
            description = string.Empty;
            icon = null;
            return false;
        }

        public bool TryGetMechanicUnlock(int levelIndex, out string mechanicId,
            out string description, out Sprite titleImage, out float titleScale,
            out Sprite illustration, out Sprite boxImage)
        {
            if (_mechanicByLevel != null && _mechanicByLevel.TryGetValue(levelIndex, out MechanicUnlockEntry entry))
            {
                mechanicId = entry.MechanicId;
                description = entry.Description;
                titleImage = entry.TitleImage;
                titleScale = entry.TitleScale;
                illustration = entry.Illustration;
                boxImage = entry.BoxImage;
                return true;
            }

            mechanicId = string.Empty;
            description = string.Empty;
            titleImage = null;
            titleScale = 1f;
            illustration = null;
            boxImage = null;
            return false;
        }

        public int GetBoosterInitialCount(BoosterId boosterId)
        {
            if (_boosterById != null && _boosterById.TryGetValue(boosterId, out BoosterUnlockEntry entry))
                return entry.InitialCount;
            return 0;
        }

        public bool TryGetBoosterInfo(BoosterId boosterId,
            out string displayName, out string description, out Sprite icon)
        {
            if (_boosterById != null && _boosterById.TryGetValue(boosterId, out BoosterUnlockEntry entry))
            {
                displayName = entry.DisplayName;
                description = entry.Description;
                icon = entry.Icon;
                return true;
            }

            displayName = string.Empty;
            description = string.Empty;
            icon = null;
            return false;
        }

        public bool TryGetBoosterUnlockLevel(BoosterId boosterId, out int unlockLevel)
        {
            if (_boosterById != null && _boosterById.TryGetValue(boosterId, out BoosterUnlockEntry entry))
            {
                unlockLevel = entry.LevelIndex;
                return true;
            }

            unlockLevel = 0;
            return false;
        }

        public bool TryGetBoosterIcon(BoosterId boosterId, int currentLevel, out Sprite icon)
        {
            if (_boosterById != null && _boosterById.TryGetValue(boosterId, out BoosterUnlockEntry entry)
                && (_cheatGrantBoosters || entry.LevelIndex <= currentLevel))
            {
                icon = entry.Icon;
                return true;
            }

            icon = null;
            return false;
        }

        public bool CheatGrantBoosters => _cheatGrantBoosters;
        public int CheatGrantAmount => _cheatGrantAmount;

#if UNITY_INCLUDE_TESTS
        internal void SetBoosterUnlockForTest(int levelIndex, BoosterId id, string name, string desc, Sprite icon, int initialCount = 0)
        {
            _boosterUnlocks.Add(new BoosterUnlockEntry
            {
                LevelIndex = levelIndex, BoosterId = id,
                DisplayName = name, Description = desc, Icon = icon,
                InitialCount = initialCount
            });
            RebuildCaches();
        }

        internal void SetMechanicUnlockForTest(int levelIndex, string mechanicId, string desc,
            Sprite titleImage, float titleScale, Sprite illustration, Sprite boxImage)
        {
            _mechanicUnlocks.Add(new MechanicUnlockEntry
            {
                LevelIndex = levelIndex, MechanicId = mechanicId,
                Description = desc, TitleImage = titleImage, TitleScale = titleScale,
                Illustration = illustration, BoxImage = boxImage
            });
            RebuildCaches();
        }
#endif
    }
}
