namespace WordStack.Meta.AppFlow.Triggers
{
    // Trigger là marker type rỗng — gom chung một file cho gọn.
    // Tách ra file riêng khi số lượng lớn hoặc trigger bắt đầu mang dữ liệu.

    public sealed class BootToSplashTrigger : IAppFlowTrigger { }

    public sealed class SplashToMainMenuTrigger : IAppFlowTrigger { }

    /// <summary>MainMenu → Gameplay. Bắn khi bấm Play.</summary>
    public sealed class StartGameplayTrigger : IAppFlowTrigger { }

    /// <summary>Gameplay → Result. Bắn khi gameplay báo thắng/thua.</summary>
    public sealed class LevelFinishedTrigger : IAppFlowTrigger { }

    /// <summary>Result → Gameplay. Thắng rồi bấm Claim (CurrentLevel đã tăng).</summary>
    public sealed class NextLevelTrigger : IAppFlowTrigger { }

    /// <summary>Result → Gameplay. Thua rồi bấm Try Again (CurrentLevel giữ nguyên).</summary>
    public sealed class RetryTrigger : IAppFlowTrigger { }

    /// <summary>Gameplay hoặc Result → MainMenu.</summary>
    public sealed class ReturnToMainMenuTrigger : IAppFlowTrigger { }
}
