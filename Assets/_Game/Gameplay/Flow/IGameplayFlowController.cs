using R3;
using UnityEngine;

namespace LogosGame.Features.Gameplay.Flow
{
    /// <summary>
    /// Trạng thái + intent của một phiên chơi. HUD đọc qua đây, không đụng board.
    ///
    /// Rút gọn so với bản aquapark (23 → 19 thành viên):
    ///   bỏ CurrentScore   — WordStack không tính điểm
    ///   bỏ PendingResult  — kênh chết ngay ở aquapark, kết quả đi qua
    ///                       GameplayOutcomePublishedEvent
    ///   bỏ RequestPause/RequestResume — WordStack không có pause
    /// </summary>
    public interface IGameplayFlowController
    {
        // --- Trạng thái ---------------------------------------------------------

        ReadOnlyReactiveProperty<GameplayPhase> CurrentPhase { get; }
        ReadOnlyReactiveProperty<int> RemainingMoves { get; }
        ReadOnlyReactiveProperty<string> LevelTitle { get; }
        ReadOnlyReactiveProperty<string> LevelId { get; }
        ReadOnlyReactiveProperty<string> AddressKey { get; }

        // Suy ra từ phase, không set tay — xem UpdateDerivedState().
        ReadOnlyReactiveProperty<bool> CanOpenSettings { get; }
        ReadOnlyReactiveProperty<bool> CanReplay { get; }
        ReadOnlyReactiveProperty<bool> IsInputBlocked { get; }

        // Do GameplayStartContext quyết định, dành cho tutorial ẩn bớt UI.
        ReadOnlyReactiveProperty<bool> ShowSettingsButton { get; }
        ReadOnlyReactiveProperty<bool> ShowCoinBox { get; }

        // --- Vòng đời màn chơi --------------------------------------------------

        /// <summary>Nạp state ban đầu từ context, vào phase Ready.</summary>
        Awaitable StartLevelAsync(GameplayStartContext context);

        /// <summary>
        /// Chơi lại. Xoá LevelId/AddressKey TRƯỚC khi gọi StartLevelAsync — nếu không,
        /// chơi lại cùng màn sẽ set lại đúng giá trị cũ, ReactiveProperty không phát,
        /// và bên nghe AddressKey không nạp lại bàn.
        /// </summary>
        Awaitable ResetLevelAsync(GameplayStartContext context);

        // --- Người chơi yêu cầu -------------------------------------------------

        /// <summary>Bắn event lên bus, KHÔNG tự đổi phase — rời màn là quyết định của AppFlow.</summary>
        Awaitable RequestRestartAsync();

        /// <inheritdoc cref="RequestRestartAsync"/>
        Awaitable RequestReturnToMainMenuAsync();

        // --- Gameplay báo cáo ---------------------------------------------------

        /// <summary>Chạm đầu tiên: Ready → Playing. Đây là thứ tắt overlay "sẵn sàng".</summary>
        Awaitable NotifyFirstInteractionAsync();

        /// <summary>Vừa đi một nước: cập nhật moves, vào Evaluating (khoá input).</summary>
        Awaitable NotifyPlayerActionCommittedAsync(GameplayActionContext context);

        /// <summary>
        /// Tính xong: rẽ 4 nhánh — IsWin → Win, IsLose → Lose,
        /// HasPendingAnimation → Animating, còn lại → về Playing.
        /// </summary>
        Awaitable NotifyEvaluationCompletedAsync(GameplayEvaluationResult result);

        /// <summary>Hiệu ứng chạy xong → về Playing, mở lại input.</summary>
        Awaitable NotifyAnimationSequenceCompletedAsync();

        /// <summary>
        /// Xoá cờ đã-công-bố-kết-quả để lần thua sau còn bắn được outcome mới.
        /// Phần khôi phục trạng thái bàn chơi (hồi sinh) chưa có — xem ghi chú trong impl.
        /// </summary>
        Awaitable ResetOutcomeStateForReviveAsync();
    }
}
