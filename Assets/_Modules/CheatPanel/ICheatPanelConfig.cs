namespace LogosMeta.CheatPanel
{
    // Implemented by the game's cheat settings asset. The panel only needs the
    // master switch — everything else (booster lists, increments...) is game data.
    public interface ICheatPanelConfig
    {
        bool EnableCheats { get; }
    }
}
