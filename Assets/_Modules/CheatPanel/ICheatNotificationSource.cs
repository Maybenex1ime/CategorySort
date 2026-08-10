using R3;

namespace LogosMeta.CheatPanel
{
    // Stream of toast events. The game's cheat service implements this so the
    // module's CheatToastView can show every cheat result, including ones from
    // game-specific cheats the module knows nothing about.
    public interface ICheatNotificationSource
    {
        Observable<CheatNotification> Notifications { get; }
    }
}
