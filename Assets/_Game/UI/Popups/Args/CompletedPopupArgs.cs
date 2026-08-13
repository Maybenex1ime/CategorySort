using System;

namespace LogosGame.Features.UI.Popups.Args
{
    public sealed class CompletedPopupArgs
    {
        public string LevelTitle { get; set; }
        public int RewardCoinAmount { get; set; }
        public Action OnClaim { get; set; }
    }
}
