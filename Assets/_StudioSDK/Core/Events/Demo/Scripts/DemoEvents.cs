namespace EventBus.Demo
{
    public struct PlayerDamagedEvent
    {
        public int DamageAmount;
        public string DamageSource;
    }

    public struct ItemCollectedEvent
    {
        public string ItemName;
        public int ItemValue;
    }

    public struct GameStateChangedEvent
    {
        public GameState NewState;
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }
} 