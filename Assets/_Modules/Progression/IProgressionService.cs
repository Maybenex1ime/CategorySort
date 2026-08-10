namespace LogosMeta.Progression
{
    /// <summary>
    /// Receives game results for progression tracking. The module only needs
    /// win/lose — game-specific result payloads stay in the game.
    /// </summary>
    public interface IProgressionService
    {
        void ReportResult(bool isWin);
    }
}
