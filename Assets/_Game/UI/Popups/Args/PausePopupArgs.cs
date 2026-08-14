using System;

namespace LogosGame.Features.UI.Popups.Args
{
    public sealed class PausePopupArgs
    {
        public bool InitialMusicEnabled { get; set; } = true;
        public bool InitialHapticEnabled { get; set; } = true;
        public Action OnMusicSelected { get; set; }
        public Action OnHapticSelected { get; set; }
        public Action OnResumeSelected { get; set; }
        public Action OnRestartSelected { get; set; }
        public Action OnQuitSelected { get; set; }
        public Action OnClose { get; set; }
    }
}
