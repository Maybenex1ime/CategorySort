namespace LogosGame.Features.Gameplay.Flow
{
    public sealed class GameplayEvaluationResult
    {
        public int CurrentScore { get; set; }
        public int RemainingMoves { get; set; }
        public int GroupsCleared { get; set; }
        public bool HasPendingAnimation { get; set; }
        public bool IsWin { get; set; }
        public bool IsLose { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public int Stars { get; set; }
        public bool CanRetry { get; set; } = true;
        public bool CanContinueToNext { get; set; }
    }
}
