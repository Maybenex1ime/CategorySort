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
}
