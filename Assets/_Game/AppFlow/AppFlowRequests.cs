namespace WordStack.Meta.AppFlow
{
    // View trong scene không inject IAppFlowManager (interface đó chỉ có StartAsync /
    // HandleRootBackAsync). Thay vào đó bắn qua Bus.Global và manager nghe — cùng lối
    // với LevelResultEvent, và giống GameplayReturnToMainMenuRequestedEvent bên aquapark.
    //
    // Phải là struct: IEventBus ràng buộc `where T : struct`.

    /// <summary>HUD gameplay xin về màn chính.</summary>
    public readonly struct ReturnToMainMenuRequestedEvent
    {
    }

    /// <summary>Gameplay xin chơi lại màn hiện tại.</summary>
    public readonly struct RestartRequestedEvent
    {
    }

    /// <summary>
    /// Cheat: nhảy tới màn N (1-based). Manager ghi LevelProgressData.CurrentLevel
    /// rồi vào gameplay — KHÁC aquapark (bên đó chỉ override in-memory): LevelService
    /// đọc thẳng save nên override tạm không có chỗ đứng, đành ghi thật.
    /// </summary>
    public readonly struct JumpToLevelRequestedEvent
    {
        public int OneBasedLevelNumber { get; }

        public JumpToLevelRequestedEvent(int oneBasedLevelNumber)
        {
            OneBasedLevelNumber = oneBasedLevelNumber;
        }
    }
}
