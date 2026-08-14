namespace LogosGame.Features.Gameplay.Flow
{
    public sealed class GameplayResultViewData
    {
        public bool IsWin { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public int Score { get; set; }
        public int Stars { get; set; }
        public bool CanRetry { get; set; } = true;
        public bool CanContinueToNext { get; set; }
    }
}
