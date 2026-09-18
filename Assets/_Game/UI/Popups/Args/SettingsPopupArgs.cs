using System;

namespace LogosGame.Features.UI.Popups.Args
{
    public sealed class SettingsPopupArgs
    {
        public bool InitialMusicEnabled { get; set; } = true;
        public bool InitialHapticEnabled { get; set; } = true;
        public bool InitialSoundEnabled { get; set; } = true;
        public bool InitialNotificationEnabled { get; set; } = true;
        public Action OnMusicSelected { get; set; }
        public Action OnHapticSelected { get; set; }
        public Action OnSoundSelected { get; set; }
        public Action OnNotificationSelected { get; set; }
        public Action OnResumeSelected { get; set; }
        public Action OnRestartSelected { get; set; }
        public Action OnQuitSelected { get; set; }
        public Action OnSupportSelected { get; set; }
        public Action OnTermsSelected { get; set; }
        public Action OnPrivacySelected { get; set; }
        public Action OnClose { get; set; }
    }
}
