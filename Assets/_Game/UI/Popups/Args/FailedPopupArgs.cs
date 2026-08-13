using System;

namespace LogosGame.Features.UI.Popups.Args
{
    public sealed class FailedPopupArgs
    {
        public string LevelTitle { get; set; }
        public Action OnTryAgain { get; set; }
        public Action OnGoHome { get; set; }
    }
}
