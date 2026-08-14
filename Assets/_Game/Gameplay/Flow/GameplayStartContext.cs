namespace LogosGame.Features.Gameplay.Flow
{
    public sealed class GameplayStartContext
    {
        public string LevelTitle { get; set; } = string.Empty;
        public string LevelId { get; set; } = string.Empty;
        public string AddressKey { get; set; } = string.Empty;
        public int StartingScore { get; set; }
        public int StartingMoves { get; set; } = 20;
        // Thêm cho WordStack: catalog là nguồn độ khó duy nhất (không phải file level).
        public WordStack.Contracts.LevelDifficulty Difficulty { get; set; }

        public bool ShowSettingsButton { get; set; } = true;
        public bool ShowCoinBox { get; set; } = true;
    }
}
