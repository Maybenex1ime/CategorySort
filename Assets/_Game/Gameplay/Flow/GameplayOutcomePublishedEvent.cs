namespace LogosGame.Features.Gameplay.Flow
{
    public readonly struct GameplayOutcomePublishedEvent
    {
        public GameplayOutcomePublishedEvent(GameplayResultViewData result)
        {
            Result = result;
        }

        public GameplayResultViewData Result { get; }
    }
}
