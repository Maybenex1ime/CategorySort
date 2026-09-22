using BoosterModule;
using LogosGame.Features.Gameplay.Flow;

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
    /// Nút settings trên HUD gameplay xin mở PausePopup. KHÔNG có phase Paused —
    /// game đứng yên nhờ gate input (LevelCommands.InputBlocked), không nhờ FSM.
    /// Tên theo aquapark (GameplayPauseRequestedEvent) cho dễ đối chiếu.
    /// </summary>
    public readonly struct PauseRequestedEvent
    {
    }

    /// <summary>
    /// Nút booster hết lượt + thiếu coin xin xem rewarded ad để nhận booster.
    /// </summary>
    public readonly struct RewardedBoosterRequestedEvent
    {
        public BoosterId BoosterId { get; }

        public RewardedBoosterRequestedEvent(BoosterId boosterId)
        {
            BoosterId = boosterId;
        }
    }

    /// <summary>
    /// Cheat: ép màn hiện tại kết thúc thắng/thua để test luồng Result.
    /// Chạy TRỌN luồng thật: cả kênh điều hướng (ViewModel → popup) lẫn kênh meta
    /// (LevelSignals.Finished → coin + progression) — thắng ép xong bấm Next là
    /// sang màn kế thật, không phải màn cũ.
    /// </summary>
    public readonly struct ForceOutcomeRequestedEvent
    {
        public bool IsWin { get; }

        /// <summary>
        /// Chỉ khi thua — giả lập lý do thua để test luồng hồi sinh:
        ///   None       — thua thẳng, vào FailedPopup (không hỏi hồi sinh).
        ///   OutOfMoves — RevivePopup kiểu +nước; hồi sinh cộng nước thật, chơi tiếp được.
        ///   Stuck      — RevivePopup kiểu nam châm; hồi sinh chạy nam châm thật lên bàn
        ///                (dù bàn không kẹt) rồi chơi tiếp.
        /// </summary>
        public LoseReason LoseReason { get; }

        public ForceOutcomeRequestedEvent(bool isWin, LoseReason loseReason = LoseReason.None)
        {
            IsWin = isWin;
            LoseReason = loseReason;
        }
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
