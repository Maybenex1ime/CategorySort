namespace LogosMeta.CheatPanel
{
    /// <summary>
    /// Toast payload emitted by the game's cheat service after each cheat action.
    /// <see cref="Success"/> picks the toast color; <see cref="Message"/> is the
    /// line shown to QA (e.g. "COIN CHEAT SUCCESS: Set gold to 10000").
    /// </summary>
    public readonly struct CheatNotification
    {
        public readonly bool Success;
        public readonly string Message;

        public CheatNotification(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
