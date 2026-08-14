using BoosterModule;
using LogosMeta.CheatPanel;
using R3;

namespace LogosGame.Features.Cheat.Services
{
    /// <summary>
    /// Entry point for QA cheat actions. Economy cheats + the toast stream come
    /// from the LogosMeta.CheatPanel contracts; this interface adds the
    /// game-specific cheats (boosters, level jump).
    /// </summary>
    public interface ICheatService : IEconomyCheatActions, ICheatNotificationSource
    {
        // True when a Gameplay-scope booster bridge is alive — controls whether
        // booster cheats apply immediately or are reported as "unavailable".
        ReadOnlyReactiveProperty<bool> AreBoostersAvailable { get; }

        // Booster cheats — routed via Bus.Global to the Gameplay-scope bridge.
        void SetBoosterCount(BoosterId boosterId, int amount);
        void AddBoosterCount(BoosterId boosterId, int delta);

        // Level cheat — works in MainMenu (starts gameplay at level) and in Gameplay
        // (in-memory jump). 1-based level number.
        void JumpToLevel(int oneBasedLevelNumber);
    }
}
