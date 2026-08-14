using BoosterModule;
using UnityEngine;

namespace LogosGame.Features.Gameplay.Content
{
    public interface IUnlockSchedule
    {
        bool TryGetBoosterUnlock(int levelIndex, out BoosterId boosterId,
            out string displayName, out string description, out Sprite icon);

        bool TryGetBoosterInfo(BoosterId boosterId,
            out string displayName, out string description, out Sprite icon);

        bool TryGetMechanicUnlock(int levelIndex, out string mechanicId,
            out string description, out Sprite titleImage, out float titleScale,
            out Sprite illustration, out Sprite boxImage);

        int GetBoosterInitialCount(BoosterId boosterId);

        bool CheatGrantBoosters { get; }
        int CheatGrantAmount { get; }
    }
}
